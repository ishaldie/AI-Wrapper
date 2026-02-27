using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Application.Calculations;

/// <summary>
/// Runs all applicable Fannie Mae product-specific risk ratings and returns
/// a compliance risk summary.
/// </summary>
public static class FannieComplianceRiskAssessment
{
    public static FannieRiskSummary Assess(
        FannieProductType productType,
        decimal dscr,
        decimal ltvPercent,
        FannieComplianceInputs? inputs = null)
    {
        var profile = FannieProductProfiles.Get(productType);
        var ratings = new List<FannieRiskRating>();

        // Core: product-aware DSCR rating
        var dscrRating = RiskRatingCalculator.RateDscr(dscr, profile.MinDscr);
        ratings.Add(new FannieRiskRating
        {
            Category = "DSCR",
            Severity = dscrRating,
            Description = $"DSCR {dscr:F2}x vs. {profile.MinDscr:F2}x minimum for {profile.DisplayName}"
        });

        // Product-specific ratings
        if (inputs != null)
        {
            switch (productType)
            {
                case FannieProductType.SeniorsIL:
                case FannieProductType.SeniorsAL:
                case FannieProductType.SeniorsALZ:
                    if (inputs.SnfNcf.HasValue)
                    {
                        // Calculate SNF % — use a rough proxy if total NCF not directly available
                        var totalNcf = inputs.SnfNcf.Value > 0 ? inputs.SnfNcf.Value / 0.20m : 0m; // Estimate
                        // Better: caller should provide actual SNF % or we derive from compliance result
                        var snfSeverity = RiskRatingCalculator.RateSeniorsSkilledNursing(
                            inputs.SnfNcfPercent ?? 0m);
                        ratings.Add(new FannieRiskRating
                        {
                            Category = "SNF NCF Concentration",
                            Severity = snfSeverity,
                            Description = $"Skilled Nursing NCF is {inputs.SnfNcfPercent ?? 0:F1}% of total property NCF"
                        });
                    }
                    break;

                case FannieProductType.StudentHousing:
                    if (inputs.NearbyEnrollment.HasValue)
                    {
                        var enrollSeverity = RiskRatingCalculator.RateStudentEnrollment(inputs.NearbyEnrollment.Value);
                        ratings.Add(new FannieRiskRating
                        {
                            Category = "University Enrollment",
                            Severity = enrollSeverity,
                            Description = $"Nearby university enrollment: {inputs.NearbyEnrollment.Value:N0} students"
                        });
                    }
                    break;

                case FannieProductType.ManufacturedHousing:
                    if (inputs.TenantOccupiedPercent.HasValue)
                    {
                        var mhcSeverity = RiskRatingCalculator.RateMhcTenantOccupied(inputs.TenantOccupiedPercent.Value);
                        ratings.Add(new FannieRiskRating
                        {
                            Category = "MHC Tenant-Occupied Homes",
                            Severity = mhcSeverity,
                            Description = $"Tenant-occupied homes: {inputs.TenantOccupiedPercent.Value:F1}% (max 35%)"
                        });
                    }
                    break;

                case FannieProductType.Cooperative:
                    if (inputs.SponsorOwnershipPercent.HasValue)
                    {
                        var coopSeverity = RiskRatingCalculator.RateCoopSponsorConcentration(inputs.SponsorOwnershipPercent.Value);
                        ratings.Add(new FannieRiskRating
                        {
                            Category = "Co-op Sponsor Concentration",
                            Severity = coopSeverity,
                            Description = $"Single sponsor owns {inputs.SponsorOwnershipPercent.Value:F1}% of units"
                        });
                    }
                    break;

                case FannieProductType.AffordableHousing:
                    if (inputs.SubDebtCombinedDscr.HasValue)
                    {
                        var affordSeverity = RiskRatingCalculator.RateAffordableSubDebt(inputs.SubDebtCombinedDscr.Value);
                        ratings.Add(new FannieRiskRating
                        {
                            Category = "Subordinate Debt DSCR",
                            Severity = affordSeverity,
                            Description = $"Hard sub combined DSCR: {inputs.SubDebtCombinedDscr.Value:F2}x (min 1.05x)"
                        });
                    }
                    break;
            }
        }

        // Determine overall severity (worst of all ratings)
        var worstSeverity = ratings.Max(r => r.Severity);

        return new FannieRiskSummary
        {
            ProductType = productType,
            ProductDisplayName = profile.DisplayName,
            OverallSeverity = worstSeverity,
            Ratings = ratings
        };
    }
}

/// <summary>
/// Aggregated risk assessment summary for a Fannie Mae product type.
/// </summary>
public sealed record FannieRiskSummary
{
    public FannieProductType ProductType { get; init; }
    public string ProductDisplayName { get; init; } = string.Empty;
    public RiskSeverity OverallSeverity { get; init; }
    public List<FannieRiskRating> Ratings { get; init; } = new();
}

/// <summary>
/// A single risk rating within the Fannie compliance assessment.
/// </summary>
public sealed record FannieRiskRating
{
    public string Category { get; init; } = string.Empty;
    public RiskSeverity Severity { get; init; }
    public string Description { get; init; } = string.Empty;
}
