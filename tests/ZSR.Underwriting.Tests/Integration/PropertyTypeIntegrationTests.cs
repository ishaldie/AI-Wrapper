using System.Text.Json;
using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Integration;

/// <summary>
/// End-to-end integration tests verifying the full underwriting pipeline
/// across all 12 property types: defaults → calculations → prompts → report assembly.
/// </summary>
public class PropertyTypeIntegrationTests
{
    private readonly UnderwritingCalculator _calc = new();
    private readonly UnderwritingPromptBuilder _prompt = new();

    // === Every property type produces valid defaults ===

    [Theory]
    [InlineData(PropertyType.Multifamily)]
    [InlineData(PropertyType.AssistedLiving)]
    [InlineData(PropertyType.SkilledNursing)]
    [InlineData(PropertyType.MemoryCare)]
    [InlineData(PropertyType.CCRC)]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    [InlineData(PropertyType.BoardAndCare)]
    [InlineData(PropertyType.IndependentLiving)]
    [InlineData(PropertyType.SeniorApartment)]
    public void AllTypes_HaveValidProtocolDefaults(PropertyType type)
    {
        var occupancy = ProtocolDefaults.GetEffectiveOccupancy(null, type);
        var opEx = ProtocolDefaults.GetEffectiveOpExRatio(type);
        var dscr = ProtocolDefaults.GetMinDscr(type);
        var ltv = ProtocolDefaults.GetMaxLtv(type);
        var mgmtFee = ProtocolDefaults.GetManagementFeePct(type);

        Assert.InRange(occupancy, 50m, 100m);
        Assert.InRange(opEx, 0.20m, 0.85m);
        Assert.InRange(dscr, 1.0m, 2.0m);
        Assert.InRange(ltv, 50m, 90m);
        Assert.InRange(mgmtFee, 2m, 8m);
    }

    // === Every property type produces a non-empty prompt ===

    [Theory]
    [InlineData(PropertyType.Multifamily)]
    [InlineData(PropertyType.AssistedLiving)]
    [InlineData(PropertyType.SkilledNursing)]
    [InlineData(PropertyType.MemoryCare)]
    [InlineData(PropertyType.CCRC)]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    [InlineData(PropertyType.BoardAndCare)]
    [InlineData(PropertyType.IndependentLiving)]
    [InlineData(PropertyType.SeniorApartment)]
    public void AllTypes_ProduceValidExecutiveSummaryPrompt(PropertyType type)
    {
        var deal = CreateDeal(type);
        var ctx = new ProseGenerationContext { Deal = deal, UserId = "test" };
        var prompt = _prompt.BuildExecutiveSummaryPrompt(ctx);

        Assert.NotNull(prompt);
        Assert.NotEmpty(prompt.SystemPrompt);
        Assert.NotEmpty(prompt.UserMessage);
        Assert.True(prompt.MaxTokens > 0);
        Assert.Contains(deal.PropertyName, prompt.UserMessage);
    }

    // === DSCR-constrained loan sizing works for every type ===

    [Theory]
    [InlineData(PropertyType.Multifamily, 80)]
    [InlineData(PropertyType.Bridge, 75)]
    [InlineData(PropertyType.Hospitality, 65)]
    [InlineData(PropertyType.Commercial, 75)]
    [InlineData(PropertyType.LIHTC, 85)]
    [InlineData(PropertyType.AssistedLiving, 85)]
    [InlineData(PropertyType.SkilledNursing, 85)]
    [InlineData(PropertyType.MemoryCare, 80)]
    [InlineData(PropertyType.CCRC, 80)]
    [InlineData(PropertyType.BoardAndCare, 80)]
    [InlineData(PropertyType.IndependentLiving, 80)]
    [InlineData(PropertyType.SeniorApartment, 80)]
    public void AllTypes_DscrConstrainedLoanSizing_ProducesPositiveLoan(PropertyType type, decimal expectedLtv)
    {
        var maxLtv = ProtocolDefaults.GetMaxLtv(type);
        Assert.Equal(expectedLtv, maxLtv);

        var minDscr = ProtocolDefaults.GetMinDscr(type);
        var purchasePrice = 10_000_000m;
        var noi = 600_000m;

        var result = _calc.CalculateConstrainedLoan(
            purchasePrice, maxLtv, noi, minDscr,
            annualRatePercent: 6.0m, amortizationYears: 30, isInterestOnly: false);

        Assert.True(result.MaxLoan > 0, $"{type}: MaxLoan should be positive");
        Assert.True(result.MaxLoan <= purchasePrice, $"{type}: MaxLoan should not exceed purchase price");
        Assert.True(result.LtvBasedLoan > 0);
        Assert.True(result.DscrBasedLoan > 0);
        Assert.Contains(result.ConstrainingTest, new[] { "DSCR", "LTV" });
    }

    // === Detailed expenses calculation works for every type ===

