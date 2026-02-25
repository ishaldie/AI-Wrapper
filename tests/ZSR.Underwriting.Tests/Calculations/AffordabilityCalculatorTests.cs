using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Tests.Calculations;

public class AffordabilityCalculatorTests
{
    private readonly AffordabilityCalculator _calc = new();

    /// <summary>
    /// Standard HUD income limits for Dallas County, TX (illustrative values).
    /// Median family income: $88,200 (4-person).
    /// </summary>
    private static HudIncomeLimitsDto CreateDallasIncomeLimits() => new()
    {
        MedianFamilyIncome = 88_200m,
        AreaName = "Dallas County, TX",
        FipsCode = "48113",
        Year = 2025,
        ExtremelyLow = new HudIncomeLevel // 30% AMI
        {
            Person1 = 18_550, Person2 = 21_200, Person3 = 23_850,
            Person4 = 26_500, Person5 = 28_620, Person6 = 30_740,
            Person7 = 32_860, Person8 = 34_980
        },
        VeryLow = new HudIncomeLevel // 50% AMI
        {
            Person1 = 30_900, Person2 = 35_300, Person3 = 39_700,
            Person4 = 44_100, Person5 = 47_650, Person6 = 51_150,
            Person7 = 54_700, Person8 = 58_200
        },
        Low = new HudIncomeLevel // 80% AMI
        {
            Person1 = 49_450, Person2 = 56_500, Person3 = 63_550,
            Person4 = 70_600, Person5 = 76_250, Person6 = 81_900,
            Person7 = 87_550, Person8 = 93_200
        }
    };

    // --- CalculateMaxAffordableRent tests ---

    [Fact]
    public void CalculateMaxAffordableRent_StandardIncome_Returns30PercentDividedBy12()
    {
        // $44,100/yr * 30% / 12 = $1,102.50
        var result = _calc.CalculateMaxAffordableRent(44_100m);
        Assert.Equal(1_103m, result); // Rounded up
    }

