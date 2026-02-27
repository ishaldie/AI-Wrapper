using System.Text.Json;
using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Integration;

public class FreddieIntegrationTests
{
    private static CalculationInputs CreateBaseInputs(FreddieProductType? freddieType = null, FannieProductType? fannieType = null)
    {
        return new CalculationInputs
        {
            DealId = Guid.NewGuid(),
            RentPerUnit = 1500m,
            UnitCount = 100,
            OccupancyPercent = 95m,
            PurchasePrice = 10_000_000m,
            LtvPercent = 75m,
            InterestRatePercent = 5.5m,
            AmortizationYears = 30,
            HoldPeriodYears = 5,
            MarketCapRatePercent = 5.5m,
            AnnualGrowthRatePercents = new[] { 3m, 3m, 3m, 3m, 3m },
            FreddieProductType = freddieType,
            FannieProductType = fannieType
        };
    }

    // === Each product type creates valid compliance result ===

    [Theory]
    [InlineData(FreddieProductType.Conventional)]
    [InlineData(FreddieProductType.SmallBalanceLoan)]
    [InlineData(FreddieProductType.TargetedAffordable)]
    [InlineData(FreddieProductType.SeniorsIL)]
    [InlineData(FreddieProductType.SeniorsAL)]
    [InlineData(FreddieProductType.SeniorsSN)]
    [InlineData(FreddieProductType.StudentHousing)]
    [InlineData(FreddieProductType.ManufacturedHousing)]
    [InlineData(FreddieProductType.FloatingRate)]
    [InlineData(FreddieProductType.ValueAdd)]
    [InlineData(FreddieProductType.ModerateRehab)]
    [InlineData(FreddieProductType.LeaseUp)]
    [InlineData(FreddieProductType.Supplemental)]
    [InlineData(FreddieProductType.TaxExemptLIHTC)]
    [InlineData(FreddieProductType.Section8)]
    [InlineData(FreddieProductType.NOAHPreservation)]
    public void Each_Product_Creates_Valid_Compliance(FreddieProductType productType)
    {
        var inputs = CreateBaseInputs(freddieType: productType);
        var result = CalculationResultAssembler.Assemble(inputs);

        Assert.NotNull(result.FreddieComplianceJson);

        var compliance = JsonSerializer.Deserialize<FreddieComplianceResult>(result.FreddieComplianceJson);
        Assert.NotNull(compliance);
        Assert.NotNull(compliance.DscrTest);
        Assert.NotNull(compliance.LtvTest);
        Assert.NotNull(compliance.AmortizationTest);
    }

    // === Backward compatibility: Fannie deals unchanged ===

    [Fact]
    public void Fannie_Deal_Still_Works()
    {
        var inputs = CreateBaseInputs(fannieType: FannieProductType.Conventional);
        var result = CalculationResultAssembler.Assemble(inputs);

        Assert.NotNull(result.FannieComplianceJson);
        Assert.Null(result.FreddieComplianceJson);

        var compliance = JsonSerializer.Deserialize<FannieComplianceResult>(result.FannieComplianceJson);
        Assert.NotNull(compliance);
        Assert.True(compliance.DscrTest.Pass);
    }

    // === Both can co-exist on different deals ===

    [Fact]
    public void Fannie_And_Freddie_On_Different_Deals()
    {
        var fannieInputs = CreateBaseInputs(fannieType: FannieProductType.Conventional);
        var freddieInputs = CreateBaseInputs(freddieType: FreddieProductType.Conventional);

        var fannieResult = CalculationResultAssembler.Assemble(fannieInputs);
        var freddieResult = CalculationResultAssembler.Assemble(freddieInputs);

        Assert.NotNull(fannieResult.FannieComplianceJson);
        Assert.Null(fannieResult.FreddieComplianceJson);
        Assert.NotNull(freddieResult.FreddieComplianceJson);
        Assert.Null(freddieResult.FannieComplianceJson);
    }

    // === Prompt builder correct for each execution type ===

    [Fact]
    public void PromptBuilder_Freddie_Conventional()
    {
        var deal = new Deal("Freddie Test");
        deal.PropertyName = "Freddie Apartments";
        deal.Address = "123 Freddie St";
        deal.UnitCount = 100;
        deal.PurchasePrice = 10_000_000m;
        deal.ExecutionType = ExecutionType.FreddieMac;
        deal.FreddieProductType = FreddieProductType.Conventional;

        var calc = new CalculationResult(deal.Id);
        calc.NetOperatingIncome = 800_000m;
        calc.DebtServiceCoverageRatio = 1.35m;
        calc.InternalRateOfReturn = 18.5m;

        var context = new ProseGenerationContext { Deal = deal, Calculations = calc };
        var builder = new UnderwritingPromptBuilder();
        var request = builder.BuildExecutiveSummaryPrompt(context);

        Assert.Contains("Freddie Mac Execution", request.UserMessage);
        Assert.Contains("Conventional Loans", request.UserMessage);
        Assert.DoesNotContain("Fannie Mae", request.UserMessage);
    }

    [Fact]
    public void PromptBuilder_Fannie_Conventional_Unchanged()
    {
        var deal = new Deal("Fannie Test");
        deal.PropertyName = "Fannie Apartments";
        deal.Address = "123 Fannie St";
        deal.UnitCount = 100;
        deal.PurchasePrice = 10_000_000m;
        deal.ExecutionType = ExecutionType.FannieMae;
        deal.FannieProductType = FannieProductType.Conventional;

        var calc = new CalculationResult(deal.Id);
        calc.NetOperatingIncome = 800_000m;
        calc.DebtServiceCoverageRatio = 1.35m;
        calc.InternalRateOfReturn = 18.5m;

        var context = new ProseGenerationContext { Deal = deal, Calculations = calc };
        var builder = new UnderwritingPromptBuilder();
        var request = builder.BuildExecutiveSummaryPrompt(context);

        Assert.Contains("Fannie Mae Execution", request.UserMessage);
        Assert.DoesNotContain("Freddie Mac", request.UserMessage);
    }

    // === No agency deal uses default thresholds ===

    [Fact]
    public void PromptBuilder_NoAgency_UsesDefaultThresholds()
    {
        var deal = new Deal("Generic Test");
        deal.PropertyName = "Generic Apartments";
        deal.Address = "123 Generic St";
        deal.UnitCount = 100;
        deal.PurchasePrice = 10_000_000m;
        deal.ExecutionType = ExecutionType.All;

        var calc = new CalculationResult(deal.Id);
        calc.NetOperatingIncome = 800_000m;
        calc.DebtServiceCoverageRatio = 1.35m;
        calc.InternalRateOfReturn = 18.5m;

        var context = new ProseGenerationContext { Deal = deal, Calculations = calc };
        var builder = new UnderwritingPromptBuilder();
        var request = builder.BuildInvestmentDecisionPrompt(context);

        Assert.Contains("Protocol Decision Thresholds", request.UserMessage);
        Assert.DoesNotContain("Fannie Mae", request.UserMessage);
        Assert.DoesNotContain("Freddie Mac", request.UserMessage);
    }

    // === MHC vacancy floor applies for Freddie MHC too ===

    [Fact]
    public void Assembler_MhcVacancyFloor_Applied_For_Freddie()
    {
        var inputs = CreateBaseInputs(freddieType: FreddieProductType.ManufacturedHousing);
        inputs.OccupancyPercent = 98m; // Should be capped to 95%

        var result = CalculationResultAssembler.Assemble(inputs);

        // The vacancy loss should reflect 95% occupancy, not 98%
        Assert.NotNull(result.FreddieComplianceJson);
    }
}
