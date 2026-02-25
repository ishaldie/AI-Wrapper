using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Formatting;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class ReportAssemblerTests : IDisposable
{
    private readonly AppDbContext _db;

    public ReportAssemblerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private Deal CreateTestDeal()
    {
        var deal = new Deal("Test Deal");
        deal.PropertyName = "Sunset Apartments";
        deal.Address = "123 Main St, Dallas, TX";
        deal.UnitCount = 100;
        deal.PurchasePrice = 10_000_000m;
        deal.RentRollSummary = 1000m;  // $1000/unit/mo
        deal.T12Summary = 800_000m;
        deal.LoanLtv = 70m;
        deal.LoanRate = 6.5m;
        deal.IsInterestOnly = false;
        deal.AmortizationYears = 30;
        deal.LoanTermYears = 5;
        deal.HoldPeriodYears = 5;
        deal.CapexBudget = 500_000m;
        deal.TargetOccupancy = 95m;
        deal.ValueAddPlans = "Interior renovations, amenity upgrades";
        return deal;
    }

    [Fact]
    public async Task AssembleReportAsync_ReturnsReportWith10Sections()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(10, report.GetSectionsInOrder().Count);
    }

    [Fact]
    public async Task AssembleReportAsync_SetsPropertyInfo()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(deal.Id, report.DealId);
        Assert.Equal("Sunset Apartments", report.PropertyName);
        Assert.Equal("123 Main St, Dallas, TX", report.Address);
    }

    [Fact]
    public async Task AssembleReportAsync_CoreMetrics_HasCorrectPurchasePrice()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(10_000_000m, report.CoreMetrics.PurchasePrice);
        Assert.Equal(100, report.CoreMetrics.UnitCount);
        Assert.Equal(100_000m, report.CoreMetrics.PricePerUnit);
    }

    [Fact]
    public async Task AssembleReportAsync_CoreMetrics_CalculatesLoanAmount()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // LTV 70% of $10M = $7M loan
        Assert.Equal(7_000_000m, report.CoreMetrics.LoanAmount);
        Assert.Equal(70m, report.CoreMetrics.LtvPercent);
    }

    [Fact]
    public async Task AssembleReportAsync_Assumptions_UsesProtocolDefaults()
    {
        var deal = CreateTestDeal();
        deal.LoanLtv = null;  // Should fall back to protocol default (65%)
        deal.HoldPeriodYears = null;  // Should fall back to 5
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        var assumptions = report.Assumptions.Assumptions;
        var ltvRow = assumptions.Find(a => a.Parameter == "Loan LTV");
        Assert.NotNull(ltvRow);
        Assert.Equal(DataSource.ProtocolDefault, ltvRow.Source);
    }

    [Fact]
    public async Task AssembleReportAsync_Assumptions_UsesUserInput()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        var assumptions = report.Assumptions.Assumptions;
        var ltvRow = assumptions.Find(a => a.Parameter == "Loan LTV");
        Assert.NotNull(ltvRow);
        Assert.Equal(DataSource.UserInput, ltvRow.Source);
    }

    [Fact]
    public async Task AssembleReportAsync_InvalidDealId_ThrowsKeyNotFoundException()
    {
        var assembler = new ReportAssembler(_db);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => assembler.AssembleReportAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AssembleReportAsync_FinancialAnalysis_HasSourcesAndUses()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(7_000_000m, report.FinancialAnalysis.SourcesAndUses.LoanAmount);
        Assert.Equal(10_000_000m, report.FinancialAnalysis.SourcesAndUses.PurchasePrice);
    }

    [Fact]
    public async Task AssembleReportAsync_RiskAssessment_HasSectionNumber9()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(9, report.RiskAssessment.SectionNumber);
    }

    [Fact]
    public async Task AssembleReportAsync_InvestmentDecision_HasSectionNumber10()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(10, report.InvestmentDecision.SectionNumber);
    }

    // === Phase 1: Financial Analysis Completion ===

    [Fact]
    public async Task AssembleReportAsync_FiveYearCashFlow_Has5Years()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(5, report.FinancialAnalysis.FiveYearCashFlow.Count);
        Assert.Equal(1, report.FinancialAnalysis.FiveYearCashFlow[0].Year);
        Assert.Equal(5, report.FinancialAnalysis.FiveYearCashFlow[4].Year);
    }

    [Fact]
    public async Task AssembleReportAsync_FiveYearCashFlow_NoiGrowsOverTime()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        var cf = report.FinancialAnalysis.FiveYearCashFlow;
        Assert.True(cf[1].Noi > cf[0].Noi, "Year 2 NOI should exceed Year 1");
        Assert.True(cf[4].Noi > cf[3].Noi, "Year 5 NOI should exceed Year 4");
    }

    [Fact]
    public async Task AssembleReportAsync_FiveYearCashFlow_DebtServiceConstant()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        var cf = report.FinancialAnalysis.FiveYearCashFlow;
        Assert.True(cf[0].DebtService > 0, "Debt service should be positive");
        Assert.Equal(cf[0].DebtService, cf[4].DebtService);
    }

    [Fact]
    public async Task AssembleReportAsync_FiveYearCashFlow_EgiAndOpExPopulated()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        var year1 = report.FinancialAnalysis.FiveYearCashFlow[0];
        Assert.True(year1.Egi > 0, "Year 1 EGI should be positive");
        Assert.True(year1.OpEx > 0, "Year 1 OpEx should be positive");
        Assert.True(year1.Noi > 0, "Year 1 NOI should be positive");
    }

    [Fact]
    public async Task AssembleReportAsync_Returns_IrrIsPositive()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.True(report.FinancialAnalysis.Returns.Irr > 0,
            "IRR should be positive for a deal with positive cash flows");
    }

    [Fact]
    public async Task AssembleReportAsync_Returns_EquityMultipleAboveOne()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.True(report.FinancialAnalysis.Returns.EquityMultiple > 1.0m,
            "Equity multiple should exceed 1.0x for a deal with positive returns");
    }

    [Fact]
    public async Task AssembleReportAsync_Returns_AverageCashOnCashPopulated()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.True(report.FinancialAnalysis.Returns.AverageCashOnCash != 0m,
            "Average cash-on-cash should be populated");
    }

    [Fact]
    public async Task AssembleReportAsync_Returns_TotalProfitPositive()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.True(report.FinancialAnalysis.Returns.TotalProfit > 0,
            "Total profit should be positive for a profitable deal");
    }

    [Fact]
    public async Task AssembleReportAsync_Exit_HasPositiveExitValue()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.True(report.FinancialAnalysis.Exit.ExitValue > 0, "Exit value should be positive");
        Assert.True(report.FinancialAnalysis.Exit.ExitCapRate > 0, "Exit cap rate should be positive");
        Assert.True(report.FinancialAnalysis.Exit.ExitNoi > 0, "Exit NOI should be positive");
    }

    [Fact]
    public async Task AssembleReportAsync_Exit_NetProceedsPositive()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.True(report.FinancialAnalysis.Exit.NetProceeds > 0,
            "Net proceeds should be positive for a profitable deal");
        Assert.True(report.FinancialAnalysis.Exit.LoanBalance > 0,
            "Loan balance at exit should be positive");
    }
}