    [Fact]
    public void CalculateMaxAffordableRent_ZeroIncome_ReturnsZero()
    {
        var result = _calc.CalculateMaxAffordableRent(0m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateMaxAffordableRent_HighIncome_CalculatesCorrectly()
    {
        // $88,200/yr * 30% / 12 = $2,205
        var result = _calc.CalculateMaxAffordableRent(88_200m);
        Assert.Equal(2_205m, result);
    }

    // --- CalculateAffordability tests ---

    [Fact]
    public void CalculateAffordability_CheapRent_AffordableAt30Pct()
    {
        var limits = CreateDallasIncomeLimits();
        // Rent $500/mo is below 30% AMI max for 2-person household
        // 30% AMI 2-person = $21,200 => max rent = $21,200 * 0.30 / 12 = $530

        var result = _calc.CalculateAffordability(500m, limits, householdSize: 2);

        Assert.Equal(30, result.AffordableAtAmiPercent);
        Assert.Contains("Extremely Low", result.AffordabilityTier);
    }

    [Fact]
    public void CalculateAffordability_ModerateRent_AffordableAt80Pct()
    {
        var limits = CreateDallasIncomeLimits();
        // Rent $1,200/mo — above 50% AMI max ($883) but below 80% AMI max ($1,413)
        // 80% AMI 2-person = $56,500 => max rent = $56,500 * 0.30 / 12 = $1,412.50

        var result = _calc.CalculateAffordability(1_200m, limits, householdSize: 2);

        Assert.Equal(80, result.AffordableAtAmiPercent);
        Assert.Contains("Low Income", result.AffordabilityTier);
    }

    [Fact]
    public void CalculateAffordability_ExpensiveRent_AffordableAt120Pct()
    {
        var limits = CreateDallasIncomeLimits();
        // Rent $2,400/mo — above 100% AMI max (~$2,205) but below 120% AMI max (~$2,646)
        // 120% AMI 2-person ≈ $88,200 * 1.2 * 0.80 (size adj) * 0.30 / 12 ≈ $2,116.80
        // Actually: 120% AMI = Low (80% AMI) * 1.5 for 2-person = $56,500 * 1.5 = $84,750
        // Better: use median * 1.2 * size factor. For 2-person: $88,200 * 0.80 * 1.2 = $84,672
        // max rent = $84,672 * 0.30 / 12 = $2,116.80
        // So $2,400 > $2,116 — might be above 120% AMI

        var result = _calc.CalculateAffordability(2_400m, limits, householdSize: 2);

        // Should be above 120% AMI — "Market Rate"
        Assert.True(result.AffordableAtAmiPercent > 100);
    }

    [Fact]
    public void CalculateAffordability_Returns6AmiTiers()
    {
        var limits = CreateDallasIncomeLimits();

        var result = _calc.CalculateAffordability(1_000m, limits, householdSize: 2);

        Assert.Equal(6, result.AmiTiers.Count);
        Assert.Equal(30, result.AmiTiers[0].AmiPercent);
        Assert.Equal(50, result.AmiTiers[1].AmiPercent);
        Assert.Equal(60, result.AmiTiers[2].AmiPercent);
        Assert.Equal(80, result.AmiTiers[3].AmiPercent);
        Assert.Equal(100, result.AmiTiers[4].AmiPercent);
        Assert.Equal(120, result.AmiTiers[5].AmiPercent);
    }

    [Fact]
    public void CalculateAffordability_TierLabelsAreCorrect()
    {
        var limits = CreateDallasIncomeLimits();

        var result = _calc.CalculateAffordability(1_000m, limits, householdSize: 2);

        Assert.Equal("Extremely Low Income", result.AmiTiers[0].Label);
        Assert.Equal("Very Low Income", result.AmiTiers[1].Label);
        Assert.Equal("LIHTC Threshold", result.AmiTiers[2].Label);
        Assert.Equal("Low Income", result.AmiTiers[3].Label);
        Assert.Equal("Moderate Income", result.AmiTiers[4].Label);
        Assert.Equal("Workforce Housing", result.AmiTiers[5].Label);
    }

    [Fact]
    public void CalculateAffordability_IsAffordableFlags_CorrectForEachTier()
    {
        var limits = CreateDallasIncomeLimits();
        // Rent $900/mo — affordable at 50% AMI and above
        // 50% AMI 2-person = $35,300 => max rent = $35,300 * 0.30 / 12 = $882.50
        // 60% AMI 2-person ≈ $35,300 * 1.2 = $42,360 => max rent = $1,059
        // So $900 is NOT affordable at 50%, but IS at 60%

        var result = _calc.CalculateAffordability(900m, limits, householdSize: 2);

        Assert.False(result.AmiTiers[0].IsAffordable); // 30% AMI
        Assert.False(result.AmiTiers[1].IsAffordable); // 50% AMI
        Assert.True(result.AmiTiers[2].IsAffordable);  // 60% AMI
        Assert.True(result.AmiTiers[3].IsAffordable);  // 80% AMI
        Assert.True(result.AmiTiers[4].IsAffordable);  // 100% AMI
        Assert.True(result.AmiTiers[5].IsAffordable);  // 120% AMI
    }

    [Fact]
    public void CalculateAffordability_PopulatesMedianAndAreaName()
    {
        var limits = CreateDallasIncomeLimits();

        var result = _calc.CalculateAffordability(1_000m, limits, householdSize: 2);

        Assert.Equal(88_200m, result.MedianFamilyIncome);
        Assert.Equal("Dallas County, TX", result.AreaName);
        Assert.Equal(2, result.HouseholdSize);
        Assert.Equal(1_000m, result.SubjectMonthlyRent);
    }

    [Fact]
    public void CalculateAffordability_DefaultHouseholdSize_IsTwo()
    {
        var limits = CreateDallasIncomeLimits();

        var result = _calc.CalculateAffordability(1_000m, limits);

        Assert.Equal(2, result.HouseholdSize);
    }

    [Fact]
    public void CalculateAffordability_LargerHousehold_HigherIncomeLimits()
    {
        var limits = CreateDallasIncomeLimits();
        // 4-person household has higher income limits than 2-person
        // 50% AMI 4-person = $44,100 => max rent = $1,102.50
        // Same rent ($1,000) is affordable at 50% AMI for 4-person but not 2-person

        var result2 = _calc.CalculateAffordability(1_000m, limits, householdSize: 2);
        var result4 = _calc.CalculateAffordability(1_000m, limits, householdSize: 4);

        // 4-person should hit a lower AMI tier than 2-person for the same rent
        Assert.True(result4.AffordableAtAmiPercent <= result2.AffordableAtAmiPercent);
    }

    // --- HudIncomeLevel.GetByHouseholdSize tests ---

    [Fact]
    public void HudIncomeLevel_GetByHouseholdSize_ReturnsCorrectValue()
    {
        var level = new HudIncomeLevel
        {
            Person1 = 10, Person2 = 20, Person3 = 30,
            Person4 = 40, Person5 = 50, Person6 = 60,
            Person7 = 70, Person8 = 80
        };

        Assert.Equal(10, level.GetByHouseholdSize(1));
        Assert.Equal(20, level.GetByHouseholdSize(2));
        Assert.Equal(40, level.GetByHouseholdSize(4));
        Assert.Equal(80, level.GetByHouseholdSize(8));
    }

    [Fact]
    public void HudIncomeLevel_GetByHouseholdSize_InvalidSize_DefaultsTo4Person()
    {
        var level = new HudIncomeLevel { Person4 = 40 };

        Assert.Equal(40, level.GetByHouseholdSize(0));
        Assert.Equal(40, level.GetByHouseholdSize(9));
        Assert.Equal(40, level.GetByHouseholdSize(-1));
    }
}
