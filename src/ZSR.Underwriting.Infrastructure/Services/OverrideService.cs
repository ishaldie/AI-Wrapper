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
        if (parsed.TotalMonthlyRent.HasValue)
        {
            var annualized = parsed.TotalMonthlyRent.Value * 12;
            TrackOverride(overrides, "RentRollSummary", deal.RentRollSummary?.ToString() ?? "", annualized.ToString(), source);
            deal.RentRollSummary = annualized;
        }

        if (parsed.OccupancyRate.HasValue)
        {
            TrackOverride(overrides, "TargetOccupancy", deal.TargetOccupancy?.ToString() ?? "", parsed.OccupancyRate.Value.ToString(), source);
            deal.TargetOccupancy = parsed.OccupancyRate.Value;
        }

        if (parsed.UnitCount.HasValue)
        {
            TrackOverride(overrides, "UnitCount", deal.UnitCount.ToString(), parsed.UnitCount.Value.ToString(), source);
            deal.UnitCount = parsed.UnitCount.Value;
        }
    }

    private static void ApplyT12Overrides(Deal deal, ParsedDocumentResult parsed, string source, List<FieldOverrideDto> overrides)
    {
        if (parsed.NetOperatingIncome.HasValue)
        {
            TrackOverride(overrides, "T12Summary", deal.T12Summary?.ToString() ?? "", parsed.NetOperatingIncome.Value.ToString(), source);
            deal.T12Summary = parsed.NetOperatingIncome.Value;
        }
    }

    private static void ApplyLoanTermOverrides(Deal deal, ParsedDocumentResult parsed, string source, List<FieldOverrideDto> overrides)
    {
        if (parsed.LtvRatio.HasValue)
        {
            TrackOverride(overrides, "LoanLtv", deal.LoanLtv?.ToString() ?? "", parsed.LtvRatio.Value.ToString(), source);
            deal.LoanLtv = parsed.LtvRatio.Value;
        }

        if (parsed.InterestRate.HasValue)
        {
            TrackOverride(overrides, "LoanRate", deal.LoanRate?.ToString() ?? "", parsed.InterestRate.Value.ToString(), source);
            deal.LoanRate = parsed.InterestRate.Value;
        }

        if (parsed.IsInterestOnly.HasValue)
        {
            TrackOverride(overrides, "IsInterestOnly", deal.IsInterestOnly.ToString(), parsed.IsInterestOnly.Value.ToString(), source);
            deal.IsInterestOnly = parsed.IsInterestOnly.Value;
        }

        if (parsed.AmortizationYears.HasValue)
        {
            TrackOverride(overrides, "AmortizationYears", deal.AmortizationYears?.ToString() ?? "", parsed.AmortizationYears.Value.ToString(), source);
            deal.AmortizationYears = parsed.AmortizationYears.Value;
        }

        if (parsed.LoanTermYears.HasValue)
        {
            TrackOverride(overrides, "LoanTermYears", deal.LoanTermYears?.ToString() ?? "", parsed.LoanTermYears.Value.ToString(), source);
            deal.LoanTermYears = parsed.LoanTermYears.Value;
        }
    }

    private static void TrackOverride(List<FieldOverrideDto> overrides, string fieldName, string original, string newValue, string source)
    {
        overrides.Add(new FieldOverrideDto
        {
            FieldName = fieldName,
            OriginalValue = original,
            NewValue = newValue,
            Source = source,
            AppliedAt = DateTime.UtcNow,
        });
    }
}
