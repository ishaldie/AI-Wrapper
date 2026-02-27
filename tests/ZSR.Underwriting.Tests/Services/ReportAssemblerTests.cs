using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Formatting;
using ZSR.Underwriting.Application.Interfaces;
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

        // Dual-constraint sizing: loan is MIN(LTV-based $7M, DSCR-based)
        // With test deal NOI ~$494k and 1.25x DSCR, DSCR constrains below $7M
        Assert.True(report.CoreMetrics.LoanAmount > 0);
        Assert.True(report.CoreMetrics.LoanAmount <= 7_000_000m);
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

        // Dual-constraint: loan may be DSCR-constrained below LTV amount
        Assert.True(report.FinancialAnalysis.SourcesAndUses.LoanAmount > 0);
        Assert.Equal(10_000_000m, report.FinancialAnalysis.SourcesAndUses.PurchasePrice);
        Assert.Equal(7_000_000m, report.FinancialAnalysis.SourcesAndUses.LtvBasedLoan);
        Assert.NotEmpty(report.FinancialAnalysis.SourcesAndUses.ConstrainingTest);
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

    // === Phase 3: Market Data Wiring ===

    [Fact]
    public async Task AssembleReportAsync_WithMarketData_PropertyCompsEnriched()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var marketService = new StubMarketDataService(CreateTestMarketContext());
        var assembler = new ReportAssembler(_db, marketService);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.DoesNotContain("pending", report.PropertyComps.Narrative, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("comparable", report.PropertyComps.Narrative, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssembleReportAsync_WithMarketData_TenantMarketEnriched()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var marketService = new StubMarketDataService(CreateTestMarketContext());
        var assembler = new ReportAssembler(_db, marketService);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.DoesNotContain("pending", report.TenantMarket.Narrative, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Major employers", report.TenantMarket.Narrative);
    }

    [Fact]
    public async Task AssembleReportAsync_WithMarketData_TenantMarketHasSubjectMetrics()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var marketService = new StubMarketDataService(CreateTestMarketContext());
        var assembler = new ReportAssembler(_db, marketService);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.True(report.TenantMarket.SubjectRentPerUnit > 0);
        Assert.True(report.TenantMarket.SubjectOccupancy > 0);
    }

    [Fact]
    public async Task AssembleReportAsync_WithoutMarketData_FallsBackToPlaceholders()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        // No market service — should still produce a valid report with placeholders
        var assembler = new ReportAssembler(_db);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.NotNull(report.PropertyComps.Narrative);
        Assert.NotNull(report.TenantMarket.Narrative);
    }

    [Fact]
    public async Task AssembleReportAsync_WithNullMarketData_FallsBackToPlaceholders()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        // Market service returns null context
        var marketService = new StubMarketDataService(null);
        var assembler = new ReportAssembler(_db, marketService);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.NotNull(report.PropertyComps.Narrative);
        Assert.NotNull(report.TenantMarket.Narrative);
    }

    [Fact]
    public async Task AssembleReportAsync_WithMarketData_UsesMarketLoanRate()
    {
        var deal = CreateTestDeal();
        deal.LoanRate = null; // No user-provided rate
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var ctx = CreateTestMarketContext();
        ctx.CurrentFannieMaeRate = 5.75m;
        var marketService = new StubMarketDataService(ctx);
        var assembler = new ReportAssembler(_db, marketService);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // Debt service should be calculated (not zero) when market rate available
        Assert.True(report.FinancialAnalysis.FiveYearCashFlow[0].DebtService > 0,
            "Debt service should use market rate when user rate is null");
    }

    [Fact]
    public async Task AssembleReportAsync_WithSalesCompExtractor_PopulatesComps()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var marketService = new StubMarketDataService(CreateTestMarketContext());
        var compExtractor = new StubSalesCompExtractor(new SalesCompResult
        {
            Comps = [new() { Address = "100 Oak Park Dr", SalePrice = 11_400_000m, Units = 120, PricePerUnit = 95_000m, CapRate = 5.5m }],
            Adjustments = [new() { Factor = "Unit Count", Adjustment = "+5%", Rationale = "Fewer units" }]
        });
        var assembler = new ReportAssembler(_db, marketService, compExtractor);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Single(report.PropertyComps.Comps);
        Assert.Equal("100 Oak Park Dr", report.PropertyComps.Comps[0].Address);
        Assert.Single(report.PropertyComps.Adjustments);
        Assert.Equal("Unit Count", report.PropertyComps.Adjustments[0].Factor);
    }

    [Fact]
    public async Task AssembleReportAsync_WithPublicDataService_PopulatesPublicData()
    {
        var deal = CreateTestDeal();
        deal.Address = "123 Main St, Dallas, TX 75201"; // Address with zip code
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var publicDataService = new StubPublicDataService(
            new CensusData { MedianHouseholdIncome = 65_000, TotalPopulation = 45_000, MedianAge = 35.2m, ZipCode = "75201" },
            new BlsData { UnemploymentRate = 3.8m, AreaName = "Dallas" },
            new FredData { CpiAllItems = 315.5m });
        var assembler = new ReportAssembler(_db, publicDataService: publicDataService);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.NotNull(report.PublicData);
        Assert.NotNull(report.PublicData!.Census);
        Assert.Equal(65_000m, report.PublicData.Census!.MedianHouseholdIncome);
        Assert.NotNull(report.PublicData.Bls);
        Assert.Equal(3.8m, report.PublicData.Bls!.UnemploymentRate);
    }

    // === Phase 7: Report Prose Generation ===

    private static GeneratedProse CreateTestProse()
    {
        return new GeneratedProse
        {
            ExecutiveSummaryNarrative = "This is an AI-generated executive summary for the deal.",
            KeyHighlights = ["Strong NOI growth", "Below-market rents", "Value-add opportunity"],
            KeyRisks = ["Rising interest rates", "Deferred maintenance"],

            MarketContextNarrative = "The Dallas metro market shows strong fundamentals.",
            ValueCreationNarrative = "Interior renovations will drive rent premiums of $150-200/unit.",

            RiskAssessmentNarrative = "The primary risks relate to market conditions and execution.",
            Risks =
            [
                new RiskItem { Category = "Market", Description = "Rising rates may compress cap rates", Severity = RiskSeverity.Medium, Mitigation = "Lock rate early" },
                new RiskItem { Category = "Execution", Description = "Renovation timeline risk", Severity = RiskSeverity.Low, Mitigation = "Phased approach" }
            ],

            Decision = InvestmentDecisionType.Go,
            InvestmentThesis = "Strong risk-adjusted returns driven by below-market rents and value-add potential.",
            Conditions = ["Complete Phase I ESA", "Verify rent roll"],
            NextSteps = ["Submit LOI", "Engage lender", "Order appraisal"],

            PropertyOverviewNarrative = "A well-located 100-unit multifamily property.",

            TotalInputTokens = 5000,
            TotalOutputTokens = 3000,
            FailedSections = null
        };
    }

    [Fact]
    public async Task AssembleReportAsync_WithProseGenerator_ExecutiveSummaryHasAINarrative()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var prose = CreateTestProse();
        var proseGen = new StubProseGenerator(prose);
        var assembler = new ReportAssembler(_db, proseGenerator: proseGen);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(prose.ExecutiveSummaryNarrative, report.ExecutiveSummary.Narrative);
        Assert.DoesNotContain("pending", report.ExecutiveSummary.Narrative, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssembleReportAsync_WithProseGenerator_ExecutiveSummaryHasHighlightsAndRisks()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var prose = CreateTestProse();
        var proseGen = new StubProseGenerator(prose);
        var assembler = new ReportAssembler(_db, proseGenerator: proseGen);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(3, report.ExecutiveSummary.KeyHighlights.Count);
        Assert.Contains("Strong NOI growth", report.ExecutiveSummary.KeyHighlights);
        Assert.Equal(2, report.ExecutiveSummary.KeyRisks.Count);
        Assert.Contains("Rising interest rates", report.ExecutiveSummary.KeyRisks);
    }

    [Fact]
    public async Task AssembleReportAsync_WithProseGenerator_ValueCreationHasAINarrative()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var prose = CreateTestProse();
        var proseGen = new StubProseGenerator(prose);
        var assembler = new ReportAssembler(_db, proseGenerator: proseGen);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(prose.ValueCreationNarrative, report.ValueCreation.Narrative);
        Assert.DoesNotContain("pending", report.ValueCreation.Narrative, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssembleReportAsync_WithProseGenerator_RiskAssessmentHasAINarrativeAndRisks()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var prose = CreateTestProse();
        var proseGen = new StubProseGenerator(prose);
        var assembler = new ReportAssembler(_db, proseGenerator: proseGen);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(prose.RiskAssessmentNarrative, report.RiskAssessment.Narrative);
        Assert.Equal(2, report.RiskAssessment.Risks.Count);
        Assert.Equal("Market", report.RiskAssessment.Risks[0].Category);
    }

    [Fact]
    public async Task AssembleReportAsync_WithProseGenerator_InvestmentDecisionMapsCorrectly()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var prose = CreateTestProse();
        var proseGen = new StubProseGenerator(prose);
        var assembler = new ReportAssembler(_db, proseGenerator: proseGen);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.Equal(InvestmentDecisionType.Go, report.InvestmentDecision.Decision);
        Assert.Equal("GO", report.InvestmentDecision.DecisionLabel);
        Assert.Equal(prose.InvestmentThesis, report.InvestmentDecision.InvestmentThesis);
        Assert.Equal(3, report.InvestmentDecision.NextSteps.Count);
        Assert.Equal(2, report.InvestmentDecision.Conditions.Count);
    }

    [Fact]
    public async Task AssembleReportAsync_ProseGeneratorThrows_FallsBackToPlaceholders()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var proseGen = new StubProseGenerator(new InvalidOperationException("API timeout"));
        var assembler = new ReportAssembler(_db, proseGenerator: proseGen);
        var report = await assembler.AssembleReportAsync(deal.Id);

        // Should fall back to placeholder text, not crash
        Assert.Contains("pending", report.ExecutiveSummary.Narrative, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pending", report.RiskAssessment.Narrative, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssembleReportAsync_WithProseGenerator_ProseContextIncludesMarketData()
    {
        var deal = CreateTestDeal();
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        ProseGenerationContext? capturedContext = null;
        var prose = CreateTestProse();
        var proseGen = new CapturingProseGenerator(prose, ctx => capturedContext = ctx);
        var marketService = new StubMarketDataService(CreateTestMarketContext());
        var assembler = new ReportAssembler(_db, marketService, proseGenerator: proseGen);
        var report = await assembler.AssembleReportAsync(deal.Id);

        Assert.NotNull(capturedContext);
        Assert.NotNull(capturedContext!.MarketContext);
        Assert.Equal(deal.Id, capturedContext.Deal.Id);
    }

    private static MarketContextDto CreateTestMarketContext()
    {
        return new MarketContextDto
        {
            MajorEmployers = [new() { Name = "Acme Corp", Description = "Major tech employer", SourceUrl = "https://example.com" }],
            EconomicDrivers = [new() { Name = "Tech Growth", Description = "Strong tech sector expansion", SourceUrl = "https://example.com" }],
            ConstructionPipeline = [new() { Name = "New Complex", Description = "200-unit development", SourceUrl = "https://example.com" }],
            ComparableTransactions = [new() { Name = "Oak Park Apts", Description = "$95K/unit, 5.5% cap", SourceUrl = "https://example.com" }],
            CurrentFannieMaeRate = 5.75m,
            SourceUrls = new Dictionary<string, List<string>>
            {
                ["MajorEmployers"] = ["https://example.com/employers"],
                ["ComparableTransactions"] = ["https://example.com/comps"]
            },
            RetrievedAt = DateTime.UtcNow
        };
    }
}

