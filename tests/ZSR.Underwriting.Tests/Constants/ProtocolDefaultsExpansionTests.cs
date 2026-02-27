using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Constants;

public class ProtocolDefaultsExpansionTests
{
    // === Occupancy defaults for new types ===
    [Theory]
    [InlineData(PropertyType.Bridge, 92)]
    [InlineData(PropertyType.Hospitality, 65)]
    [InlineData(PropertyType.Commercial, 93)]
    [InlineData(PropertyType.LIHTC, 97)]
    [InlineData(PropertyType.BoardAndCare, 85)]
    [InlineData(PropertyType.IndependentLiving, 90)]
    [InlineData(PropertyType.SeniorApartment, 95)]
    public void NewTypeOccupancy_ReturnsCorrectDefault(PropertyType type, decimal expected)
    {
        var result = ProtocolDefaults.GetEffectiveOccupancy(null, type);
        Assert.Equal(expected, result);
    }

    // === OpEx ratio defaults for new types ===
    [Theory]
    [InlineData(PropertyType.Bridge, 0.50)]
    [InlineData(PropertyType.Hospitality, 0.62)]
    [InlineData(PropertyType.Commercial, 0.45)]
    [InlineData(PropertyType.LIHTC, 0.58)]
    [InlineData(PropertyType.BoardAndCare, 0.70)]
    [InlineData(PropertyType.IndependentLiving, 0.60)]
    [InlineData(PropertyType.SeniorApartment, 0.52)]
    public void NewTypeOpExRatio_ReturnsCorrectDefault(PropertyType type, double expected)
    {
        var result = ProtocolDefaults.GetEffectiveOpExRatio(type);
        Assert.Equal((decimal)expected, result);
    }

    // === Other income ratio defaults for new types ===
    [Theory]
    [InlineData(PropertyType.Bridge, 0.10)]
    [InlineData(PropertyType.Hospitality, 0.15)]
    [InlineData(PropertyType.Commercial, 0.03)]
    [InlineData(PropertyType.LIHTC, 0.05)]
    [InlineData(PropertyType.BoardAndCare, 0.05)]
    [InlineData(PropertyType.IndependentLiving, 0.08)]
    [InlineData(PropertyType.SeniorApartment, 0.10)]
    public void NewTypeOtherIncomeRatio_ReturnsCorrectDefault(PropertyType type, double expected)
    {
        var result = ProtocolDefaults.GetEffectiveOtherIncomeRatio(type);
        Assert.Equal((decimal)expected, result);
    }

    // === Management fee defaults ===
    [Theory]
    [InlineData(PropertyType.Multifamily, 3.5)]
    [InlineData(PropertyType.Bridge, 3.5)]
    [InlineData(PropertyType.Hospitality, 3.0)]
    [InlineData(PropertyType.Commercial, 4.0)]
    [InlineData(PropertyType.LIHTC, 6.0)]
    [InlineData(PropertyType.SkilledNursing, 5.0)]
    [InlineData(PropertyType.AssistedLiving, 5.0)]
    [InlineData(PropertyType.MemoryCare, 5.0)]
    [InlineData(PropertyType.CCRC, 5.0)]
    [InlineData(PropertyType.BoardAndCare, 5.0)]
    [InlineData(PropertyType.IndependentLiving, 5.0)]
    [InlineData(PropertyType.SeniorApartment, 4.0)]
    public void ManagementFeePct_ReturnsCorrectDefault(PropertyType type, double expected)
    {
        var result = ProtocolDefaults.GetManagementFeePct(type);
        Assert.Equal((decimal)expected, result);
    }

    // === Replacement reserve PUPA defaults ===
    [Theory]
    [InlineData(PropertyType.Multifamily, 250)]
    [InlineData(PropertyType.Bridge, 250)]
    [InlineData(PropertyType.Hospitality, 250)]
    [InlineData(PropertyType.Commercial, 250)]
    [InlineData(PropertyType.LIHTC, 300)]
    [InlineData(PropertyType.SkilledNursing, 350)]
    [InlineData(PropertyType.AssistedLiving, 350)]
    [InlineData(PropertyType.MemoryCare, 350)]
    [InlineData(PropertyType.CCRC, 350)]
    [InlineData(PropertyType.BoardAndCare, 350)]
    [InlineData(PropertyType.IndependentLiving, 300)]
    [InlineData(PropertyType.SeniorApartment, 250)]
    public void ReservesPupa_ReturnsCorrectDefault(PropertyType type, decimal expected)
    {
        var result = ProtocolDefaults.GetReservesPupa(type);
        Assert.Equal(expected, result);
    }

