using System.Text.Json;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Domain;

public class FreddieProductTests
{
    // === Enum has 16 values ===

    [Fact]
    public void FreddieProductType_Has_16_Values()
    {
        var values = Enum.GetValues<FreddieProductType>();
        Assert.Equal(16, values.Length);
    }

    // === Deal entity fields ===

    [Fact]
    public void Deal_FreddieProductType_Is_Null_By_Default()
    {
        var deal = new Deal("Test Property");
        Assert.Null(deal.FreddieProductType);
    }

    [Fact]
    public void Deal_FreddieProductType_Can_Be_Set()
    {
        var deal = new Deal("Test Property");
        deal.FreddieProductType = FreddieProductType.Conventional;
        Assert.Equal(FreddieProductType.Conventional, deal.FreddieProductType);
    }

    [Theory]
    [InlineData(FreddieProductType.SmallBalanceLoan)]
    [InlineData(FreddieProductType.SeniorsAL)]
    [InlineData(FreddieProductType.FloatingRate)]
    [InlineData(FreddieProductType.NOAHPreservation)]
    [InlineData(FreddieProductType.ValueAdd)]
    [InlineData(FreddieProductType.LeaseUp)]
    public void Deal_FreddieProductType_Accepts_All_Types(FreddieProductType productType)
    {
        var deal = new Deal("Test Property");
        deal.FreddieProductType = productType;
        Assert.Equal(productType, deal.FreddieProductType);
    }

    // === ComplianceResult serialization ===

