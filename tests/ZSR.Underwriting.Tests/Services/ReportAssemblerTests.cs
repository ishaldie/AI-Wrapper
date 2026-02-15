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
}