internal class StubMarketDataService : IMarketDataService
{
    private readonly MarketContextDto? _context;

    public StubMarketDataService(MarketContextDto? context)
    {
        _context = context;
    }

    public Task<MarketContextDto> GetMarketContextForDealAsync(Guid dealId, string city, string state)
        => Task.FromResult(_context ?? new MarketContextDto());

    public Task<MarketContextDto> GetMarketContextAsync(string city, string state)
        => Task.FromResult(_context ?? new MarketContextDto());
}

internal class StubProseGenerator : IReportProseGenerator
{
    private readonly GeneratedProse? _prose;
    private readonly Exception? _exception;

    public StubProseGenerator(GeneratedProse? prose)
    {
        _prose = prose;
    }

    public StubProseGenerator(Exception exception)
    {
        _exception = exception;
    }

    public Task<GeneratedProse> GenerateAllProseAsync(ProseGenerationContext context, CancellationToken ct = default)
    {
        if (_exception != null)
            throw _exception;
        return Task.FromResult(_prose!);
    }
}

internal class CapturingProseGenerator : IReportProseGenerator
{
    private readonly GeneratedProse _prose;
    private readonly Action<ProseGenerationContext> _capture;

    public CapturingProseGenerator(GeneratedProse prose, Action<ProseGenerationContext> capture)
    {
        _prose = prose;
        _capture = capture;
    }

    public Task<GeneratedProse> GenerateAllProseAsync(ProseGenerationContext context, CancellationToken ct = default)
    {
        _capture(context);
        return Task.FromResult(_prose);
    }
}

internal class StubSalesCompExtractor : ISalesCompExtractor
{
    private readonly SalesCompResult _result;

    public StubSalesCompExtractor(SalesCompResult result)
    {
        _result = result;
    }

    public Task<SalesCompResult> ExtractCompsAsync(
        MarketContextDto marketContext, string subjectAddress, decimal subjectPricePerUnit,
        int subjectUnits, string? userId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(_result);
}
