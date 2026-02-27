using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Application.Calculations;

/// <summary>
/// Runs all applicable Freddie Mac product-specific risk ratings and returns
/// a compliance risk summary.
/// </summary>
public static class FreddieComplianceRiskAssessment
{
    public static FreddieRiskSummary Assess(
        FreddieProductType productType,
        decimal dscr,
        decimal ltvPercent,
        FreddieComplianceInputs? inputs = null)
    {
        var profile = FreddieProductProfiles.Get(productType);
        var ratings = new List<FreddieRiskRating>();

        // Core: product-aware DSCR rating
        var dscrRating = RiskRatingCalculator.RateDscr(dscr, profile.MinDscr);
        ratings.Add(new FreddieRiskRating
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
                case FreddieProductType.SeniorsIL:
                case FreddieProductType.SeniorsAL:
                case FreddieProductType.SeniorsSN:
                    if (inputs.SnfNoiPercent.HasValue)
                    {
                        var snfSeverity = RiskRatingCalculator.RateSeniorsSkilledNursing(inputs.SnfNoiPercent.Value);
                        ratings.Add(new FreddieRiskRating
                        {
                            Category = "SNF NOI Concentration",
                            Severity = snfSeverity,
                            Description = $"Skilled Nursing NOI is {inputs.SnfNoiPercent.Value:F1}% of total property NOI"
                        });
                    }
                    break;

                case FreddieProductType.StudentHousing:
                    if (inputs.NearbyEnrollment.HasValue)
                    {
                        var enrollSeverity = RiskRatingCalculator.RateStudentEnrollment(inputs.NearbyEnrollment.Value);
                        ratings.Add(new FreddieRiskRating
                        {
                            Category = "University Enrollment",
                            Severity = enrollSeverity,
                            Description = $"Nearby university enrollment: {inputs.NearbyEnrollment.Value:N0} students"
                        });
                    }
                    break;

                case FreddieProductType.ManufacturedHousing:
                    if (inputs.RentalHomesPercent.HasValue)
                    {
                        var mhcSeverity = RateMhcRentalHomes(inputs.RentalHomesPercent.Value);
                        ratings.Add(new FreddieRiskRating
                        {
                            Category = "MHC Rental Homes",
                            Severity = mhcSeverity,
                            Description = $"Rental homes: {inputs.RentalHomesPercent.Value:F1}% (max 25%)"
                        });
                    }
                    break;

                case FreddieProductType.FloatingRate:
                    if (!inputs.HasRateCap && ltvPercent > 60m)
                    {
                        ratings.Add(new FreddieRiskRating
                        {
                            Category = "Floating Rate Cap",
                            Severity = RiskSeverity.High,
                            Description = $"Rate cap required at LTV > 60% (actual LTV: {ltvPercent:F1}%) — not in place"
                        });
                    }
                    break;

                case FreddieProductType.ValueAdd:
                    if (inputs.IsRehabPeriod)
                    {
                        var rehabRating = RiskRatingCalculator.RateDscr(dscr, 1.10m);
                        ratings.Add(new FreddieRiskRating
                        {
                            Category = "Value-Add Rehab DSCR",
                            Severity = rehabRating,
                            Description = $"Rehab-period DSCR {dscr:F2}x vs. 1.10x IO minimum"
                        });
                    }
                    break;
            }
        }

        // Determine overall severity (worst of all ratings)
        var worstSeverity = ratings.Max(r => r.Severity);

        return new FreddieRiskSummary
        {
            ProductType = productType,
            ProductDisplayName = profile.DisplayName,
            OverallSeverity = worstSeverity,
            Ratings = ratings
        };
    }

    /// <summary>
    /// MHC rental homes risk rating — Freddie-specific (25% cap vs Fannie's 35%).
    /// > 35% → Critical, > 25% → High, > 15% → Moderate.
    /// </summary>
    public static RiskSeverity RateMhcRentalHomes(decimal rentalHomesPercent)
    {
        return rentalHomesPercent switch
        {
            > 35m => RiskSeverity.Critical,
            > 25m => RiskSeverity.High,
            > 15m => RiskSeverity.Moderate,
            _ => RiskSeverity.Low,
        };
    }
}

/// <summary>
/// Aggregated risk assessment summary for a Freddie Mac product type.
/// </summary>
public sealed record FreddieRiskSummary
{
    public FreddieProductType ProductType { get; init; }
    public string ProductDisplayName { get; init; } = string.Empty;
    public RiskSeverity OverallSeverity { get; init; }
    public List<FreddieRiskRating> Ratings { get; init; } = new();
}

/// <summary>
/// A single risk rating within the Freddie compliance assessment.
/// </summary>
public sealed record FreddieRiskRating
{
    public string Category { get; init; } = string.Empty;
    public RiskSeverity Severity { get; init; }
    public string Description { get; init; } = string.Empty;
}
