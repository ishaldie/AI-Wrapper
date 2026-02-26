using FluentValidation;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Application.Validators;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Parsing;

namespace ZSR.Underwriting.Infrastructure.Services;

public class BulkImportService : IBulkImportService
{
    private readonly IDealService _dealService;
    private readonly IPortfolioService _portfolioService;
    private readonly PortfolioImportParser _parser;
    private readonly ILogger<BulkImportService> _logger;

    public BulkImportService(
        IDealService dealService,
        IPortfolioService portfolioService,
        ILogger<BulkImportService> logger)
    {
        _dealService = dealService;
        _portfolioService = portfolioService;
        _parser = new PortfolioImportParser();
        _logger = logger;
    }

    public Task<List<BulkImportRowDto>> ParseFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        return _parser.ParseAsync(fileStream, fileName, ct);
    }

    public async Task<BulkImportResultDto> ImportAsync(
        List<BulkImportRowDto> rows,
        string portfolioName,
        string userId,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var result = new BulkImportResultDto
        {
            TotalRows = rows.Count,
            PortfolioName = portfolioName,
        };

        // Validate all rows first
        var validator = new BulkImportRowValidator();
        foreach (var row in rows)
        {
            var validation = await validator.ValidateAsync(row, ct);
            if (!validation.IsValid)
                row.Errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
        }

        var validRows = rows.Where(r => r.IsValid).ToList();
        var preValidationFailures = rows.Count - validRows.Count;

        if (validRows.Count == 0)
        {
            result.Errors.Add("No valid rows to import.");
            result.FailedCount = rows.Count;
            return result;
        }

        // Create portfolio
        Guid portfolioId;
        try
        {
            portfolioId = await _portfolioService.CreateAsync(portfolioName, userId);
            result.PortfolioId = portfolioId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create portfolio '{Name}'", portfolioName);
            result.Errors.Add($"Failed to create portfolio: {ex.Message}");
            return result;
        }

        // Import deals sequentially (geocoding per deal)
        int completed = 0;
        foreach (var row in validRows)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var propertyType = PropertyType.Multifamily;
                if (!string.IsNullOrWhiteSpace(row.PropertyType) &&
                    Enum.TryParse<PropertyType>(row.PropertyType, true, out var parsed))
                {
                    propertyType = parsed;
                }

                var input = new DealInputDto
                {
                    PropertyName = row.PropertyName,
                    Address = row.Address,
                    UnitCount = row.UnitCount,
                    PurchasePrice = row.PurchasePrice,
                    RentRollSummary = row.RentRollSummary,
                    T12Summary = row.T12Summary,
                    LoanLtv = row.LoanLtv,
                    LoanRate = row.LoanRate,
                    CapexBudget = row.CapexBudget,
                    PropertyType = propertyType,
                    LicensedBeds = row.LicensedBeds,
                    AverageDailyRate = row.AverageDailyRate,
                    PrivatePayPct = row.PrivatePayPct,
                };

                var dealId = await _dealService.CreateDealAsync(input, userId);
                await _portfolioService.AssignDealAsync(portfolioId, dealId, userId);

                result.CreatedDealIds.Add(dealId);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to import row {Row}: {Property}", row.RowNumber, row.PropertyName);
                row.Errors.Add($"Import failed: {ex.Message}");
                result.FailedCount++;
                result.Errors.Add($"Row {row.RowNumber} ({row.PropertyName}): {ex.Message}");
            }

            completed++;
            progress?.Report((int)((double)completed / validRows.Count * 100));
        }

        // Count pre-validation failures (only those that were invalid before import)
        result.FailedCount += preValidationFailures;

        return result;
    }
}
