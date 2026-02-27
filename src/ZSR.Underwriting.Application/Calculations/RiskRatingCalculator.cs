using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Application.Calculations;

public static class RiskRatingCalculator
{
    public static RiskSeverity RateRentPremium(decimal subjectRent, decimal marketRent)
    {
        if (marketRent == 0m) return RiskSeverity.Low;

        var premiumPercent = (subjectRent - marketRent) / marketRent * 100m;

        return premiumPercent switch
        {
            >= 15m => RiskSeverity.Critical,
            >= 10m => RiskSeverity.High,
            >= 5m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    /// <summary>
    /// Rates DSCR using hardcoded 1.25x baseline (legacy — for non-Fannie deals).
    /// </summary>
    public static RiskSeverity RateDscr(decimal dscr)
    {
        return RateDscr(dscr, 1.25m);
    }

    /// <summary>
    /// Rates DSCR using a product-specific minimum as baseline.
    /// Tiers: below min → tiers based on distance from min DSCR.
    /// </summary>
    public static RiskSeverity RateDscr(decimal dscr, decimal productMinDscr)
    {
        if (dscr < productMinDscr * 0.80m) return RiskSeverity.Critical;  // >20% below min
        if (dscr < productMinDscr * 0.92m) return RiskSeverity.High;      // 8-20% below min
        if (dscr < productMinDscr) return RiskSeverity.Moderate;           // 0-8% below min
        return RiskSeverity.Low;                                            // At or above min
    }

    public static RiskSeverity RateOccupancyGap(decimal subjectOccupancy, decimal marketOccupancy)
    {
        var gap = marketOccupancy - subjectOccupancy;

        return gap switch
        {
            >= 20m => RiskSeverity.Critical,
            >= 10m => RiskSeverity.High,
            >= 5m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    public static RiskSeverity RateFicoGap(int subjectFico, int metroFico)
    {
        var gap = metroFico - subjectFico;

        return gap switch
        {
            >= 75 => RiskSeverity.Critical,
            >= 50 => RiskSeverity.High,
            >= 25 => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    // === Fannie Mae product-specific risk ratings ===

    /// <summary>
    /// Seniors Housing: rates SNF NCF as a percentage of total property NCF.
    /// SNF NCF > 20% → Critical, > 15% → High, > 10% → Moderate.
    /// </summary>
    public static RiskSeverity RateSeniorsSkilledNursing(decimal snfNcfPercent)
    {
        return snfNcfPercent switch
        {
            > 20m => RiskSeverity.Critical,
            > 15m => RiskSeverity.High,
            > 10m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    /// <summary>
    /// Student Housing: rates nearby university enrollment for Dedicated properties.
    /// Enrollment < 5K → Critical, < 10K → High, < 15K → Moderate.
    /// </summary>
    public static RiskSeverity RateStudentEnrollment(int enrollment)
    {
        return enrollment switch
        {
            < 5_000 => RiskSeverity.Critical,
            < 10_000 => RiskSeverity.High,
            < 15_000 => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    /// <summary>
    /// MHC: rates tenant-occupied homes as percentage of total.
    /// > 50% → Critical, > 35% → High, > 25% → Moderate.
    /// </summary>
    public static RiskSeverity RateMhcTenantOccupied(decimal tenantOccupiedPercent)
    {
        return tenantOccupiedPercent switch
        {
            > 50m => RiskSeverity.Critical,
            > 35m => RiskSeverity.High,
            > 25m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    /// <summary>
    /// Cooperative: rates single sponsor concentration as percentage of units.
    /// > 60% → High, > 40% → Moderate.
    /// </summary>
    public static RiskSeverity RateCoopSponsorConcentration(decimal sponsorOwnershipPercent)
    {
        return sponsorOwnershipPercent switch
        {
            > 60m => RiskSeverity.High,
            > 40m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }

    /// <summary>
    /// Affordable Housing: rates hard subordinate debt combined DSCR.
    /// < 1.00x → Critical, < 1.05x → High, < 1.10x → Moderate.
    /// </summary>
    public static RiskSeverity RateAffordableSubDebt(decimal combinedDscr)
    {
        return combinedDscr switch
        {
            < 1.00m => RiskSeverity.Critical,
            < 1.05m => RiskSeverity.High,
            < 1.10m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }
}