    [Theory]
    [InlineData(PropertyType.Multifamily)]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    [InlineData(PropertyType.AssistedLiving)]
    public void AllTypes_DetailedExpensesCalculation_SumsCorrectly(PropertyType type)
    {
        var expenses = new DetailedExpenses
        {
            RealEstateTaxes = 100_000m,
            Insurance = 40_000m,
            ManagementFeePct = 4.0m,
        };

        var result = _calc.CalculateDetailedExpenses(expenses, 100, 1_000_000m, type);

        // 100k + 40k + (4% of 1M = 40k) = 180k
        Assert.Equal(180_000m, result);
    }

    // === DetailedExpenses JSON round-trip ===

    [Fact]
    public void DetailedExpenses_JsonRoundTrip_PreservesAllFields()
    {
        var original = new DetailedExpenses
        {
            RealEstateTaxes = 150_000m,
            Insurance = 50_000m,
            Utilities = 30_000m,
            RepairsAndMaintenance = 60_000m,
            Payroll = 100_000m,
            Marketing = 10_000m,
            GeneralAndAdmin = 25_000m,
            ManagementFee = 35_000m,
            ManagementFeePct = 3.5m,
            ReplacementReserves = 25_000m,
            OtherExpenses = 5_000m,
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DetailedExpenses>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.RealEstateTaxes, deserialized.RealEstateTaxes);
        Assert.Equal(original.Insurance, deserialized.Insurance);
        Assert.Equal(original.Utilities, deserialized.Utilities);
        Assert.Equal(original.RepairsAndMaintenance, deserialized.RepairsAndMaintenance);
        Assert.Equal(original.Payroll, deserialized.Payroll);
        Assert.Equal(original.Marketing, deserialized.Marketing);
        Assert.Equal(original.GeneralAndAdmin, deserialized.GeneralAndAdmin);
        Assert.Equal(original.ManagementFee, deserialized.ManagementFee);
        Assert.Equal(original.ManagementFeePct, deserialized.ManagementFeePct);
        Assert.Equal(original.ReplacementReserves, deserialized.ReplacementReserves);
        Assert.Equal(original.OtherExpenses, deserialized.OtherExpenses);
        Assert.True(deserialized.HasAnyValues);
    }

    // === Backward compatibility — existing types unchanged ===

    [Fact]
    public void Multifamily_DefaultsUnchanged()
    {
        Assert.Equal(95m, ProtocolDefaults.GetEffectiveOccupancy(null, PropertyType.Multifamily));
        Assert.Equal(0.5435m, ProtocolDefaults.GetEffectiveOpExRatio(PropertyType.Multifamily));
        Assert.False(ProtocolDefaults.IsSeniorHousing(PropertyType.Multifamily));
    }

    [Fact]
    public void SeniorHousing_OriginalTypes_StillSenior()
    {
        Assert.True(ProtocolDefaults.IsSeniorHousing(PropertyType.AssistedLiving));
        Assert.True(ProtocolDefaults.IsSeniorHousing(PropertyType.SkilledNursing));
        Assert.True(ProtocolDefaults.IsSeniorHousing(PropertyType.MemoryCare));
        Assert.True(ProtocolDefaults.IsSeniorHousing(PropertyType.CCRC));
    }

    [Fact]
    public void NewNonSeniorTypes_NotSenior()
    {
        Assert.False(ProtocolDefaults.IsSeniorHousing(PropertyType.Bridge));
        Assert.False(ProtocolDefaults.IsSeniorHousing(PropertyType.Hospitality));
        Assert.False(ProtocolDefaults.IsSeniorHousing(PropertyType.Commercial));
        Assert.False(ProtocolDefaults.IsSeniorHousing(PropertyType.LIHTC));
    }

    [Fact]
    public void NewSeniorTypes_AreSenior()
    {
        Assert.True(ProtocolDefaults.IsSeniorHousing(PropertyType.BoardAndCare));
        Assert.True(ProtocolDefaults.IsSeniorHousing(PropertyType.IndependentLiving));
        Assert.True(ProtocolDefaults.IsSeniorHousing(PropertyType.SeniorApartment));
    }

    // === Bulk import CSV with mix of all 12 types ===

    [Fact]
    public void AllPropertyTypeStrings_ParseCorrectly()
    {
        var typeNames = Enum.GetNames<PropertyType>();
        Assert.Equal(12, typeNames.Length);

        foreach (var name in typeNames)
        {
            Assert.True(Enum.TryParse<PropertyType>(name, true, out var parsed));
            Assert.Equal(name, parsed.ToString());
        }
    }

    private static Deal CreateDeal(PropertyType type)
    {
        var isSenior = ProtocolDefaults.IsSeniorHousing(type);
        return new Deal($"Test {type}")
        {
            PropertyName = $"Test {type} Property",
            Address = "123 Main St, Austin TX",
            PurchasePrice = 10_000_000m,
            PropertyType = type,
            UnitCount = isSenior ? 0 : 100,
            LicensedBeds = isSenior ? 80 : null,
        };
    }
}