    [Fact]
    public void FreddieComplianceResult_Roundtrips_Json()
    {
        var result = new FreddieComplianceResult
        {
            OverallPass = true,
            ProductMinDscr = 1.25m,
            ProductMaxLtvPercent = 80m,
            ProductMaxAmortYears = 30,
            DscrTest = new ComplianceTest { Name = "DSCR", Pass = true, ActualValue = 1.35m, RequiredValue = 1.25m },
            LtvTest = new ComplianceTest { Name = "LTV", Pass = true, ActualValue = 75m, RequiredValue = 80m },
            AmortizationTest = new ComplianceTest { Name = "Amort", Pass = true, ActualValue = 30, RequiredValue = 30 }
        };

        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<FreddieComplianceResult>(json);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.OverallPass);
        Assert.Equal(1.25m, deserialized.ProductMinDscr);
        Assert.True(deserialized.DscrTest.Pass);
    }

    // === All 16 profiles exist ===

    [Fact]
    public void All_Returns_16_Profiles()
    {
        Assert.Equal(16, FreddieProductProfiles.All.Count);
    }

    [Theory]
    [InlineData(FreddieProductType.Conventional, 80, 1.25, 30)]
    [InlineData(FreddieProductType.SmallBalanceLoan, 80, 1.20, 30)]
    [InlineData(FreddieProductType.TargetedAffordable, 80, 1.20, 30)]
    [InlineData(FreddieProductType.SeniorsIL, 75, 1.30, 30)]
    [InlineData(FreddieProductType.SeniorsAL, 75, 1.45, 30)]
    [InlineData(FreddieProductType.SeniorsSN, 75, 1.50, 30)]
    [InlineData(FreddieProductType.StudentHousing, 80, 1.30, 30)]
    [InlineData(FreddieProductType.ManufacturedHousing, 80, 1.25, 30)]
    [InlineData(FreddieProductType.FloatingRate, 80, 1.25, 30)]
    [InlineData(FreddieProductType.ValueAdd, 85, 1.15, 30)]
    [InlineData(FreddieProductType.ModerateRehab, 80, 1.20, 30)]
    [InlineData(FreddieProductType.LeaseUp, 75, 1.30, 30)]
    [InlineData(FreddieProductType.Supplemental, 80, 1.25, 30)]
    [InlineData(FreddieProductType.TaxExemptLIHTC, 90, 1.15, 30)]
    [InlineData(FreddieProductType.Section8, 80, 1.20, 30)]
    [InlineData(FreddieProductType.NOAHPreservation, 80, 1.20, 30)]
    public void Profile_Has_Correct_LTV_DSCR_Amortization(
        FreddieProductType type, decimal expectedLtv, decimal expectedDscr, int expectedAmort)
    {
        var profile = FreddieProductProfiles.Get(type);
        Assert.Equal(expectedLtv, profile.MaxLtvPercent);
        Assert.Equal(expectedDscr, profile.MinDscr);
        Assert.Equal(expectedAmort, profile.MaxAmortizationYears);
    }

    // === SBL tier fields ===

    [Fact]
    public void SBL_Has_Loan_Range_And_Tier()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.SmallBalanceLoan);
        Assert.Equal(1_000_000m, profile.MinLoanAmount);
        Assert.Equal(7_500_000m, profile.MaxLoanAmount);
        Assert.NotNull(profile.SblMarketTier);
    }

    // === Freddie-specific product fields ===

    [Fact]
    public void SeniorsAL_Has_1_45_DSCR()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.SeniorsAL);
        Assert.Equal(1.45m, profile.SeniorsAlDscr);
    }

    [Fact]
    public void SeniorsSN_Has_1_50_DSCR()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.SeniorsSN);
        Assert.Equal(1.50m, profile.SeniorsSnDscr);
    }

    [Fact]
    public void Seniors_Have_20Pct_SNF_NOI_Cap()
    {
        Assert.Equal(20m, FreddieProductProfiles.Get(FreddieProductType.SeniorsIL).MaxSnfNoiPercent);
        Assert.Equal(20m, FreddieProductProfiles.Get(FreddieProductType.SeniorsAL).MaxSnfNoiPercent);
        Assert.Equal(20m, FreddieProductProfiles.Get(FreddieProductType.SeniorsSN).MaxSnfNoiPercent);
    }

    [Fact]
    public void MHC_Has_5_MinPads_And_25Pct_RentalCap()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.ManufacturedHousing);
        Assert.Equal(5, profile.MinPadSites);
        Assert.Equal(25m, profile.MaxRentalHomesPercent);
    }

    [Fact]
    public void FloatingRate_Requires_Cap()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.FloatingRate);
        Assert.True(profile.RequiresRateCap);
        Assert.Equal(60m, profile.RateCapLtvThreshold);
    }

    [Fact]
    public void ValueAdd_Has_Rehab_Range()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.ValueAdd);
        Assert.Equal(10_000m, profile.MinRehabPerUnit);
        Assert.Equal(25_000m, profile.MaxRehabPerUnit);
        Assert.Equal(1.10m, profile.RehabMinDscrIo);
        Assert.Equal(1.15m, profile.RehabMinDscrAmortizing);
    }

    [Fact]
    public void LeaseUp_Has_Occupancy_Thresholds()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.LeaseUp);
        Assert.Equal(65m, profile.LeaseUpMinOccupancy);
        Assert.Equal(75m, profile.LeaseUpMinLeased);
    }

    [Fact]
    public void NOAHPreservation_Requires_Nonprofit()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.NOAHPreservation);
        Assert.True(profile.NonprofitRequired);
        Assert.Equal(15, profile.MaxTermYears);
    }

    [Fact]
    public void Supplemental_Requires_Combined_Test()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.Supplemental);
        Assert.True(profile.RequiresCombinedLoanTest);
        Assert.Equal(1_000_000m, profile.MinLoanAmount);
    }

    [Fact]
    public void TaxExemptLIHTC_Has_90Pct_LTV()
    {
        var profile = FreddieProductProfiles.Get(FreddieProductType.TaxExemptLIHTC);
        Assert.Equal(90m, profile.MaxLtvPercent);
        Assert.Equal(1.15m, profile.MinDscr);
    }

    // === SuggestFromPropertyType ===

    [Theory]
    [InlineData(PropertyType.Multifamily, FreddieProductType.Conventional)]
    [InlineData(PropertyType.AssistedLiving, FreddieProductType.SeniorsAL)]
    [InlineData(PropertyType.SkilledNursing, FreddieProductType.SeniorsSN)]
    [InlineData(PropertyType.MemoryCare, FreddieProductType.SeniorsAL)]
    [InlineData(PropertyType.CCRC, FreddieProductType.SeniorsIL)]
    public void SuggestFromPropertyType_Returns_Correct_Mapping(
        PropertyType propertyType, FreddieProductType expected)
    {
        Assert.Equal(expected, FreddieProductProfiles.SuggestFromPropertyType(propertyType));
    }

    // === Blended DSCR calculation ===

    [Fact]
    public void BlendedDscr_AllIL_Returns_1_30()
    {
        var result = FreddieProductProfiles.CalculateSeniorsBlendedMinDscr(100, 0, 0);
        Assert.Equal(1.30m, result);
    }

    [Fact]
    public void BlendedDscr_AllAL_Returns_1_45()
    {
        var result = FreddieProductProfiles.CalculateSeniorsBlendedMinDscr(0, 100, 0);
        Assert.Equal(1.45m, result);
    }

    [Fact]
    public void BlendedDscr_AllSN_Returns_1_50()
    {
        var result = FreddieProductProfiles.CalculateSeniorsBlendedMinDscr(0, 0, 100);
        Assert.Equal(1.50m, result);
    }

    [Fact]
    public void BlendedDscr_EqualMix_Returns_Weighted_Average()
    {
        // Equal thirds: (1.30 + 1.45 + 1.50) / 3 = 1.4166... → 1.42
        var result = FreddieProductProfiles.CalculateSeniorsBlendedMinDscr(100, 100, 100);
        Assert.Equal(1.42m, result);
    }

    [Fact]
    public void BlendedDscr_ZeroBeds_Defaults_To_IL()
    {
        var result = FreddieProductProfiles.CalculateSeniorsBlendedMinDscr(0, 0, 0);
        Assert.Equal(1.30m, result);
    }

    // === Get/TryGet ===

    [Fact]
    public void Get_Throws_For_Invalid_Type()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FreddieProductProfiles.Get((FreddieProductType)999));
    }

    [Fact]
    public void TryGet_Returns_Null_For_Null_Type()
    {
        Assert.Null(FreddieProductProfiles.TryGet(null));
    }

    [Fact]
    public void TryGet_Returns_Profile_For_Valid_Type()
    {
        var profile = FreddieProductProfiles.TryGet(FreddieProductType.Conventional);
        Assert.NotNull(profile);
        Assert.Equal(FreddieProductType.Conventional, profile.ProductType);
    }
}
