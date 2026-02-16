using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Claude;

public class PromptBuilderTests
{
    private static ProseGenerationContext CreateFullContext()
    {
        var deal = new Deal("Sunrise Apartments");
        deal.PropertyName = "Sunrise Apartments";
        deal.Address = "123 Main St, Dallas TX 75201";
        deal.UnitCount = 120;
        deal.PurchasePrice = 12_000_000m;
        deal.LoanLtv = 65m;
        deal.LoanRate = 5.5m;
        deal.IsInterestOnly = false;
        deal.AmortizationYears = 30;
        deal.LoanTermYears = 5;
        deal.HoldPeriodYears = 5;
        deal.CapexBudget = 500_000m;
        deal.TargetOccupancy = 95m;
        deal.ValueAddPlans = "Unit renovations, amenity upgrades, RUBS implementation";

        var calc = new CalculationResult(deal.Id)
        {
            GrossPotentialRent = 1_728_000m,
            VacancyLoss = 86_400m,
            EffectiveGrossIncome = 1_863_360m,
            NetOperatingIncome = 840_000m,
            NoiMargin = 45.1m,
            GoingInCapRate = 7.0m,
            ExitCapRate = 7.5m,
            PricePerUnit = 100_000m,
            LoanAmount = 7_800_000m,
            AnnualDebtService = 531_000m,
            DebtServiceCoverageRatio = 1.58m,
            CashOnCashReturn = 7.35m,
            InternalRateOfReturn = 18.5m,
            EquityMultiple = 2.1m,
            ExitValue = 14_000_000m,
            TotalProfit = 4_200_000m
        };

        var realAi = new RealAiData(deal.Id)
        {
            InPlaceRent = 1_150m,
            Occupancy = 92m,
            YearBuilt = 1998,
            BuildingType = "Garden",
            SquareFootage = 96_000,
            MarketCapRate = 6.5m,
            RentGrowth = 3.5m,
            JobGrowth = 2.5m,
            NetMigration = 15_000,
            Permits = 8_500,
            AverageFico = 720,
            RentToIncomeRatio = 28m,
            MedianHhi = 55_000m
        };

        var marketContext = new MarketContextDto
        {
            MajorEmployers =
            [
                new() { Name = "AT&T", Description = "Headquarters" },
                new() { Name = "Toyota", Description = "North American HQ" }
            ],
            ConstructionPipeline =
            [
                new() { Name = "The Vue", Description = "200 units under construction" }
            ]
        };

        return new ProseGenerationContext
        {
            Deal = deal,
            Calculations = calc,
            RealAiData = realAi,
            MarketContext = marketContext
        };
    }

    private static ProseGenerationContext CreateMinimalContext()
    {
        var deal = new Deal("Basic Deal");
        deal.PropertyName = "Basic Deal";
        deal.Address = "456 Elm St, Houston TX";
        deal.UnitCount = 50;
        deal.PurchasePrice = 5_000_000m;

        return new ProseGenerationContext { Deal = deal };
    }

    private readonly UnderwritingPromptBuilder _builder = new();

    // === Executive Summary ===

