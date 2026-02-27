using System.Text.Json;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Claude;

public class FanniePromptBuilderTests
{
    private readonly UnderwritingPromptBuilder _builder = new();

    // === Helper: create a Fannie Mae deal context ===

    private static ProseGenerationContext CreateFannieContext(
        FannieProductType productType,
        FannieComplianceResult? compliance = null)
    {
        var deal = new Deal("Fannie Test Deal");
        deal.PropertyName = "Maple Ridge Apartments";
        deal.Address = "500 Oak Blvd, Atlanta GA 30303";
        deal.UnitCount = 200;
        deal.PurchasePrice = 25_000_000m;
        deal.ExecutionType = ExecutionType.FannieMae;
        deal.FannieProductType = productType;

        var calc = new CalculationResult(deal.Id)
        {
            NetOperatingIncome = 1_900_000m,
            GoingInCapRate = 7.6m,
            LoanAmount = 18_750_000m,
            AnnualDebtService = 1_275_000m,
            DebtServiceCoverageRatio = 1.49m,
            CashOnCashReturn = 10.0m,
            InternalRateOfReturn = 17.5m,
            EquityMultiple = 2.0m,
            ExitValue = 28_000_000m,
            TotalProfit = 6_250_000m
        };

        if (compliance != null)
            calc.FannieComplianceJson = JsonSerializer.Serialize(compliance);

        return new ProseGenerationContext
        {
            Deal = deal,
            Calculations = calc
        };
    }

    private static FannieComplianceResult CreatePassingCompliance() => new()
    {
        OverallPass = true,
        ProductMinDscr = 1.25m,
        ProductMaxLtvPercent = 80m,
        ProductMaxAmortYears = 30,
        DscrTest = new ComplianceTest { Name = "DSCR", Pass = true, ActualValue = 1.49m, RequiredValue = 1.25m },
        LtvTest = new ComplianceTest { Name = "LTV", Pass = true, ActualValue = 75m, RequiredValue = 80m },
        AmortizationTest = new ComplianceTest { Name = "Amortization", Pass = true, ActualValue = 30, RequiredValue = 30 }
    };

    private static FannieComplianceResult CreateSeniorsCompliance() => new()
    {
        OverallPass = true,
        ProductMinDscr = 1.40m,
        ProductMaxLtvPercent = 75m,
        ProductMaxAmortYears = 30,
        DscrTest = new ComplianceTest { Name = "DSCR", Pass = true, ActualValue = 1.49m, RequiredValue = 1.40m },
        LtvTest = new ComplianceTest { Name = "LTV", Pass = true, ActualValue = 70m, RequiredValue = 75m },
        AmortizationTest = new ComplianceTest { Name = "Amortization", Pass = true, ActualValue = 30, RequiredValue = 30 },
        SeniorsBlendedDscrTest = new ComplianceTest { Name = "Blended DSCR", Pass = true, ActualValue = 1.45m, RequiredValue = 1.40m },
        SnfNcfCapTest = new ComplianceTest { Name = "SNF NCF Cap", Pass = true, ActualValue = 12m, RequiredValue = 20m }
    };

    private static FannieComplianceResult CreateCoopCompliance() => new()
    {
        OverallPass = true,
        ProductMinDscr = 1.00m,
        ProductMaxLtvPercent = 55m,
        ProductMaxAmortYears = 30,
        DscrTest = new ComplianceTest { Name = "DSCR", Pass = true, ActualValue = 1.10m, RequiredValue = 1.00m },
        LtvTest = new ComplianceTest { Name = "LTV", Pass = true, ActualValue = 50m, RequiredValue = 55m },
        AmortizationTest = new ComplianceTest { Name = "Amortization", Pass = true, ActualValue = 30, RequiredValue = 30 },
        CoopActualDscrTest = new ComplianceTest { Name = "Co-op Actual Ops DSCR", Pass = true, ActualValue = 1.10m, RequiredValue = 1.00m },
        CoopMarketRentalDscrTest = new ComplianceTest { Name = "Co-op Market Rental DSCR", Pass = true, ActualValue = 1.60m, RequiredValue = 1.55m }
    };

