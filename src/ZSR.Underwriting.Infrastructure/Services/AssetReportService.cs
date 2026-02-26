using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class AssetReportService : IAssetReportService
{
    private readonly AppDbContext _db;
    private readonly IVarianceCalculator _varianceCalculator;
    private readonly IActualsService _actualsService;

    public AssetReportService(AppDbContext db, IVarianceCalculator varianceCalculator, IActualsService actualsService)
    {
        _db = db;
        _varianceCalculator = varianceCalculator;
        _actualsService = actualsService;
    }

    public async Task<IReadOnlyList<AssetReport>> GetReportsAsync(Guid dealId)
    {
        return await _db.AssetReports
            .Where(r => r.DealId == dealId)
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Quarter ?? 0)
            .ThenByDescending(r => r.Month ?? 0)
            .ToListAsync();
    }

    public async Task<AssetReport?> GetReportAsync(Guid reportId)
    {
        return await _db.AssetReports.FindAsync(reportId);
    }

    public async Task<AssetReport> GenerateMonthlyReportAsync(Guid dealId, int year, int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month));

        var report = new AssetReport(dealId, AssetReportType.Monthly, year) { Month = month };
        await PopulateReportDataAsync(report, dealId);
        _db.AssetReports.Add(report);
        await _db.SaveChangesAsync();
        return report;
    }

    public async Task<AssetReport> GenerateQuarterlyReportAsync(Guid dealId, int year, int quarter)
    {
        if (quarter < 1 || quarter > 4)
            throw new ArgumentOutOfRangeException(nameof(quarter));

        var report = new AssetReport(dealId, AssetReportType.Quarterly, year) { Quarter = quarter };
        await PopulateReportDataAsync(report, dealId);
        _db.AssetReports.Add(report);
        await _db.SaveChangesAsync();
        return report;
    }

    public async Task<AssetReport> GenerateAnnualReportAsync(Guid dealId, int year)
    {
        var report = new AssetReport(dealId, AssetReportType.Annual, year);
        await PopulateReportDataAsync(report, dealId);
        _db.AssetReports.Add(report);
        await _db.SaveChangesAsync();
        return report;
    }

    public async Task DeleteReportAsync(Guid reportId)
    {
        var report = await _db.AssetReports.FindAsync(reportId);
        if (report is not null)
        {
            _db.AssetReports.Remove(report);
            await _db.SaveChangesAsync();
        }
    }

    private async Task PopulateReportDataAsync(AssetReport report, Guid dealId)
    {
        var deal = await _db.Deals
            .Include(d => d.CalculationResult)
            .FirstOrDefaultAsync(d => d.Id == dealId);

        if (deal?.CalculationResult is null) return;

        var actuals = await _actualsService.GetTrailingTwelveAsync(dealId);
        var variance = actuals.Count > 0
            ? _varianceCalculator.CalculateVariance(deal.CalculationResult, actuals)
            : null;

        var annualSummary = await _actualsService.GetAnnualSummaryAsync(dealId, report.Year);

        // Build metrics snapshot
        var snapshot = new
        {
            ProjectedNoi = deal.CalculationResult.NetOperatingIncome,
            ActualNoi = variance?.ActualNoi,
            NoiVariancePercent = variance?.NoiVariancePercent,
            ProjectedRevenue = deal.CalculationResult.EffectiveGrossIncome,
            ActualRevenue = variance?.ActualRevenue,
            ProjectedExpenses = deal.CalculationResult.OperatingExpenses,
            ActualExpenses = variance?.ActualExpenses,
            annualSummary?.AverageOccupancy
        };

        report.MetricsSnapshotJson = JsonSerializer.Serialize(snapshot);

        // Generate summary narratives from data
        report.PerformanceSummary = BuildPerformanceSummary(deal, variance, annualSummary);
        report.VarianceAnalysis = BuildVarianceNarrative(variance);
    }

    private static string BuildPerformanceSummary(Deal deal, VarianceReport? variance, AnnualSummaryDto? summary)
    {
        if (variance is null)
            return "Insufficient data to generate performance summary. Enter monthly actuals to enable analysis.";

        var noiDirection = variance.NoiVariancePercent >= 0 ? "above" : "below";
        return $"{deal.PropertyName} is performing {Math.Abs(variance.NoiVariancePercent):F1}% {noiDirection} " +
               $"underwriting projections with annualized NOI of {variance.ActualNoi:C0} vs. projected {variance.ProjectedNoi:C0}. " +
               $"Revenue is tracking at {variance.ActualRevenue:C0} against a projection of {variance.ProjectedRevenue:C0}. " +
               (summary is not null ? $"Average occupancy for the period is {summary.AverageOccupancy:F1}%." : "");
    }

    private static string BuildVarianceNarrative(VarianceReport? variance)
    {
        if (variance is null) return "No variance data available.";

        var criticalItems = variance.RevenueItems.Concat(variance.ExpenseItems)
            .Where(i => i.Severity == VarianceSeverity.Critical)
            .ToList();

        if (criticalItems.Count == 0)
            return "All line items are within acceptable variance thresholds.";

        var labels = string.Join(", ", criticalItems.Select(i => i.Label));
        return $"Critical variances detected in: {labels}. " +
               $"Overall NOI variance is {variance.NoiVariancePercent:F1}% ({variance.NoiVariance:C0}).";
    }
}
