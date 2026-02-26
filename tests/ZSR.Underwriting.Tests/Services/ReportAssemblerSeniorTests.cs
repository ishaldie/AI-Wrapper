using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class ReportAssemblerSeniorTests : IDisposable
{
    private readonly AppDbContext _db;

    public ReportAssemblerSeniorTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose() => _db.Dispose();

    private Deal CreateSeniorDeal()
    {
        var deal = new Deal("Senior Test");
        deal.PropertyName = "Sunrise Senior Living";
        deal.Address = "456 Oak Dr, Austin, TX";
        deal.PropertyType = PropertyType.AssistedLiving;
        deal.LicensedBeds = 120;
        deal.AverageDailyRate = 250m;
        deal.PurchasePrice = 15_000_000m;
        deal.UnitCount = 0; // Senior deals use beds, not units
        deal.LoanLtv = 65m;
        deal.LoanRate = 6.5m;
        deal.IsInterestOnly = false;
        deal.AmortizationYears = 30;
        deal.LoanTermYears = 5;
        deal.HoldPeriodYears = 5;
        deal.TargetOccupancy = 87m;
        return deal;
    }

    private Deal CreateMultifamilyDeal()
    {
        var deal = new Deal("MF Test");
        deal.PropertyName = "Sunset Apartments";
        deal.Address = "123 Main St, Dallas, TX";
        deal.PropertyType = PropertyType.Multifamily;
        deal.UnitCount = 100;
        deal.PurchasePrice = 10_000_000m;
        deal.RentRollSummary = 1000m;
        deal.T12Summary = 800_000m;
        deal.LoanLtv = 70m;
        deal.LoanRate = 6.5m;
        deal.IsInterestOnly = false;
        deal.AmortizationYears = 30;
        deal.LoanTermYears = 5;
        deal.HoldPeriodYears = 5;
        deal.TargetOccupancy = 95m;
        return deal;
    }

    [Fact]
    public async Task SeniorDeal_Revenue_UsesBedTimesAdrTimes365()
    {
        var deal = CreateSeniorDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // GPR = 120 beds × $250/day × 365 = $10,950,000
        var expectedGpr = 120m * 250m * 365m;
        var gprRow = report.Operations.RevenueItems.Find(r => r.LineItem == "Gross Potential Rent");
        Assert.NotNull(gprRow);
        Assert.Equal(expectedGpr, gprRow.Annual);
    }

    [Fact]
    public async Task SeniorDeal_Occupancy_UsesTypeDefault()
    {
        var deal = CreateSeniorDeal();
        deal.TargetOccupancy = null; // Use AL default of 87%
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // Vacancy loss should reflect 87% occupancy (13% vacancy)
        var gprRow = report.Operations.RevenueItems.Find(r => r.LineItem == "Gross Potential Rent");
        var vacRow = report.Operations.RevenueItems.Find(r => r.LineItem == "Vacancy Loss");
        Assert.NotNull(gprRow);
        Assert.NotNull(vacRow);
        var expectedVacancy = gprRow.Annual * (1m - 87m / 100m);
        Assert.Equal(-expectedVacancy, vacRow.Annual, 2);
    }

    [Fact]
    public async Task SeniorDeal_OpExRatio_UsesTypeSpecificDefault()
    {
        var deal = CreateSeniorDeal();
        deal.T12Summary = null; // Force estimated opex
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // AL opex ratio = 68%
        var egi = report.Operations.TotalRevenue;
        var expectedOpEx = egi * 0.68m;
        Assert.Equal(expectedOpEx, report.Operations.TotalExpenses, 2);
    }

    [Fact]
    public async Task SeniorDeal_OtherIncomeRatio_Uses5Percent()
    {
        var deal = CreateSeniorDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // Senior other income = 5% of net rent
        var netRentRow = report.Operations.RevenueItems.Find(r => r.LineItem == "Net Rental Income");
        var otherRow = report.Operations.RevenueItems.Find(r => r.LineItem == "Other Income");
        Assert.NotNull(netRentRow);
        Assert.NotNull(otherRow);
        var expected = netRentRow.Annual * 0.05m;
        Assert.Equal(expected, otherRow.Annual, 2);
    }

    [Fact]
    public async Task SeniorDeal_CoreMetrics_ShowsBedsAndPricePerBed()
    {
        var deal = CreateSeniorDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // UnitCount on CoreMetrics should be the bed count
        Assert.Equal(120, report.CoreMetrics.UnitCount);
        // Price per bed = $15M / 120 = $125,000
        Assert.Equal(125_000m, report.CoreMetrics.PricePerUnit);

        // Verify labels
        var sizeMetric = report.CoreMetrics.Metrics.Find(m => m.Label == "Licensed Beds");
        Assert.NotNull(sizeMetric);
        var priceMetric = report.CoreMetrics.Metrics.Find(m => m.Label == "Price/Bed");
        Assert.NotNull(priceMetric);
    }

    [Fact]
    public async Task MultifamilyDeal_StillUsesUnitsAndMultifamilyRatios()
    {
        var deal = CreateMultifamilyDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // Multifamily: GPR = 100 units × $1000/mo × 12 = $1,200,000
        var expectedGpr = 100m * 1000m * 12m;
        var gprRow = report.Operations.RevenueItems.Find(r => r.LineItem == "Gross Potential Rent");
        Assert.NotNull(gprRow);
        Assert.Equal(expectedGpr, gprRow.Annual);

        // Unit-based labels
        Assert.Equal(100, report.CoreMetrics.UnitCount);
        var sizeMetric = report.CoreMetrics.Metrics.Find(m => m.Label == "Unit Count");
        Assert.NotNull(sizeMetric);
        var priceMetric = report.CoreMetrics.Metrics.Find(m => m.Label == "Price/Unit");
        Assert.NotNull(priceMetric);
    }

    [Fact]
    public async Task SeniorDeal_NoAdr_FallsBackToMultifamilyRevenuePath()
    {
        var deal = CreateSeniorDeal();
        deal.AverageDailyRate = null; // No ADR → falls back to MF formula
        deal.RentRollSummary = 800m;
        deal.UnitCount = 80;
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // Fallback: GPR = 80 units × $800/mo × 12 = $768,000
        var expectedGpr = 80m * 800m * 12m;
        var gprRow = report.Operations.RevenueItems.Find(r => r.LineItem == "Gross Potential Rent");
        Assert.NotNull(gprRow);
        Assert.Equal(expectedGpr, gprRow.Annual);
    }

    [Fact]
    public async Task SeniorDeal_CapRate_CalculatedFromBedsRevenue()
    {
        var deal = CreateSeniorDeal();
        deal.T12Summary = null; // Use estimated opex
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // Cap rate = NOI / Purchase Price × 100
        Assert.True(report.CoreMetrics.CapRate > 0, "Cap rate should be positive for senior deal");
        var expectedCapRate = report.Operations.Noi / deal.PurchasePrice * 100m;
        Assert.Equal(expectedCapRate, report.CoreMetrics.CapRate, 2);
    }

    [Fact]
    public async Task SkilledNursing_UsesSnfOpExRatio()
    {
        var deal = CreateSeniorDeal();
        deal.PropertyType = PropertyType.SkilledNursing;
        deal.TargetOccupancy = null; // SNF default = 82%
        deal.T12Summary = null; // Force estimated opex (75%)
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // SNF opex ratio = 75%
        var egi = report.Operations.TotalRevenue;
        var expectedOpEx = egi * 0.75m;
        Assert.Equal(expectedOpEx, report.Operations.TotalExpenses, 2);
    }
}