    private static FannieComplianceResult CreateSarmCompliance() => new()
    {
        OverallPass = false,
        ProductMinDscr = 1.05m,
        ProductMaxLtvPercent = 65m,
        ProductMaxAmortYears = 30,
        DscrTest = new ComplianceTest { Name = "DSCR", Pass = true, ActualValue = 1.20m, RequiredValue = 1.05m },
        LtvTest = new ComplianceTest { Name = "LTV", Pass = true, ActualValue = 60m, RequiredValue = 65m },
        AmortizationTest = new ComplianceTest { Name = "Amortization", Pass = true, ActualValue = 30, RequiredValue = 30 },
        SarmStressDscrTest = new ComplianceTest { Name = "SARM Stress DSCR", Pass = false, ActualValue = 0.98m, RequiredValue = 1.05m, Notes = "DSCR at max note rate fails minimum" }
    };

    private static FannieComplianceResult CreateStudentCompliance() => new()
    {
        OverallPass = true,
        ProductMinDscr = 1.30m,
        ProductMaxLtvPercent = 75m,
        ProductMaxAmortYears = 30,
        DscrTest = new ComplianceTest { Name = "DSCR", Pass = true, ActualValue = 1.40m, RequiredValue = 1.30m },
        LtvTest = new ComplianceTest { Name = "LTV", Pass = true, ActualValue = 70m, RequiredValue = 75m },
        AmortizationTest = new ComplianceTest { Name = "Amortization", Pass = true, ActualValue = 30, RequiredValue = 30 }
    };

    // === Task 5: BuildFannieComplianceSummary ===

    [Fact]
    public void BuildFannieComplianceSummary_ReturnsEmptyString_WhenNoCompliance()
    {
        var ctx = CreateFannieContext(FannieProductType.Conventional);
        var summary = UnderwritingPromptBuilder.BuildFannieComplianceSummary(ctx);
        Assert.Equal(string.Empty, summary);
    }

    [Fact]
    public void BuildFannieComplianceSummary_IncludesOverallPassStatus()
    {
        var ctx = CreateFannieContext(FannieProductType.Conventional, CreatePassingCompliance());
        var summary = UnderwritingPromptBuilder.BuildFannieComplianceSummary(ctx);

        Assert.Contains("PASS", summary);
    }

    [Fact]
    public void BuildFannieComplianceSummary_IncludesOverallFailStatus()
    {
        var ctx = CreateFannieContext(FannieProductType.SARM, CreateSarmCompliance());
        var summary = UnderwritingPromptBuilder.BuildFannieComplianceSummary(ctx);

        Assert.Contains("FAIL", summary);
    }

    [Fact]
    public void BuildFannieComplianceSummary_IncludesProductThresholds()
    {
        var ctx = CreateFannieContext(FannieProductType.Conventional, CreatePassingCompliance());
        var summary = UnderwritingPromptBuilder.BuildFannieComplianceSummary(ctx);

        Assert.Contains("1.25", summary); // Min DSCR
        Assert.Contains("80", summary);   // Max LTV
    }

    [Fact]
    public void BuildFannieComplianceSummary_IncludesCoreTests()
    {
        var ctx = CreateFannieContext(FannieProductType.Conventional, CreatePassingCompliance());
        var summary = UnderwritingPromptBuilder.BuildFannieComplianceSummary(ctx);

        Assert.Contains("DSCR", summary);
        Assert.Contains("LTV", summary);
        Assert.Contains("Amortization", summary);
    }

    [Fact]
    public void BuildFannieComplianceSummary_IncludesSeniorsTests()
    {
        var ctx = CreateFannieContext(FannieProductType.SeniorsAL, CreateSeniorsCompliance());
        var summary = UnderwritingPromptBuilder.BuildFannieComplianceSummary(ctx);

        Assert.Contains("Blended DSCR", summary);
        Assert.Contains("SNF NCF Cap", summary);
    }

    [Fact]
    public void BuildFannieComplianceSummary_IncludesCoopTests()
    {
        var ctx = CreateFannieContext(FannieProductType.Cooperative, CreateCoopCompliance());
        var summary = UnderwritingPromptBuilder.BuildFannieComplianceSummary(ctx);

        Assert.Contains("Co-op Actual Ops DSCR", summary);
        Assert.Contains("Co-op Market Rental DSCR", summary);
    }