    [Fact]
    public void BuildExecutiveSummaryPrompt_ReturnsNonEmptyRequest()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.False(string.IsNullOrWhiteSpace(result.SystemPrompt));
        Assert.False(string.IsNullOrWhiteSpace(result.UserMessage));
    }

    [Fact]
    public void BuildExecutiveSummaryPrompt_IncludesPropertyInfo()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.Contains("Sunrise Apartments", result.UserMessage);
        Assert.Contains("Dallas", result.UserMessage);
        Assert.Contains("120", result.UserMessage);
    }

    [Fact]
    public void BuildExecutiveSummaryPrompt_IncludesFinancialMetrics()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.Contains("12,000,000", result.UserMessage);
        Assert.Contains("18.5", result.UserMessage); // IRR
        Assert.Contains("1.58", result.UserMessage); // DSCR
        Assert.Contains("7.0", result.UserMessage); // Cap rate
    }

    [Fact]
    public void BuildExecutiveSummaryPrompt_SystemPromptSetsAnalystRole()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.Contains("underwriting", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildExecutiveSummaryPrompt_WorksWithMinimalContext()
    {
        var ctx = CreateMinimalContext();
        var result = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.False(string.IsNullOrWhiteSpace(result.UserMessage));
        Assert.Contains("Basic Deal", result.UserMessage);
    }

    // === Market Context ===

    [Fact]
    public void BuildMarketContextPrompt_IncludesMarketData()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildMarketContextPrompt(ctx);

        Assert.Contains("3.5", result.UserMessage); // Rent growth
        Assert.Contains("2.5", result.UserMessage); // Job growth
        Assert.Contains("15,000", result.UserMessage); // Net migration
    }

    [Fact]
    public void BuildMarketContextPrompt_IncludesEmployers()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildMarketContextPrompt(ctx);

        Assert.Contains("AT&T", result.UserMessage);
        Assert.Contains("Toyota", result.UserMessage);
    }

    [Fact]
    public void BuildMarketContextPrompt_WorksWithMinimalContext()
    {
        var ctx = CreateMinimalContext();
        var result = _builder.BuildMarketContextPrompt(ctx);

        Assert.False(string.IsNullOrWhiteSpace(result.UserMessage));
    }

    // === Value Creation Strategy ===

    [Fact]
    public void BuildValueCreationPrompt_IncludesValueAddPlans()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildValueCreationPrompt(ctx);

        Assert.Contains("Unit renovations", result.UserMessage);
        Assert.Contains("amenity upgrades", result.UserMessage);
        Assert.Contains("RUBS", result.UserMessage);
    }

    [Fact]
    public void BuildValueCreationPrompt_IncludesCapexBudget()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildValueCreationPrompt(ctx);

        Assert.Contains("500,000", result.UserMessage);
    }

    [Fact]
    public void BuildValueCreationPrompt_IncludesRentUpside()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildValueCreationPrompt(ctx);

        // Should reference current vs market rents
        Assert.Contains("1,150", result.UserMessage); // In-place rent from RealAI
    }

    // === Risk Assessment ===

    [Fact]
    public void BuildRiskAssessmentPrompt_IncludesFinancialMetrics()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        Assert.Contains("1.58", result.UserMessage); // DSCR
        Assert.Contains("18.5", result.UserMessage); // IRR
    }

    [Fact]
    public void BuildRiskAssessmentPrompt_IncludesMarketRisk()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        Assert.Contains("8,500", result.UserMessage); // Permits (supply risk)
    }

    [Fact]
    public void BuildRiskAssessmentPrompt_SystemPromptRequestsStructuredOutput()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        // System prompt should request risks with severity levels
        Assert.Contains("severity", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    // === Investment Decision ===

    [Fact]
    public void BuildInvestmentDecisionPrompt_IncludesThresholds()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildInvestmentDecisionPrompt(ctx);

        // Should reference protocol thresholds
        Assert.Contains("15", result.UserMessage); // IRR threshold
        Assert.Contains("1.5", result.UserMessage); // DSCR threshold
    }

    [Fact]
    public void BuildInvestmentDecisionPrompt_IncludesActualMetrics()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildInvestmentDecisionPrompt(ctx);

        Assert.Contains("18.5", result.UserMessage); // Actual IRR
        Assert.Contains("1.58", result.UserMessage); // Actual DSCR
    }

    [Fact]
    public void BuildInvestmentDecisionPrompt_RequestsGoNoGoDecision()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildInvestmentDecisionPrompt(ctx);

        Assert.Contains("GO", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    // === Property Overview ===

    [Fact]
    public void BuildPropertyOverviewPrompt_IncludesPropertyDetails()
    {
        var ctx = CreateFullContext();
        var result = _builder.BuildPropertyOverviewPrompt(ctx);

        Assert.Contains("Sunrise Apartments", result.UserMessage);
        Assert.Contains("123 Main St", result.UserMessage);
        Assert.Contains("120", result.UserMessage); // Units
        Assert.Contains("1998", result.UserMessage); // Year built
        Assert.Contains("Garden", result.UserMessage); // Building type
    }

    [Fact]
    public void BuildPropertyOverviewPrompt_WorksWithMinimalContext()
    {
        var ctx = CreateMinimalContext();
        var result = _builder.BuildPropertyOverviewPrompt(ctx);

        Assert.Contains("Basic Deal", result.UserMessage);
        Assert.Contains("456 Elm St", result.UserMessage);
    }

    // === All prompts set MaxTokens ===

    [Fact]
    public void AllPrompts_SetMaxTokens()
    {
        var ctx = CreateFullContext();

        Assert.NotNull(_builder.BuildExecutiveSummaryPrompt(ctx).MaxTokens);
        Assert.NotNull(_builder.BuildMarketContextPrompt(ctx).MaxTokens);
        Assert.NotNull(_builder.BuildValueCreationPrompt(ctx).MaxTokens);
        Assert.NotNull(_builder.BuildRiskAssessmentPrompt(ctx).MaxTokens);
        Assert.NotNull(_builder.BuildInvestmentDecisionPrompt(ctx).MaxTokens);
        Assert.NotNull(_builder.BuildPropertyOverviewPrompt(ctx).MaxTokens);
    }
}
