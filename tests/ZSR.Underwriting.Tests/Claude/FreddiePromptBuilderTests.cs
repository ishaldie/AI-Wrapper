using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Models;
using System.Text.Json;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Claude;

public class FreddiePromptBuilderTests
{
    private readonly UnderwritingPromptBuilder _builder = new();

    private static ProseGenerationContext CreateContext(
        FreddieProductType? freddieProduct = null,
        FannieProductType? fannieProduct = null,
        string? freddieComplianceJson = null)
    {
        var deal = new Deal("Test Property");
        deal.PropertyName = "Test Apartments";
        deal.Address = "123 Test St";
        deal.UnitCount = 100;
        deal.PurchasePrice = 10_000_000m;

        if (freddieProduct.HasValue)
        {
            deal.ExecutionType = ExecutionType.FreddieMac;
            deal.FreddieProductType = freddieProduct;
        }
        else if (fannieProduct.HasValue)
        {
            deal.ExecutionType = ExecutionType.FannieMae;
            deal.FannieProductType = fannieProduct;
        }

        var calc = new CalculationResult(deal.Id);
        calc.NetOperatingIncome = 800_000m;
        calc.DebtServiceCoverageRatio = 1.35m;
        calc.InternalRateOfReturn = 18.5m;
        calc.GoingInCapRate = 8.0m;
        calc.LoanAmount = 7_500_000m;
        calc.AnnualDebtService = 590_000m;

        if (freddieComplianceJson != null)
            calc.FreddieComplianceJson = freddieComplianceJson;

        return new ProseGenerationContext
        {
            Deal = deal,
            Calculations = calc
        };
    }

    private static string CreateSampleFreddieComplianceJson()
    {
        var compliance = new FreddieComplianceResult
        {
            OverallPass = true,
            ProductMinDscr = 1.25m,
            ProductMaxLtvPercent = 80m,
            ProductMaxAmortYears = 30,
            DscrTest = new ComplianceTest { Name = "DSCR Minimum", Pass = true, ActualValue = 1.35m, RequiredValue = 1.25m },
            LtvTest = new ComplianceTest { Name = "LTV Maximum", Pass = true, ActualValue = 75m, RequiredValue = 80m },
            AmortizationTest = new ComplianceTest { Name = "Amortization Maximum", Pass = true, ActualValue = 30, RequiredValue = 30 }
        };
        return JsonSerializer.Serialize(compliance);
    }

    // === Freddie header appears in prompts ===

    [Fact]
    public void ExecutiveSummary_Includes_Freddie_Header()
    {
        var context = CreateContext(freddieProduct: FreddieProductType.Conventional);
        var request = _builder.BuildExecutiveSummaryPrompt(context);

        Assert.Contains("Freddie Mac Execution", request.UserMessage);
        Assert.Contains("Conventional Loans", request.UserMessage);
    }

    [Fact]
    public void ExecutiveSummary_Excludes_Freddie_For_NonFreddie()
    {
        var context = CreateContext();
        var request = _builder.BuildExecutiveSummaryPrompt(context);

        Assert.DoesNotContain("Freddie Mac", request.UserMessage);
    }

    // === Compliance summary ===

    [Fact]
    public void ExecutiveSummary_Includes_Freddie_Compliance()
    {
        var json = CreateSampleFreddieComplianceJson();
        var context = CreateContext(
            freddieProduct: FreddieProductType.Conventional,
            freddieComplianceJson: json);
        var request = _builder.BuildExecutiveSummaryPrompt(context);

        Assert.Contains("Freddie Mac Compliance:", request.UserMessage);
        Assert.Contains("PASS", request.UserMessage);
    }

    [Fact]
    public void BuildFreddieComplianceSummary_Returns_Empty_For_NonFreddie()
    {
        var context = CreateContext();
        var summary = UnderwritingPromptBuilder.BuildFreddieComplianceSummary(context);
        Assert.Empty(summary);
    }

    [Fact]
    public void BuildFreddieComplianceSummary_Returns_Summary_For_Freddie()
    {
        var json = CreateSampleFreddieComplianceJson();
        var context = CreateContext(
            freddieProduct: FreddieProductType.Conventional,
            freddieComplianceJson: json);
        var summary = UnderwritingPromptBuilder.BuildFreddieComplianceSummary(context);

        Assert.Contains("Overall Compliance: PASS", summary);
        Assert.Contains("DSCR Minimum", summary);
    }

    // === Investment decision thresholds ===

    [Fact]
    public void InvestmentDecision_Uses_Freddie_Thresholds()
    {
        var json = CreateSampleFreddieComplianceJson();
        var context = CreateContext(
            freddieProduct: FreddieProductType.Conventional,
            freddieComplianceJson: json);
        var request = _builder.BuildInvestmentDecisionPrompt(context);

        Assert.Contains("Freddie Mac Product Decision Thresholds", request.UserMessage);
        Assert.Contains("Freddie Mac compliance PASS", request.UserMessage);
    }

    [Fact]
    public void InvestmentDecision_Uses_Default_When_No_Agency()
    {
        var context = CreateContext();
        var request = _builder.BuildInvestmentDecisionPrompt(context);

        Assert.Contains("Protocol Decision Thresholds", request.UserMessage);
        Assert.DoesNotContain("Freddie Mac", request.UserMessage);
    }

    // === Risk assessment ===

    [Fact]
    public void RiskAssessment_Includes_Freddie_Suffix()
    {
        var context = CreateContext(freddieProduct: FreddieProductType.Conventional);
        var request = _builder.BuildRiskAssessmentPrompt(context);

        Assert.Contains("Freddie Mac compliance risks", request.SystemPrompt);
    }

    // === Fannie deals unchanged ===

    [Fact]
    public void Fannie_Prompt_Unchanged_When_Freddie_Exists()
    {
        var context = CreateContext(fannieProduct: FannieProductType.Conventional);
        var request = _builder.BuildExecutiveSummaryPrompt(context);

        Assert.Contains("Fannie Mae Execution", request.UserMessage);
        Assert.DoesNotContain("Freddie Mac", request.UserMessage);
    }
}
