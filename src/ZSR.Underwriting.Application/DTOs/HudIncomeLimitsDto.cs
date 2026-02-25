namespace ZSR.Underwriting.Application.DTOs;

public class HudIncomeLimitsDto
{
    public decimal MedianFamilyIncome { get; set; }
    public HudIncomeLevel ExtremelyLow { get; set; } = new(); // 30% AMI
    public HudIncomeLevel VeryLow { get; set; } = new();      // 50% AMI
    public HudIncomeLevel Low { get; set; } = new();           // 80% AMI
    public string AreaName { get; set; } = string.Empty;
    public string FipsCode { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Source { get; set; } = "U.S. Department of Housing and Urban Development";
}

/// <summary>
/// Income limits by household size (1-8 persons) at a given AMI tier.
/// Fields match HUD IL API response (p1 through p8).
/// </summary>
public class HudIncomeLevel
{
    public int Person1 { get; set; }
    public int Person2 { get; set; }
    public int Person3 { get; set; }
    public int Person4 { get; set; }
    public int Person5 { get; set; }
    public int Person6 { get; set; }
    public int Person7 { get; set; }
    public int Person8 { get; set; }

    /// <summary>
    /// Gets the income limit for a given household size (1-8).
    /// </summary>
    public int GetByHouseholdSize(int size) => size switch
    {
        1 => Person1,
        2 => Person2,
        3 => Person3,
        4 => Person4,
        5 => Person5,
        6 => Person6,
        7 => Person7,
        8 => Person8,
        _ => Person4 // Default to 4-person household
    };
}
