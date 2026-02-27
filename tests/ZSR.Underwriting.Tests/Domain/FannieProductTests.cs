using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Domain;

public class FannieProductTests
{
    // === Task 4: Deal entity has FannieProductType property ===

    [Fact]
    public void Deal_FannieProductType_Is_Null_By_Default()
    {
        var deal = new Deal("Test Property");
        Assert.Null(deal.FannieProductType);
    }

    [Fact]
    public void Deal_FannieProductType_Can_Be_Set()
    {
        var deal = new Deal("Test Property");
        deal.FannieProductType = FannieProductType.Conventional;
        Assert.Equal(FannieProductType.Conventional, deal.FannieProductType);
    }

    [Theory]
    [InlineData(FannieProductType.SmallLoan)]
    [InlineData(FannieProductType.SeniorsAL)]
    [InlineData(FannieProductType.SARM)]
    [InlineData(FannieProductType.ROAR)]
    public void Deal_FannieProductType_Accepts_All_Types(FannieProductType productType)
    {
        var deal = new Deal("Test Property");
        deal.FannieProductType = productType;
        Assert.Equal(productType, deal.FannieProductType);
    }

    // === Task 6: Verify all 14 product profiles ===

    [Fact]
    public void FannieProductType_Has_14_Values()
    {
        var values = Enum.GetValues<FannieProductType>();
        Assert.Equal(14, values.Length);
    }

    [Fact]
    public void All_Returns_14_Profiles()
    {
        Assert.Equal(14, FannieProductProfiles.All.Count);
    }

    [Theory]
    [InlineData(FannieProductType.Conventional, 80, 1.25, 30)]
    [InlineData(FannieProductType.SmallLoan, 80, 1.25, 30)]
    [InlineData(FannieProductType.AffordableHousing, 80, 1.20, 35)]
    [InlineData(FannieProductType.SeniorsIL, 75, 1.30, 30)]
    [InlineData(FannieProductType.SeniorsAL, 75, 1.40, 30)]
    [InlineData(FannieProductType.SeniorsALZ, 75, 1.45, 30)]
    [InlineData(FannieProductType.StudentHousing, 75, 1.30, 30)]
    [InlineData(FannieProductType.ManufacturedHousing, 80, 1.25, 30)]
    [InlineData(FannieProductType.Cooperative, 55, 1.00, 30)]
    [InlineData(FannieProductType.SARM, 65, 1.05, 30)]
    [InlineData(FannieProductType.GreenRewards, 80, 1.25, 30)]
    [InlineData(FannieProductType.Supplemental, 70, 1.30, 30)]
    [InlineData(FannieProductType.NearStabilization, 75, 1.25, 30)]
    [InlineData(FannieProductType.ROAR, 90, 1.15, 35)]
    public void Profile_Has_Correct_LTV_DSCR_Amortization(
        FannieProductType type, decimal expectedLtv, decimal expectedDscr, int expectedAmort)
    {
        var profile = FannieProductProfiles.Get(type);
        Assert.Equal(expectedLtv, profile.MaxLtvPercent);
        Assert.Equal(expectedDscr, profile.MinDscr);
        Assert.Equal(expectedAmort, profile.MaxAmortizationYears);
    }

