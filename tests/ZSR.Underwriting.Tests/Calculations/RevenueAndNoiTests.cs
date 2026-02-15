using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Domain.Interfaces;

namespace ZSR.Underwriting.Tests.Calculations;

public class RevenueAndNoiTests
{
    private readonly IUnderwritingCalculator _calc = new UnderwritingCalculator();

    // --- GPR ---

    [Fact]
    public void CalculateGpr_StandardInputs_ReturnsAnnualRent()
    {
        var result = _calc.CalculateGpr(1200m, 100);
        Assert.Equal(1_440_000m, result);
    }

    [Fact]
    public void CalculateGpr_SingleUnit_ReturnsCorrect()
    {
        var result = _calc.CalculateGpr(950m, 1);
        Assert.Equal(11_400m, result);
    }

    [Fact]
    public void CalculateGpr_ZeroUnits_ReturnsZero()
    {
        var result = _calc.CalculateGpr(1200m, 0);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateGpr_ZeroRent_ReturnsZero()
    {
        var result = _calc.CalculateGpr(0m, 100);
        Assert.Equal(0m, result);
    }

    // --- Vacancy Loss ---

    [Fact]
    public void CalculateVacancyLoss_93PercentOccupancy_Returns7PercentOfGpr()
    {
        var result = _calc.CalculateVacancyLoss(1_440_000m, 93m);
        Assert.Equal(100_800m, result);
    }

    [Fact]
    public void CalculateVacancyLoss_100PercentOccupancy_ReturnsZero()
    {
        var result = _calc.CalculateVacancyLoss(1_440_000m, 100m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateVacancyLoss_ZeroOccupancy_ReturnsFullGpr()
    {
        var result = _calc.CalculateVacancyLoss(1_440_000m, 0m);
        Assert.Equal(1_440_000m, result);
    }

    // --- Net Rent ---

    [Fact]
    public void CalculateNetRent_ReturnsGprMinusVacancy()
    {
        var result = _calc.CalculateNetRent(1_440_000m, 100_800m);
        Assert.Equal(1_339_200m, result);
    }

    // --- Other Income ---

    [Fact]
    public void CalculateOtherIncome_Default135Percent_ReturnsCorrect()
    {
        var result = _calc.CalculateOtherIncome(1_339_200m);
        Assert.Equal(180_792.00m, result);
    }

    [Fact]
    public void CalculateOtherIncome_CustomPercent_ReturnsCorrect()
    {
        var result = _calc.CalculateOtherIncome(1_339_200m, null, 0.10m);
        Assert.Equal(133_920m, result);
    }

    [Fact]
    public void CalculateOtherIncome_ActualDollarAmount_OverridesFormula()
    {
        var result = _calc.CalculateOtherIncome(1_339_200m, 200_000m);
        Assert.Equal(200_000m, result);
    }

    [Fact]
    public void CalculateOtherIncome_ActualZero_ReturnsZero()
    {
        var result = _calc.CalculateOtherIncome(1_339_200m, 0m);
        Assert.Equal(0m, result);
    }

    // --- EGI ---

    [Fact]
    public void CalculateEgi_ReturnsNetRentPlusOtherIncome()
    {
        var result = _calc.CalculateEgi(1_339_200m, 180_792m);
        Assert.Equal(1_519_992m, result);
    }

    // --- Operating Expenses ---

    [Fact]
    public void CalculateOperatingExpenses_DefaultRatio_Returns5435PercentOfEgi()
    {
        var result = _calc.CalculateOperatingExpenses(1_519_992m, null);
        Assert.Equal(826_115.65m, result);
    }

    [Fact]
    public void CalculateOperatingExpenses_ActualExpenses_OverridesRatio()
    {
        var result = _calc.CalculateOperatingExpenses(1_519_992m, 750_000m);
        Assert.Equal(750_000m, result);
    }

    [Fact]
    public void CalculateOperatingExpenses_CustomRatio_AppliesCorrectly()
    {
        var result = _calc.CalculateOperatingExpenses(1_519_992m, null, 0.50m);
        Assert.Equal(759_996m, result);
    }

    // --- NOI ---

    [Fact]
    public void CalculateNoi_ReturnsEgiMinusOpEx()
    {
        var result = _calc.CalculateNoi(1_519_992m, 826_115.65m);
        Assert.Equal(693_876.35m, result);
    }

    // --- NOI Margin ---

    [Fact]
    public void CalculateNoiMargin_ReturnsNoiOverEgi()
    {
        var result = _calc.CalculateNoiMargin(693_876.35m, 1_519_992m);
        Assert.Equal(45.7m, result);
    }

    [Fact]
    public void CalculateNoiMargin_ZeroEgi_ReturnsZero()
    {
        var result = _calc.CalculateNoiMargin(0m, 0m);
        Assert.Equal(0m, result);
    }

    // --- Edge Cases ---

    [Fact]
    public void CalculateNoi_NegativeNoi_HandledCorrectly()
    {
        var result = _calc.CalculateNoi(100_000m, 150_000m);
        Assert.Equal(-50_000m, result);
    }

    // --- Full Pipeline Integration ---

    [Fact]
    public void FullPipeline_100Units_1200Rent_93Occupancy_ProducesCorrectNoi()
    {
        var gpr = _calc.CalculateGpr(1200m, 100);
        var vacancyLoss = _calc.CalculateVacancyLoss(gpr, 93m);
        var netRent = _calc.CalculateNetRent(gpr, vacancyLoss);
        var otherIncome = _calc.CalculateOtherIncome(netRent);
        var egi = _calc.CalculateEgi(netRent, otherIncome);
        var opEx = _calc.CalculateOperatingExpenses(egi, null);
        var noi = _calc.CalculateNoi(egi, opEx);
        var noiMargin = _calc.CalculateNoiMargin(noi, egi);

        Assert.Equal(1_440_000m, gpr);
        Assert.Equal(100_800m, vacancyLoss);
        Assert.Equal(1_339_200m, netRent);
        Assert.Equal(180_792m, otherIncome);
        Assert.Equal(1_519_992m, egi);
        Assert.Equal(826_115.65m, opEx);
        Assert.Equal(693_876.35m, noi);
        Assert.Equal(45.7m, noiMargin);
    }
}