    [Fact]
    public void BuildFannieComplianceSummary_IncludesSarmStressTest()
    {
        var ctx = CreateFannieContext(FannieProductType.SARM, CreateSarmCompliance());
        var summary = UnderwritingPromptBuilder.BuildFannieComplianceSummary(ctx);

        Assert.Contains("SARM Stress DSCR", summary);
        Assert.Contains("FAIL", summary);
    }

    // === Task 1: All prompts include FannieProductType when set ===

    [Fact]
    public void BuildExecutiveSummaryPrompt_IncludesFannieProductType()
    {
        var ctx = CreateFannieContext(FannieProductType.SeniorsAL, CreateSeniorsCompliance());
        var result = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.Contains("Fannie Mae", result.UserMessage);
        Assert.Contains("Assisted Living", result.UserMessage);
    }

    [Fact]
    public void BuildRiskAssessmentPrompt_IncludesFannieProductType()
    {
        var ctx = CreateFannieContext(FannieProductType.StudentHousing, CreateStudentCompliance());
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        Assert.Contains("Fannie Mae", result.UserMessage);
        Assert.Contains("Student Housing", result.UserMessage);
    }

    [Fact]
    public void BuildInvestmentDecisionPrompt_IncludesFannieProductType()
    {
        var ctx = CreateFannieContext(FannieProductType.SARM, CreateSarmCompliance());
        var result = _builder.BuildInvestmentDecisionPrompt(ctx);

        Assert.Contains("Fannie Mae", result.UserMessage);
        Assert.Contains("SARM", result.UserMessage);
    }

    [Fact]
    public void AllPrompts_SkipFannieSection_WhenNoProductType()
    {
        var deal = new Deal("Non-Fannie");
        deal.PropertyName = "Plain Deal";
        deal.Address = "100 Test St";
        deal.UnitCount = 50;
        deal.PurchasePrice = 5_000_000m;

        var ctx = new ProseGenerationContext { Deal = deal };

        var exec = _builder.BuildExecutiveSummaryPrompt(ctx);
        var risk = _builder.BuildRiskAssessmentPrompt(ctx);
        var invest = _builder.BuildInvestmentDecisionPrompt(ctx);

        Assert.DoesNotContain("Fannie Mae Compliance", exec.UserMessage);
        Assert.DoesNotContain("Fannie Mae Compliance", risk.UserMessage);
        Assert.DoesNotContain("Fannie Mae Compliance", invest.UserMessage);
    }

    // === Task 4: Executive Summary — Fannie product identification ===

    [Fact]
    public void BuildExecutiveSummaryPrompt_IncludesComplianceMetrics()
    {
        var ctx = CreateFannieContext(FannieProductType.Conventional, CreatePassingCompliance());
        var result = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.Contains("1.25", result.UserMessage);  // Product min DSCR
        Assert.Contains("PASS", result.UserMessage);   // Overall compliance
    }

    [Fact]
    public void BuildExecutiveSummaryPrompt_IncludesExecutionPath()
    {
        var ctx = CreateFannieContext(FannieProductType.Conventional, CreatePassingCompliance());
        var result = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.Contains("Fannie Mae", result.UserMessage);
        Assert.Contains("Conventional", result.UserMessage);
    }

    // === Task 2: Risk Assessment — compliance test results ===

    [Fact]
    public void BuildRiskAssessmentPrompt_IncludesComplianceTestResults()
    {
        var ctx = CreateFannieContext(FannieProductType.SeniorsAL, CreateSeniorsCompliance());
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        Assert.Contains("Blended DSCR", result.UserMessage);
        Assert.Contains("SNF NCF Cap", result.UserMessage);
    }

    [Fact]
    public void BuildRiskAssessmentPrompt_IncludesFailingTests_ForSarm()
    {
        var ctx = CreateFannieContext(FannieProductType.SARM, CreateSarmCompliance());
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        Assert.Contains("SARM Stress DSCR", result.UserMessage);
        Assert.Contains("FAIL", result.UserMessage);
    }

    [Fact]
    public void BuildRiskAssessmentPrompt_MentionsFannieCompliance_InSystemPrompt()
    {
        var ctx = CreateFannieContext(FannieProductType.Conventional, CreatePassingCompliance());
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        Assert.Contains("Fannie Mae", result.SystemPrompt);
    }