    // === DSCR minimum thresholds ===
    [Theory]
    [InlineData(PropertyType.Multifamily, 1.25)]
    [InlineData(PropertyType.Bridge, 1.20)]
    [InlineData(PropertyType.Hospitality, 1.40)]
    [InlineData(PropertyType.Commercial, 1.30)]
    [InlineData(PropertyType.LIHTC, 1.15)]
    [InlineData(PropertyType.SkilledNursing, 1.45)]
    [InlineData(PropertyType.AssistedLiving, 1.45)]
    [InlineData(PropertyType.MemoryCare, 1.45)]
    [InlineData(PropertyType.CCRC, 1.40)]
    [InlineData(PropertyType.BoardAndCare, 1.45)]
    [InlineData(PropertyType.IndependentLiving, 1.35)]
    [InlineData(PropertyType.SeniorApartment, 1.25)]
    public void MinDscr_ReturnsCorrectDefault(PropertyType type, double expected)
    {
        var result = ProtocolDefaults.GetMinDscr(type);
        Assert.Equal((decimal)expected, result);
    }

    // === LTV maximum thresholds ===
    [Theory]
    [InlineData(PropertyType.Multifamily, 80)]
    [InlineData(PropertyType.Bridge, 75)]
    [InlineData(PropertyType.Hospitality, 65)]
    [InlineData(PropertyType.Commercial, 75)]
    [InlineData(PropertyType.LIHTC, 85)]
    [InlineData(PropertyType.SkilledNursing, 85)]
    [InlineData(PropertyType.AssistedLiving, 85)]
    [InlineData(PropertyType.MemoryCare, 80)]
    [InlineData(PropertyType.CCRC, 80)]
    [InlineData(PropertyType.BoardAndCare, 80)]
    [InlineData(PropertyType.IndependentLiving, 80)]
    [InlineData(PropertyType.SeniorApartment, 80)]
    public void MaxLtv_ReturnsCorrectDefault(PropertyType type, decimal expected)
    {
        var result = ProtocolDefaults.GetMaxLtv(type);
        Assert.Equal(expected, result);
    }

    // === Classification helpers ===
    [Theory]
    [InlineData(PropertyType.BoardAndCare)]
    [InlineData(PropertyType.IndependentLiving)]
    [InlineData(PropertyType.SeniorApartment)]
    public void IsSeniorHousing_NewSeniorTypesReturnTrue(PropertyType type)
    {
        Assert.True(ProtocolDefaults.IsSeniorHousing(type));
    }

    [Theory]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    [InlineData(PropertyType.Hospitality)]
    public void IsSeniorHousing_NonSeniorReturnsFalse(PropertyType type)
    {
        Assert.False(ProtocolDefaults.IsSeniorHousing(type));
    }

    [Theory]
    [InlineData(PropertyType.SkilledNursing)]
    [InlineData(PropertyType.AssistedLiving)]
    [InlineData(PropertyType.MemoryCare)]
    [InlineData(PropertyType.CCRC)]
    [InlineData(PropertyType.BoardAndCare)]
    public void IsHealthcare_HealthcareTypesReturnTrue(PropertyType type)
    {
        Assert.True(ProtocolDefaults.IsHealthcare(type));
    }

    [Theory]
    [InlineData(PropertyType.Multifamily)]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.IndependentLiving)]
    [InlineData(PropertyType.SeniorApartment)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    public void IsHealthcare_NonHealthcareReturnsFalse(PropertyType type)
    {
        Assert.False(ProtocolDefaults.IsHealthcare(type));
    }

    // === Expense PUPA minimums ===
    [Fact]
    public void ExpensePupaMinimums_ContainsExpectedKeys()
    {
        var mins = ProtocolDefaults.ExpensePupaMinimums;
        Assert.True(mins.ContainsKey("RepairsAndMaintenance"));
        Assert.True(mins.ContainsKey("Marketing"));
        Assert.True(mins.ContainsKey("GeneralAndAdmin"));
        Assert.Equal(600m, mins["RepairsAndMaintenance"]);
        Assert.Equal(50m, mins["Marketing"]);
        Assert.Equal(250m, mins["GeneralAndAdmin"]);
    }

    // === Backward compatibility ===
    [Fact]
    public void ExistingMultifamilyDefaults_Unchanged()
    {
        Assert.Equal(95m, ProtocolDefaults.GetEffectiveOccupancy(null, PropertyType.Multifamily));
        Assert.Equal(0.5435m, ProtocolDefaults.GetEffectiveOpExRatio(PropertyType.Multifamily));
        Assert.Equal(0.135m, ProtocolDefaults.GetEffectiveOtherIncomeRatio(PropertyType.Multifamily));
    }

    [Fact]
    public void ExistingSeniorDefaults_Unchanged()
    {
        Assert.Equal(87m, ProtocolDefaults.GetEffectiveOccupancy(null, PropertyType.AssistedLiving));
        Assert.Equal(82m, ProtocolDefaults.GetEffectiveOccupancy(null, PropertyType.SkilledNursing));
        Assert.Equal(0.68m, ProtocolDefaults.GetEffectiveOpExRatio(PropertyType.AssistedLiving));
        Assert.Equal(0.75m, ProtocolDefaults.GetEffectiveOpExRatio(PropertyType.SkilledNursing));
    }
}
