using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class OverrideService : IOverrideService
{
    private readonly AppDbContext _db;

    public OverrideService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OverrideApplicationResult> ApplyOverridesAsync(
        Guid dealId, ParsedDocumentResult parsedData, CancellationToken ct = default)
    {
        if (!parsedData.Success)
            return new OverrideApplicationResult
            {
                Success = false,
                ErrorMessage = $"Cannot apply overrides from failed parse: {parsedData.ErrorMessage}"
            };

        var deal = await _db.Deals.FindAsync(new object[] { dealId }, ct);
        if (deal is null)
            return new OverrideApplicationResult { Success = false, ErrorMessage = "Deal not found." };

        var source = $"User-Provided: {parsedData.DocumentType}";
        var overrides = new List<FieldOverrideDto>();

        switch (parsedData.DocumentType)
        {
            case DocumentType.RentRoll:
                ApplyRentRollOverrides(deal, parsedData, source, overrides);
                break;
            case DocumentType.T12PAndL:
                ApplyT12Overrides(deal, parsedData, source, overrides);
                break;
            case DocumentType.LoanTermSheet:
                ApplyLoanTermOverrides(deal, parsedData, source, overrides);
                break;
        }

        // Persist FieldOverride records
        foreach (var dto in overrides)
        {
            var entity = new FieldOverride(dealId, parsedData.DocumentId, dto.FieldName, dto.OriginalValue, dto.NewValue, source);
            _db.FieldOverrides.Add(entity);
        }

        await _db.SaveChangesAsync(ct);

        return new OverrideApplicationResult { Success = true, AppliedOverrides = overrides };
    }

    public async Task<IReadOnlyList<FieldOverrideDto>> GetOverridesForDealAsync(
        Guid dealId, CancellationToken ct = default)
    {
        return await _db.FieldOverrides
            .Where(f => f.DealId == dealId)
            .OrderByDescending(f => f.AppliedAt)
            .Select(f => new FieldOverrideDto
            {
                FieldName = f.FieldName,
                OriginalValue = f.OriginalValue,
                NewValue = f.NewValue,
                Source = f.Source,
                AppliedAt = f.AppliedAt,
            })
            .ToListAsync(ct);
    }

    private static void ApplyRentRollOverrides(Deal deal, ParsedDocumentResult parsed, string source, List<FieldOverrideDto> overrides)
    {
        decimal? annualized = parsed.TotalMonthlyRent.HasValue ? parsed.TotalMonthlyRent.Value * 12 : null;
        ApplyField(overrides, source, "RentRollSummary", deal.RentRollSummary?.ToString() ?? "", annualized, v => deal.RentRollSummary = v);
        ApplyField(overrides, source, "TargetOccupancy", deal.TargetOccupancy?.ToString() ?? "", parsed.OccupancyRate, v => deal.TargetOccupancy = v);
        ApplyField<int>(overrides, source, "UnitCount", deal.UnitCount.ToString(), parsed.UnitCount, v => deal.UnitCount = v);
    }

    private static void ApplyT12Overrides(Deal deal, ParsedDocumentResult parsed, string source, List<FieldOverrideDto> overrides)
    {
        ApplyField(overrides, source, "T12Summary", deal.T12Summary?.ToString() ?? "", parsed.NetOperatingIncome, v => deal.T12Summary = v);
    }

    private static void ApplyLoanTermOverrides(Deal deal, ParsedDocumentResult parsed, string source, List<FieldOverrideDto> overrides)
    {
        ApplyField(overrides, source, "LoanLtv", deal.LoanLtv?.ToString() ?? "", parsed.LtvRatio, v => deal.LoanLtv = v);
        ApplyField(overrides, source, "LoanRate", deal.LoanRate?.ToString() ?? "", parsed.InterestRate, v => deal.LoanRate = v);
        ApplyField(overrides, source, "IsInterestOnly", deal.IsInterestOnly.ToString(), parsed.IsInterestOnly, v => deal.IsInterestOnly = v);
        ApplyField(overrides, source, "AmortizationYears", deal.AmortizationYears?.ToString() ?? "", parsed.AmortizationYears, v => deal.AmortizationYears = v);
        ApplyField(overrides, source, "LoanTermYears", deal.LoanTermYears?.ToString() ?? "", parsed.LoanTermYears, v => deal.LoanTermYears = v);
    }

    private static void ApplyField<T>(List<FieldOverrideDto> overrides, string source, string fieldName, string currentValue, T? newValue, Action<T> setter)
        where T : struct
    {
        if (!newValue.HasValue) return;
        overrides.Add(new FieldOverrideDto
        {
            FieldName = fieldName,
            OriginalValue = currentValue,
            NewValue = newValue.Value.ToString()!,
            Source = source,
            AppliedAt = DateTime.UtcNow,
        });
        setter(newValue.Value);
    }
}
