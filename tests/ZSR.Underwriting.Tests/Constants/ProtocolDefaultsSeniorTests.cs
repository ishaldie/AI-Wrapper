using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Constants;

public class ProtocolDefaultsSeniorTests
{
    [Fact]
    public void MultifamilyOccupancy_Returns95()
    {
        var result = ProtocolDefaults.GetEffectiveOccupancy(null, PropertyType.Multifamily);
        Assert.Equal(95m, result);
    }

    [Fact]
    public void AssistedLivingOccupancy_Returns87()
    {
        var result = ProtocolDefaults.GetEffectiveOccupancy(null, PropertyType.AssistedLiving);
        Assert.Equal(87m, result);
    }

    [Fact]
    public void SkilledNursingOccupancy_Returns82()
    {
        var result = ProtocolDefaults.GetEffectiveOccupancy(null, PropertyType.SkilledNursing);
        Assert.Equal(82m, result);
    }

    [Fact]
    public void MemoryCareOccupancy_Returns85()
    {
        var result = ProtocolDefaults.GetEffectiveOccupancy(null, PropertyType.MemoryCare);
        Assert.Equal(85m, result);
    }

    [Fact]
    public void CcrcOccupancy_Returns90()
    {
        var result = ProtocolDefaults.GetEffectiveOccupancy(null, PropertyType.CCRC);
        Assert.Equal(90m, result);
    }

    [Fact]
    public void UserOverride_TakesPrecedence()
    {
        var result = ProtocolDefaults.GetEffectiveOccupancy(92m, PropertyType.SkilledNursing);
        Assert.Equal(92m, result);
    }

    [Fact]
    public void MultifamilyOpExRatio_Returns5435()
    {
        var result = ProtocolDefaults.GetEffectiveOpExRatio(PropertyType.Multifamily);
        Assert.Equal(0.5435m, result);
    }

    [Fact]
    public void AssistedLivingOpExRatio_Returns68()
    {
        var result = ProtocolDefaults.GetEffectiveOpExRatio(PropertyType.AssistedLiving);
        Assert.Equal(0.68m, result);
    }

    [Fact]
    public void SkilledNursingOpExRatio_Returns75()
    {
        var result = ProtocolDefaults.GetEffectiveOpExRatio(PropertyType.SkilledNursing);
        Assert.Equal(0.75m, result);
    }

    [Fact]
    public void MultifamilyOtherIncomeRatio_Returns135()
    {
        var result = ProtocolDefaults.GetEffectiveOtherIncomeRatio(PropertyType.Multifamily);
        Assert.Equal(0.135m, result);
    }

    [Fact]
    public void SeniorOtherIncomeRatio_Returns5()
    {
        var result = ProtocolDefaults.GetEffectiveOtherIncomeRatio(PropertyType.AssistedLiving);
        Assert.Equal(0.05m, result);
    }

    [Fact]
    public void IsSeniorHousing_MultifamilyReturnsFalse()
    {
        Assert.False(ProtocolDefaults.IsSeniorHousing(PropertyType.Multifamily));
    }

    [Theory]
    [InlineData(PropertyType.AssistedLiving)]
    [InlineData(PropertyType.SkilledNursing)]
    [InlineData(PropertyType.MemoryCare)]
    [InlineData(PropertyType.CCRC)]
    public void IsSeniorHousing_SeniorTypesReturnTrue(PropertyType type)
    {
        Assert.True(ProtocolDefaults.IsSeniorHousing(type));
    }

    [Fact]
    public void ExistingDefaults_Unchanged()
    {
        // Verify backward compatibility
        Assert.Equal(65m, ProtocolDefaults.LoanLtv);
        Assert.Equal(5, ProtocolDefaults.HoldPeriodYears);
        Assert.Equal(95m, ProtocolDefaults.TargetOccupancy);
        Assert.Equal(30, ProtocolDefaults.AmortizationYears);
        Assert.Equal(5, ProtocolDefaults.LoanTermYears);
    }
}
