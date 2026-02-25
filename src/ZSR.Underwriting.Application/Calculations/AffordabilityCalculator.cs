using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Calculations;

/// <summary>
/// Calculates property affordability based on HUD Area Median Income (AMI) thresholds.
/// Uses the standard 30% of gross income rule for housing affordability.
/// AMI tiers: 30% (Extremely Low), 50% (Very Low), 60% (LIHTC), 80% (Low), 100% (Moderate), 120% (Workforce).
/// </summary>
public class AffordabilityCalculator
{
    /// <summary>
    /// Standard HUD housing cost ratio — households should spend no more than 30% of income on housing.
    /// </summary>
    public const decimal HousingCostRatio = 0.30m;

    /// <summary>
    /// Calculates the maximum affordable monthly rent for a given annual income.
    /// Formula: (annualIncome * 30%) / 12
    /// </summary>
    public decimal CalculateMaxAffordableRent(decimal annualIncome)
        => Math.Round(annualIncome * HousingCostRatio / 12m, 0, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Calculates affordability analysis comparing property rent to HUD AMI tiers.
    /// Returns the lowest AMI tier at which the property is affordable, plus a full tier breakdown.
    /// </summary>
    public AffordabilityResultDto CalculateAffordability(
        decimal monthlyRentPerUnit,
        HudIncomeLimitsDto incomeLimits,
        int householdSize = 2)
    {
        var tiers = BuildAmiTiers(incomeLimits, householdSize, monthlyRentPerUnit);

        // Find the lowest AMI tier where rent is affordable
        var affordableTier = tiers.FirstOrDefault(t => t.IsAffordable);
        var amiPercent = affordableTier?.AmiPercent ?? 999;
        var tierLabel = amiPercent switch
        {
            30 => "Affordable at 30% AMI — Extremely Low Income",
            50 => "Affordable at 50% AMI — Very Low Income",
            60 => "Affordable at 60% AMI — LIHTC Threshold",
            80 => "Affordable at 80% AMI — Low Income",
            100 => "Affordable at 100% AMI — Moderate Income",
            120 => "Affordable at 120% AMI — Workforce Housing",
            _ => "Above 120% AMI — Market Rate"
        };

        return new AffordabilityResultDto
        {
            SubjectMonthlyRent = monthlyRentPerUnit,
            HouseholdSize = householdSize,
            AffordableAtAmiPercent = amiPercent,
            AffordabilityTier = tierLabel,
            MedianFamilyIncome = incomeLimits.MedianFamilyIncome,
            AreaName = incomeLimits.AreaName,
            AmiTiers = tiers
        };
    }

    private List<AmiTierRent> BuildAmiTiers(
        HudIncomeLimitsDto limits, int householdSize, decimal subjectRent)
    {
        // HUD provides 30%, 50%, 80% directly. We derive 60%, 100%, 120%.
        var extremelyLow = limits.ExtremelyLow.GetByHouseholdSize(householdSize); // 30% AMI
        var veryLow = limits.VeryLow.GetByHouseholdSize(householdSize);           // 50% AMI
        var low = limits.Low.GetByHouseholdSize(householdSize);                   // 80% AMI

        // 60% AMI = scale up from 50% AMI (60/50 = 1.2)
        var lihtc = (int)Math.Round(veryLow * 1.2m, 0);

        // 100% AMI = scale up from 80% AMI (100/80 = 1.25)
        var moderate = (int)Math.Round(low * 1.25m, 0);

        // 120% AMI = scale up from 100% AMI (120/100 = 1.2)
        var workforce = (int)Math.Round(moderate * 1.2m, 0);

        var tierDefs = new (int AmiPercent, string Label, int AnnualIncome)[]
        {
            (30, "Extremely Low Income", extremelyLow),
            (50, "Very Low Income", veryLow),
            (60, "LIHTC Threshold", lihtc),
            (80, "Low Income", low),
            (100, "Moderate Income", moderate),
            (120, "Workforce Housing", workforce)
        };

        return tierDefs.Select(t =>
        {
            var maxRent = CalculateMaxAffordableRent(t.AnnualIncome);
            return new AmiTierRent
            {
                AmiPercent = t.AmiPercent,
                Label = t.Label,
                AnnualIncomeLimit = t.AnnualIncome,
                MaxAffordableRent = maxRent,
                IsAffordable = subjectRent <= maxRent
            };
        }).ToList();
    }
}
