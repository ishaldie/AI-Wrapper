using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class SecuritizationCompService : ISecuritizationCompService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SecuritizationCompService> _logger;

    private static readonly DateTime CutoffDate = DateTime.UtcNow.AddYears(-3);

    public SecuritizationCompService(AppDbContext db, ILogger<SecuritizationCompService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ComparisonResult> FindCompsAsync(Deal deal, int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var propertyType = deal.PropertyType;
        var state = ParseState(deal.Address);
        var unitCount = deal.UnitCount;

        // Step 1: Filter by property type + recent vintage (last 3 years)
        var cutoff = DateTime.UtcNow.AddYears(-3);
        var baseQuery = _db.SecuritizationComps
            .Where(c => c.PropertyType == propertyType)
            .Where(c => c.OriginationDate == null || c.OriginationDate >= cutoff);

        // Step 2: Try same state first
        var stateComps = !string.IsNullOrEmpty(state)
            ? await baseQuery.Where(c => c.State == state).ToListAsync(cancellationToken)
            : new List<SecuritizationComp>();

        // Step 3: If fewer than 5 comps in-state, expand to nationwide
        List<SecuritizationComp> allComps;
        if (stateComps.Count < 5)
        {
            var nationwideComps = await baseQuery.ToListAsync(cancellationToken);
            // Deduplicate: state comps are already in nationwide set
            allComps = nationwideComps;
        }
        else
        {
            allComps = stateComps;
        }

        if (allComps.Count == 0)
        {
            return new ComparisonResult
            {
                Comps = Array.Empty<SecuritizationComp>(),
                TotalCompsFound = 0,
            };
        }

        var totalFound = allComps.Count;

        // Step 4: Rank by similarity score
        var ranked = allComps
            .Select(c => new
            {
                Comp = c,
                Score = ComputeSimilarityScore(c, state, unitCount)
            })
            .OrderByDescending(x => x.Score)
            .Select(x => x.Comp)
            .Take(maxResults)
            .ToList();

        // Step 5: Compute market aggregates from the ranked comps
        return BuildComparisonResult(ranked, totalFound, deal);
    }

    private static double ComputeSimilarityScore(SecuritizationComp comp, string? targetState, int targetUnits)
    {
        double score = 0;

        // State match bonus: +40 points for same state
        if (!string.IsNullOrEmpty(targetState) && comp.State == targetState)
            score += 40;

        // Unit count proximity: up to +30 points
        // Score = 30 * (1 - |unitDiff| / targetUnits), clamped to [0, 30]
        if (comp.Units.HasValue && targetUnits > 0)
        {
            var unitDiff = Math.Abs(comp.Units.Value - targetUnits);
            var unitScore = Math.Max(0, 30.0 * (1.0 - (double)unitDiff / targetUnits));
            score += unitScore;
        }

        // Recency bonus: up to +30 points
        // More recent = higher score. Linear decay over 3 years.
        if (comp.OriginationDate.HasValue)
        {
            var monthsAgo = (DateTime.UtcNow - comp.OriginationDate.Value).TotalDays / 30.0;
            var recencyScore = Math.Max(0, 30.0 * (1.0 - monthsAgo / 36.0));
            score += recencyScore;
        }

        return score;
    }

    private static ComparisonResult BuildComparisonResult(List<SecuritizationComp> comps, int totalFound, Deal deal)
    {
        var dscrValues = comps.Where(c => c.DSCR.HasValue).Select(c => c.DSCR!.Value).OrderBy(v => v).ToList();
        var ltvValues = comps.Where(c => c.LTV.HasValue).Select(c => c.LTV!.Value).OrderBy(v => v).ToList();
        var capRateValues = comps.Where(c => c.CapRate.HasValue).Select(c => c.CapRate!.Value).OrderBy(v => v).ToList();
        var occupancyValues = comps.Where(c => c.Occupancy.HasValue).Select(c => c.Occupancy!.Value).OrderBy(v => v).ToList();
        var rateValues = comps.Where(c => c.InterestRate.HasValue).Select(c => c.InterestRate!.Value).OrderBy(v => v).ToList();

        return new ComparisonResult
        {
            Comps = comps,
            TotalCompsFound = totalFound,

            // User metrics
            UserDSCR = deal.CalculationResult?.DebtServiceCoverageRatio,
            UserLTV = deal.LoanLtv,
            UserCapRate = deal.CalculationResult?.GoingInCapRate,
            UserOccupancy = deal.TargetOccupancy,
            UserInterestRate = deal.LoanRate,

            // Market aggregates
            MedianDSCR = Median(dscrValues),
            MinDSCR = dscrValues.Count > 0 ? dscrValues[0] : null,
            MaxDSCR = dscrValues.Count > 0 ? dscrValues[^1] : null,

            MedianLTV = Median(ltvValues),
            MinLTV = ltvValues.Count > 0 ? ltvValues[0] : null,
            MaxLTV = ltvValues.Count > 0 ? ltvValues[^1] : null,

            MedianCapRate = Median(capRateValues),
            MinCapRate = capRateValues.Count > 0 ? capRateValues[0] : null,
            MaxCapRate = capRateValues.Count > 0 ? capRateValues[^1] : null,

            MedianOccupancy = Median(occupancyValues),
            MinOccupancy = occupancyValues.Count > 0 ? occupancyValues[0] : null,
            MaxOccupancy = occupancyValues.Count > 0 ? occupancyValues[^1] : null,

            MedianInterestRate = Median(rateValues),
            MinInterestRate = rateValues.Count > 0 ? rateValues[0] : null,
            MaxInterestRate = rateValues.Count > 0 ? rateValues[^1] : null,
        };
    }

    private static decimal? Median(List<decimal> sorted)
    {
        if (sorted.Count == 0) return null;
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    private static string? ParseState(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            // State is typically the last comma-separated token (or second-to-last if zip is present)
            var candidate = parts[^1];
            // If it looks like a zip code, use the previous part
            if (candidate.Length >= 5 && candidate.All(c => char.IsDigit(c) || c == '-'))
                candidate = parts.Length >= 3 ? parts[^2] : candidate;

            // Extract just the state abbreviation (could be "GA" or "GA 30301")
            var stateToken = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (stateToken?.Length == 2 && stateToken.All(char.IsLetter))
                return stateToken.ToUpperInvariant();
        }

        return null;
    }
}