    // === Task 3: Investment Decision — product-aware GO/NO GO ===

    [Fact]
    public void BuildInvestmentDecisionPrompt_UsesProductMinDscr()
    {
        var ctx = CreateFannieContext(FannieProductType.SeniorsAL, CreateSeniorsCompliance());
        var result = _builder.BuildInvestmentDecisionPrompt(ctx);

        // Should reference 1.40x product min, not hardcoded 1.5x
        Assert.Contains("1.40", result.UserMessage);
    }

    [Fact]
    public void BuildInvestmentDecisionPrompt_IncludesCompliancePassFail()
    {
        var ctx = CreateFannieContext(FannieProductType.SARM, CreateSarmCompliance());
        var result = _builder.BuildInvestmentDecisionPrompt(ctx);

        // Should mention compliance status
        Assert.Contains("FAIL", result.UserMessage);
    }

    [Fact]
    public void BuildInvestmentDecisionPrompt_FannieDeal_ReferencesComplianceInDecision()
    {
        var ctx = CreateFannieContext(FannieProductType.Conventional, CreatePassingCompliance());
        var result = _builder.BuildInvestmentDecisionPrompt(ctx);

        Assert.Contains("compliance", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildInvestmentDecisionPrompt_NonFannie_KeepsLegacyThresholds()
    {
        var deal = new Deal("Non-Fannie");
        deal.PropertyName = "Legacy Deal";
        deal.Address = "100 Test St";
        deal.UnitCount = 50;
        deal.PurchasePrice = 5_000_000m;

        var calc = new CalculationResult(deal.Id)
        {
            DebtServiceCoverageRatio = 1.58m,
            InternalRateOfReturn = 18.5m
        };

        var ctx = new ProseGenerationContext { Deal = deal, Calculations = calc };
        var result = _builder.BuildInvestmentDecisionPrompt(ctx);

        // Legacy thresholds should remain
        Assert.Contains("15%", result.UserMessage);   // IRR > 15%
        Assert.Contains("1.5x", result.UserMessage);  // DSCR > 1.5x
    }

    // === Task 6: Integration tests — multiple product types ===

    [Theory]
    [InlineData(FannieProductType.SeniorsAL, "Assisted Living")]
    [InlineData(FannieProductType.StudentHousing, "Student Housing")]
    [InlineData(FannieProductType.SARM, "SARM")]
    [InlineData(FannieProductType.Cooperative, "Cooperative")]
    public void AllFanniePrompts_IncludeProductDisplayName(FannieProductType productType, string expectedText)
    {
        var compliance = CreatePassingCompliance();
        var ctx = CreateFannieContext(productType, compliance);
        var exec = _builder.BuildExecutiveSummaryPrompt(ctx);

        Assert.Contains(expectedText, exec.UserMessage);
    }

    [Fact]
    public void RiskAssessment_Seniors_IncludesBlendedDscr()
    {
        var ctx = CreateFannieContext(FannieProductType.SeniorsAL, CreateSeniorsCompliance());
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        Assert.Contains("Blended DSCR", result.UserMessage);
        Assert.Contains("1.45", result.UserMessage); // Actual blended value
    }

    [Fact]
    public void RiskAssessment_Cooperative_IncludesDualDscr()
    {
        var ctx = CreateFannieContext(FannieProductType.Cooperative, CreateCoopCompliance());
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        Assert.Contains("Co-op Actual Ops DSCR", result.UserMessage);
        Assert.Contains("Co-op Market Rental DSCR", result.UserMessage);
    }

    [Fact]
    public void RiskAssessment_Sarm_IncludesStressTest()
    {
        var ctx = CreateFannieContext(FannieProductType.SARM, CreateSarmCompliance());
        var result = _builder.BuildRiskAssessmentPrompt(ctx);

        Assert.Contains("SARM Stress DSCR", result.UserMessage);
    }

    [Fact]
    public void InvestmentDecision_Student_UsesProductThresholds()
    {
        var ctx = CreateFannieContext(FannieProductType.StudentHousing, CreateStudentCompliance());
        var result = _builder.BuildInvestmentDecisionPrompt(ctx);

        Assert.Contains("1.30", result.UserMessage); // Student min DSCR
    }
}
