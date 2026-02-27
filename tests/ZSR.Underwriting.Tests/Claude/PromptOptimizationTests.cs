using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Claude;

public class PromptOptimizationTests
{
    private readonly UnderwritingPromptBuilder _builder = new();

    private static ProseGenerationContext CreateContext(PropertyType type = PropertyType.Multifamily)
    {
        var deal = new Deal("Optimization Test")
        {
            PropertyName = "Test Property",
            Address = "100 Main St, Dallas TX",
            PurchasePrice = 10_000_000m,
            UnitCount = 100,
            PropertyType = type,
            LicensedBeds = type == PropertyType.AssistedLiving ? 80 : null,
        };
        var calc = new CalculationResult(deal.Id)
        {
            NetOperatingIncome = 800_000m,
            GoingInCapRate = 8.0m,
            DebtServiceCoverageRatio = 1.50m,
            InternalRateOfReturn = 16.0m,
        };
        return new ProseGenerationContext { Deal = deal, Calculations = calc };
    }

    // === Phase 1: All system prompts share the institutional-quality suffix ===

    [Theory]
    [InlineData(PropertyType.Multifamily)]
    [InlineData(PropertyType.AssistedLiving)]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    public void AllPropertyTypes_SystemPrompt_ContainsInstitutionalQualitySuffix(PropertyType type)
    {
        var ctx = CreateContext(type);
        var prompts = new[]
        {
            _builder.BuildExecutiveSummaryPrompt(ctx),
            _builder.BuildMarketContextPrompt(ctx),
            _builder.BuildValueCreationPrompt(ctx),
            _builder.BuildRiskAssessmentPrompt(ctx),
            _builder.BuildInvestmentDecisionPrompt(ctx),
            _builder.BuildPropertyOverviewPrompt(ctx),
        };

        foreach (var prompt in prompts)
        {
            Assert.Contains("institutional-quality", prompt.SystemPrompt);
            Assert.Contains("precise", prompt.SystemPrompt);
            Assert.Contains("Do not use markdown headers", prompt.SystemPrompt);
        }
    }

    [Theory]
    [InlineData(PropertyType.Multifamily, "multifamily")]
    [InlineData(PropertyType.AssistedLiving, "senior housing")]
    [InlineData(PropertyType.Bridge, "bridge")]
    [InlineData(PropertyType.Hospitality, "hospitality")]
    [InlineData(PropertyType.Commercial, "commercial")]
    [InlineData(PropertyType.LIHTC, "affordable")]
    public void SystemPrompt_ContainsPropertyTypeSpecificIntro(PropertyType type, string expectedKeyword)
    {
        var ctx = CreateContext(type);
        var prompt = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.Contains(expectedKeyword, prompt.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SystemPrompt_SectionFocus_VariesByPromptMethod()
    {
        var ctx = CreateContext();

        var exec = _builder.BuildExecutiveSummaryPrompt(ctx);
        var market = _builder.BuildMarketContextPrompt(ctx);
        var value = _builder.BuildValueCreationPrompt(ctx);
        var risk = _builder.BuildRiskAssessmentPrompt(ctx);
        var invest = _builder.BuildInvestmentDecisionPrompt(ctx);
        var overview = _builder.BuildPropertyOverviewPrompt(ctx);

        Assert.Contains("executive summary", exec.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("market context", market.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value creation", value.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("risk assessment", risk.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GO", invest.SystemPrompt);
        Assert.Contains("property overview", overview.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    // === Phase 2: Conciseness directive appears in all user messages ===

    [Fact]
    public void AllPrompts_UserMessage_ContainsConcisenesDirective()
    {
        var ctx = CreateContext();
        var prompts = new[]
        {
            _builder.BuildExecutiveSummaryPrompt(ctx),
            _builder.BuildMarketContextPrompt(ctx),
            _builder.BuildValueCreationPrompt(ctx),
            _builder.BuildRiskAssessmentPrompt(ctx),
            _builder.BuildInvestmentDecisionPrompt(ctx),
            _builder.BuildPropertyOverviewPrompt(ctx),
        };

        foreach (var prompt in prompts)
        {
            Assert.Contains("2-3 sentences", prompt.UserMessage);
            Assert.Contains("No preamble", prompt.UserMessage);
        }
    }

    // === Phase 2: max_tokens reduced to tighter caps ===

    [Fact]
    public void ExecutiveSummary_MaxTokens_Is1024()
    {
        var ctx = CreateContext();
        Assert.Equal(1024, _builder.BuildExecutiveSummaryPrompt(ctx).MaxTokens);
    }

    [Fact]
    public void MarketContext_MaxTokens_Is1024()
    {
        var ctx = CreateContext();
        Assert.Equal(1024, _builder.BuildMarketContextPrompt(ctx).MaxTokens);
    }

    [Fact]
    public void ValueCreation_MaxTokens_Is1024()
    {
        var ctx = CreateContext();
        Assert.Equal(1024, _builder.BuildValueCreationPrompt(ctx).MaxTokens);
    }

    [Fact]
    public void RiskAssessment_MaxTokens_Is1536()
    {
        var ctx = CreateContext();
        Assert.Equal(1536, _builder.BuildRiskAssessmentPrompt(ctx).MaxTokens);
    }

    [Fact]
    public void InvestmentDecision_MaxTokens_Is1024()
    {
        var ctx = CreateContext();
        Assert.Equal(1024, _builder.BuildInvestmentDecisionPrompt(ctx).MaxTokens);
    }

    [Fact]
    public void PropertyOverview_MaxTokens_Is256()
    {
        var ctx = CreateContext();
        Assert.Equal(256, _builder.BuildPropertyOverviewPrompt(ctx).MaxTokens);
    }

    [Fact]
    public void TotalMaxTokens_DoesNotExceed6400()
    {
        var ctx = CreateContext();
        var total =
            _builder.BuildExecutiveSummaryPrompt(ctx).MaxTokens +
            _builder.BuildMarketContextPrompt(ctx).MaxTokens +
            _builder.BuildValueCreationPrompt(ctx).MaxTokens +
            _builder.BuildRiskAssessmentPrompt(ctx).MaxTokens +
            _builder.BuildInvestmentDecisionPrompt(ctx).MaxTokens +
            _builder.BuildPropertyOverviewPrompt(ctx).MaxTokens;

        Assert.True(total <= 6400, $"Total max_tokens {total} exceeds 6,400 budget");
    }
}
