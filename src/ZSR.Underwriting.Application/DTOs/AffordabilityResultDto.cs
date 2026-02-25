namespace ZSR.Underwriting.Application.DTOs;

/// <summary>
/// Result of the HUD affordability analysis for a property.
/// Compares property rents against AMI-based affordable rent thresholds.
/// </summary>
public class AffordabilityResultDto
{
    /// <summary>The property's monthly rent per unit used for comparison.</summary>
    public decimal SubjectMonthlyRent { get; set; }

    /// <summary>Household size assumed for the analysis (default: 2).</summary>
    public int HouseholdSize { get; set; }

    /// <summary>The lowest AMI percentage at which the property rent is affordable (e.g. 80).</summary>
    public int AffordableAtAmiPercent { get; set; }

    /// <summary>Human-readable tier label (e.g. "Affordable at 80% AMI — Low Income").</summary>
    public string AffordabilityTier { get; set; } = string.Empty;

    /// <summary>Area Median Family Income from HUD.</summary>
    public decimal MedianFamilyIncome { get; set; }

    /// <summary>Max affordable rent at each AMI tier for the given household size.</summary>
    public List<AmiTierRent> AmiTiers { get; set; } = new();

    /// <summary>HUD area name used for the lookup.</summary>
    public string AreaName { get; set; } = string.Empty;

    /// <summary>Data source attribution.</summary>
    public string Source { get; set; } = "U.S. Department of Housing and Urban Development";
}

/// <summary>
/// Max affordable monthly rent at a specific AMI tier.
/// </summary>
public class AmiTierRent
{
    /// <summary>AMI percentage (e.g. 30, 50, 60, 80, 100, 120).</summary>
    public int AmiPercent { get; set; }

    /// <summary>Tier label (e.g. "Extremely Low Income").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Annual income limit at this tier for the household size.</summary>
    public decimal AnnualIncomeLimit { get; set; }

    /// <summary>Max affordable monthly rent (income * 30% / 12).</summary>
    public decimal MaxAffordableRent { get; set; }

    /// <summary>Whether the property rent is at or below this tier's max affordable rent.</summary>
    public bool IsAffordable { get; set; }
}