    [Fact]
    public void SmallLoan_Has_9M_Max()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.SmallLoan);
        Assert.Equal(9_000_000m, profile.MaxLoanAmount);
    }

    [Fact]
    public void SARM_Has_25M_Min_And_Stress_Test()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.SARM);
        Assert.Equal(25_000_000m, profile.MinLoanAmount);
        Assert.True(profile.RequiresRateCapStressTest);
        Assert.Equal(5, profile.MinTermYears);
        Assert.Equal(10, profile.MaxTermYears);
    }

    [Fact]
    public void Cooperative_Has_Dual_DSCR_And_FixedOnly()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.Cooperative);
        Assert.Equal(1.00m, profile.CoopActualDscr);
        Assert.Equal(1.55m, profile.CoopMarketRentalDscr);
        Assert.True(profile.FixedRateAvailable);
        Assert.False(profile.VariableRateAvailable);
        Assert.False(profile.IsAssumable);
    }

    [Fact]
    public void SeniorsAL_Has_Secondary_DSCR()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.SeniorsAL);
        Assert.Equal(1.40m, profile.SeniorsAlDscr);
    }

    [Fact]
    public void SeniorsALZ_Has_Secondary_DSCR()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.SeniorsALZ);
        Assert.Equal(1.45m, profile.SeniorsAlzDscr);
    }

    [Fact]
    public void Seniors_Have_20Pct_SNF_Cap()
    {
        Assert.Equal(20m, FannieProductProfiles.Get(FannieProductType.SeniorsIL).MaxSnfNcfPercent);
        Assert.Equal(20m, FannieProductProfiles.Get(FannieProductType.SeniorsAL).MaxSnfNcfPercent);
        Assert.Equal(20m, FannieProductProfiles.Get(FannieProductType.SeniorsALZ).MaxSnfNcfPercent);
    }

    [Fact]
    public void MHC_Has_Correct_Thresholds()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.ManufacturedHousing);
        Assert.Equal(5m, profile.MinVacancyPercent);
        Assert.Equal(50, profile.MinPadSites);
        Assert.Equal(35m, profile.MaxTenantOccupiedPercent);
    }

    [Fact]
    public void StudentHousing_Has_Enrollment_Requirements()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.StudentHousing);
        Assert.Equal(40m, profile.MinStudentPercent);
        Assert.Equal(10_000, profile.DedicatedMinEnrollment);
    }

    [Fact]
    public void GreenRewards_Has_Savings_Split()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.GreenRewards);
        Assert.Equal(75m, profile.GreenOwnerSavingsPercent);
        Assert.Equal(25m, profile.GreenTenantSavingsPercent);
        Assert.Equal(5m, profile.GreenMaxAdditionalProceedsPercent);
    }

    [Fact]
    public void ROAR_Has_Rehab_Parameters()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.ROAR);
        Assert.Equal(50m, profile.RoarRehabMinOccupancy);
        Assert.Equal(1.00m, profile.RoarRehabMinDscrIo);
        Assert.Equal(0.75m, profile.RoarRehabMinDscrAmortizing);
        Assert.Equal(120_000m, profile.RoarMaxPerUnitRehab);
        Assert.Equal(5_000_000m, profile.MinLoanAmount);
    }

    [Fact]
    public void NearStabilization_Has_Correct_Thresholds()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.NearStabilization);
        Assert.Equal(10_000_000m, profile.MinLoanAmount);
        Assert.Equal(75m, profile.MinOccupancyPercent);
    }

    [Fact]
    public void Supplemental_Requires_Combined_Test()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.Supplemental);
        Assert.True(profile.RequiresCombinedLoanTest);
    }

    [Fact]
    public void AffordableHousing_Has_35yr_Amortization()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.AffordableHousing);
        Assert.Equal(35, profile.MaxAmortizationYears);
        Assert.Equal(1.20m, profile.MinDscr);
    }

    [Fact]
    public void Conventional_Has_90Pct_Occupancy()
    {
        var profile = FannieProductProfiles.Get(FannieProductType.Conventional);
        Assert.Equal(90m, profile.MinOccupancyPercent);
    }

    [Fact]
    public void Get_Throws_For_Invalid_Type()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FannieProductProfiles.Get((FannieProductType)999));
    }

    [Fact]
    public void TryGet_Returns_Null_For_Null_Type()
    {
        Assert.Null(FannieProductProfiles.TryGet(null));
    }

    [Fact]
    public void TryGet_Returns_Profile_For_Valid_Type()
    {
        var profile = FannieProductProfiles.TryGet(FannieProductType.Conventional);
        Assert.NotNull(profile);
        Assert.Equal(FannieProductType.Conventional, profile.ProductType);
    }

    // === SuggestFromPropertyType ===

    [Theory]
    [InlineData(PropertyType.Multifamily, FannieProductType.Conventional)]
    [InlineData(PropertyType.AssistedLiving, FannieProductType.SeniorsAL)]
    [InlineData(PropertyType.SkilledNursing, FannieProductType.SeniorsIL)]
    [InlineData(PropertyType.MemoryCare, FannieProductType.SeniorsALZ)]
    [InlineData(PropertyType.CCRC, FannieProductType.SeniorsIL)]
    public void SuggestFromPropertyType_Returns_Correct_Mapping(
        PropertyType propertyType, FannieProductType expected)
    {
        Assert.Equal(expected, FannieProductProfiles.SuggestFromPropertyType(propertyType));
    }

    // === Blended DSCR calculation ===

    [Fact]
    public void BlendedDscr_AllIL_Returns_1_30()
    {
        var result = FannieProductProfiles.CalculateSeniorsBlendedMinDscr(100, 0, 0);
        Assert.Equal(1.30m, result);
    }

    [Fact]
    public void BlendedDscr_AllAL_Returns_1_40()
    {
        var result = FannieProductProfiles.CalculateSeniorsBlendedMinDscr(0, 100, 0);
        Assert.Equal(1.40m, result);
    }

    [Fact]
    public void BlendedDscr_AllALZ_Returns_1_45()
    {
        var result = FannieProductProfiles.CalculateSeniorsBlendedMinDscr(0, 0, 100);
        Assert.Equal(1.45m, result);
    }

    [Fact]
    public void BlendedDscr_EqualMix_Returns_Weighted_Average()
    {
        // Equal thirds: (1.30 + 1.40 + 1.45) / 3 = 1.3833... → 1.38
        var result = FannieProductProfiles.CalculateSeniorsBlendedMinDscr(100, 100, 100);
        Assert.Equal(1.38m, result);
    }

    [Fact]
    public void BlendedDscr_ZeroBeds_Defaults_To_IL()
    {
        var result = FannieProductProfiles.CalculateSeniorsBlendedMinDscr(0, 0, 0);
        Assert.Equal(1.30m, result);
    }
}
