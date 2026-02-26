using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class AssetReportServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly AssetReportService _service;
    private readonly Guid _dealId;

    public AssetReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AssetReportTests_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        var varianceCalc = new VarianceCalculator();
        var actualsService = new ActualsService(_db);
        _service = new AssetReportService(_db, varianceCalc, actualsService);

        _dealId = Guid.NewGuid();
        var deal = new Deal("Report Test Property", "test-user");
        typeof(Deal).GetProperty("Id")!.SetValue(deal, _dealId);
        typeof(Deal).GetProperty("PurchasePrice")!.SetValue(deal, 1000000m);
        deal.PropertyName = "Report Test Property";

        var calc = new CalculationResult(_dealId)
        {
            NetOperatingIncome = 100000m,
            EffectiveGrossIncome = 160000m,
            OperatingExpenses = 60000m,
            GrossPotentialRent = 170000m,
            VacancyLoss = 10000m,
            OtherIncome = 0m,
            CashOnCashReturn = 8m,
            LoanAmount = 700000m
        };
        deal.CalculationResult = calc;

        _db.Deals.Add(deal);
        _db.CalculationResults.Add(calc);
        _db.SaveChanges();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task GenerateMonthlyReport_CreatesReport()
    {
        var report = await _service.GenerateMonthlyReportAsync(_dealId, 2025, 6);

        Assert.NotNull(report);
        Assert.Equal(AssetReportType.Monthly, report.Type);
        Assert.Equal(2025, report.Year);
        Assert.Equal(6, report.Month);
        Assert.NotNull(report.PerformanceSummary);
    }

    [Fact]
    public async Task GenerateQuarterlyReport_CreatesReport()
    {
        var report = await _service.GenerateQuarterlyReportAsync(_dealId, 2025, 2);

        Assert.Equal(AssetReportType.Quarterly, report.Type);
        Assert.Equal(2, report.Quarter);
    }

    [Fact]
    public async Task GenerateAnnualReport_CreatesReport()
    {
        var report = await _service.GenerateAnnualReportAsync(_dealId, 2025);

        Assert.Equal(AssetReportType.Annual, report.Type);
        Assert.Null(report.Month);
        Assert.Null(report.Quarter);
    }

    [Fact]
    public async Task GetReports_ReturnsOrderedByDate()
    {
        await _service.GenerateMonthlyReportAsync(_dealId, 2025, 1);
        await _service.GenerateMonthlyReportAsync(_dealId, 2025, 6);
        await _service.GenerateMonthlyReportAsync(_dealId, 2025, 3);

        var reports = await _service.GetReportsAsync(_dealId);

        Assert.Equal(3, reports.Count);
        // Ordered by year desc, then month desc
        Assert.Equal(6, reports[0].Month);
        Assert.Equal(3, reports[1].Month);
        Assert.Equal(1, reports[2].Month);
    }

    [Fact]
    public async Task GetReport_ById_ReturnsCorrectReport()
    {
        var generated = await _service.GenerateMonthlyReportAsync(_dealId, 2025, 5);

        var fetched = await _service.GetReportAsync(generated.Id);

        Assert.NotNull(fetched);
        Assert.Equal(generated.Id, fetched.Id);
    }

    [Fact]
    public async Task DeleteReport_RemovesFromDb()
    {
        var report = await _service.GenerateAnnualReportAsync(_dealId, 2025);

        await _service.DeleteReportAsync(report.Id);

        var fetched = await _service.GetReportAsync(report.Id);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task GenerateReport_WithActuals_IncludesVarianceNarrative()
    {
        // Add some actuals
        for (int m = 1; m <= 3; m++)
        {
            var actual = new MonthlyActual(_dealId, 2025, m)
            {
                GrossRentalIncome = 14000m,
                VacancyLoss = 700m,
                OtherIncome = 0m,
                PropertyTaxes = 1500m,
                Insurance = 600m,
                Utilities = 800m,
                Repairs = 500m,
                Management = 1000m,
                Payroll = 0m, Marketing = 0m, Administrative = 0m, OtherExpenses = 200m,
                DebtService = 3000m
            };
            actual.Recalculate();
            _db.MonthlyActuals.Add(actual);
        }
        await _db.SaveChangesAsync();

        var report = await _service.GenerateMonthlyReportAsync(_dealId, 2025, 4);

        Assert.NotNull(report.PerformanceSummary);
        Assert.NotNull(report.VarianceAnalysis);
        Assert.NotNull(report.MetricsSnapshotJson);
        Assert.Contains("Report Test Property", report.PerformanceSummary);
    }

    [Fact]
    public async Task PeriodLabel_Monthly_FormatsCorrectly()
    {
        var report = await _service.GenerateMonthlyReportAsync(_dealId, 2025, 3);
        Assert.Equal("Mar 2025", report.PeriodLabel);
    }

    [Fact]
    public async Task PeriodLabel_Quarterly_FormatsCorrectly()
    {
        var report = await _service.GenerateQuarterlyReportAsync(_dealId, 2025, 2);
        Assert.Equal("Q2 2025", report.PeriodLabel);
    }

    [Fact]
    public async Task PeriodLabel_Annual_FormatsCorrectly()
    {
        var report = await _service.GenerateAnnualReportAsync(_dealId, 2025);
        Assert.Equal("2025", report.PeriodLabel);
    }

    [Fact]
    public async Task GenerateMonthlyReport_InvalidMonth_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.GenerateMonthlyReportAsync(_dealId, 2025, 13));
    }

    [Fact]
    public async Task GenerateQuarterlyReport_InvalidQuarter_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.GenerateQuarterlyReportAsync(_dealId, 2025, 5));
    }
}
