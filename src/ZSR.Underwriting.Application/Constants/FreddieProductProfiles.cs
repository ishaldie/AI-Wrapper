using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Application.Constants;

/// <summary>
/// Static registry of Freddie Mac product profiles sourced from official
/// term sheets at mf.freddiemac.com (reviewed 2026-02-27).
/// </summary>
public static class FreddieProductProfiles
{
    private static readonly Dictionary<FreddieProductType, FreddieProductProfile> _profiles = new()
    {
        [FreddieProductType.Conventional] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.Conventional,
            DisplayName = "Conventional Loans",
            MaxLtvPercent = 80m,
            MinDscr = 1.25m,
            MaxAmortizationYears = 30,
            MinLoanAmount = 5_000_000m,
            Notes = "Standard fixed-rate; stabilized properties; $5M+ typical"
        },
        [FreddieProductType.SmallBalanceLoan] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.SmallBalanceLoan,
            DisplayName = "Small Balance Loan (SBL)",
            MaxLtvPercent = 80m,
            MinDscr = 1.20m,
            MaxAmortizationYears = 30,
            MinLoanAmount = 1_000_000m,
            MaxLoanAmount = 7_500_000m,
            SblMarketTier = "Top/Standard/Small",
            SblTierMaxLtv = 80m,
            SblTierMinDscr = 1.20m,
            Notes = "$1M–$7.5M; tiered by market size (Top/Standard/Small); streamlined process"
        },
        [FreddieProductType.TargetedAffordable] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.TargetedAffordable,
            DisplayName = "Targeted Affordable Housing",
            MaxLtvPercent = 80m,
            MinDscr = 1.20m,
            MaxAmortizationYears = 30,
            Notes = "Income/rent restricted properties; HAP contracts; LIHTC; regulatory agreements"
        },
        [FreddieProductType.SeniorsIL] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.SeniorsIL,
            DisplayName = "Seniors Housing — Independent Living",
            MaxLtvPercent = 75m,
            MinDscr = 1.30m,
            MaxAmortizationYears = 30,
            MaxSnfNoiPercent = 20m,
            Notes = "Purpose-built; experienced sponsor + operator required"
        },
        [FreddieProductType.SeniorsAL] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.SeniorsAL,
            DisplayName = "Seniors Housing — Assisted Living",
            MaxLtvPercent = 75m,
            MinDscr = 1.45m,
            MaxAmortizationYears = 30,
            SeniorsAlDscr = 1.45m,
            MaxSnfNoiPercent = 20m,
            Notes = "AL component DSCR 1.45x; mgmt + regulatory compliance required"
        },
        [FreddieProductType.SeniorsSN] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.SeniorsSN,
            DisplayName = "Seniors Housing — Skilled Nursing",
            MaxLtvPercent = 75m,
            MinDscr = 1.50m,
            MaxAmortizationYears = 30,
            SeniorsSnDscr = 1.50m,
            MaxSnfNoiPercent = 20m,
            Notes = "SN component DSCR 1.50x; 20% SNF NOI cap for blended properties"
        },
        [FreddieProductType.StudentHousing] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.StudentHousing,
            DisplayName = "Student Housing",
            MaxLtvPercent = 80m,
            MinDscr = 1.30m,
            MaxAmortizationYears = 30,
            MinStudentPercent = 40m,
            DedicatedMinEnrollment = 10_000,
            Notes = "40%+ student occupancy; Dedicated = 80%+ student with 10K+ enrollment"
        },
        [FreddieProductType.ManufacturedHousing] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.ManufacturedHousing,
            DisplayName = "Manufactured Housing Communities",
            MaxLtvPercent = 80m,
            MinDscr = 1.25m,
            MaxAmortizationYears = 30,
            MinPadSites = 5,
            MaxRentalHomesPercent = 25m,
            Notes = "5+ pads; max 25% rental homes; pad-rent focused"
        },
        [FreddieProductType.FloatingRate] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.FloatingRate,
            DisplayName = "Floating Rate Loans",
            MaxLtvPercent = 80m,
            MinDscr = 1.25m,
            MaxAmortizationYears = 30,
            RequiresRateCap = true,
            RateCapLtvThreshold = 60m,
            VariableRateAvailable = true,
            Notes = "Rate cap required at LTV > 60%; SOFR-based"
        },
        [FreddieProductType.ValueAdd] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.ValueAdd,
            DisplayName = "Value-Add Loans",
            MaxLtvPercent = 85m,
            MinDscr = 1.15m,
            MaxAmortizationYears = 30,
            MinRehabPerUnit = 10_000m,
            MaxRehabPerUnit = 25_000m,
            RehabMinDscrIo = 1.10m,
            RehabMinDscrAmortizing = 1.15m,
            Notes = "3yr + ext; $10K–$25K/unit rehab; IO during rehab at 1.10x"
        },
        [FreddieProductType.ModerateRehab] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.ModerateRehab,
            DisplayName = "Moderate Rehabilitation Loans",
            MaxLtvPercent = 80m,
            MinDscr = 1.20m,
            MaxAmortizationYears = 30,
            MinRehabPerUnit = 25_000m,
            MaxRehabPerUnit = 60_000m,
            Notes = "$25K–$60K/unit rehab; stabilized DSCR at 1.20x"
        },
        [FreddieProductType.LeaseUp] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.LeaseUp,
            DisplayName = "Lease-Up Loans",
            MaxLtvPercent = 75m,
            MinDscr = 1.30m,
            MaxAmortizationYears = 30,
            LeaseUpMinOccupancy = 65m,
            LeaseUpMinLeased = 75m,
            Notes = "65% physical occupancy + 75% leased at closing"
        },
        [FreddieProductType.Supplemental] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.Supplemental,
            DisplayName = "Supplemental Mortgage Loans",
            MaxLtvPercent = 80m,
            MinDscr = 1.25m,
            MaxAmortizationYears = 30,
            MinLoanAmount = 1_000_000m,
            RequiresCombinedLoanTest = true,
            Notes = "$1M minimum; combined DSCR 1.25x + combined LTV 80% tested"
        },
        [FreddieProductType.TaxExemptLIHTC] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.TaxExemptLIHTC,
            DisplayName = "Tax-Exempt Bond / LIHTC",
            MaxLtvPercent = 90m,
            MinDscr = 1.15m,
            MaxAmortizationYears = 30,
            Notes = "90% LTV / 1.15x DSCR for tax-exempt bond credit enhanced loans"
        },
        [FreddieProductType.Section8] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.Section8,
            DisplayName = "Section 8 — Project-Based",
            MaxLtvPercent = 80m,
            MinDscr = 1.20m,
            MaxAmortizationYears = 30,
            Notes = "80% LTV / 1.20x DSCR standard; 90% / 1.15x for LIHTC overlay"
        },
        [FreddieProductType.NOAHPreservation] = new FreddieProductProfile
        {
            ProductType = FreddieProductType.NOAHPreservation,
            DisplayName = "NOAH Preservation",
            MaxLtvPercent = 80m,
            MinDscr = 1.20m,
            MaxAmortizationYears = 30,
            MaxTermYears = 15,
            NonprofitRequired = true,
            Notes = "15yr max term; nonprofit borrower only; naturally occurring affordable housing"
        }
    };

    public static FreddieProductProfile Get(FreddieProductType type) =>
        _profiles.TryGetValue(type, out var profile)
            ? profile
            : throw new ArgumentOutOfRangeException(nameof(type), $"No profile defined for {type}");

    public static FreddieProductProfile? TryGet(FreddieProductType? type) =>
        type.HasValue && _profiles.TryGetValue(type.Value, out var profile) ? profile : null;

    public static IReadOnlyDictionary<FreddieProductType, FreddieProductProfile> All => _profiles;

    /// <summary>
    /// Suggests a default FreddieProductType based on PropertyType.
    /// </summary>
    public static FreddieProductType SuggestFromPropertyType(PropertyType propertyType) => propertyType switch
    {
        PropertyType.Multifamily => FreddieProductType.Conventional,
        PropertyType.AssistedLiving => FreddieProductType.SeniorsAL,
        PropertyType.SkilledNursing => FreddieProductType.SeniorsSN,
        PropertyType.MemoryCare => FreddieProductType.SeniorsAL,
        PropertyType.CCRC => FreddieProductType.SeniorsIL,
        PropertyType.IndependentLiving => FreddieProductType.SeniorsIL,
        _ => FreddieProductType.Conventional
    };

    /// <summary>
    /// Calculates the blended minimum DSCR for a Freddie Mac seniors housing
    /// property based on the bed mix of IL, AL, and SN beds.
    /// Freddie thresholds: IL=1.30, AL=1.45, SN=1.50.
    /// </summary>
    public static decimal CalculateSeniorsBlendedMinDscr(int ilBeds, int alBeds, int snBeds)
    {
        int totalBeds = ilBeds + alBeds + snBeds;
        if (totalBeds <= 0) return 1.30m;

        decimal ilWeight = (decimal)ilBeds / totalBeds;
        decimal alWeight = (decimal)alBeds / totalBeds;
        decimal snWeight = (decimal)snBeds / totalBeds;

        const decimal ilDscr = 1.30m;
        const decimal alDscr = 1.45m;
        const decimal snDscr = 1.50m;

        return Math.Round(ilWeight * ilDscr + alWeight * alDscr + snWeight * snDscr, 2);
    }
}
