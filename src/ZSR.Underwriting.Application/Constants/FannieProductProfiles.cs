using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Application.Constants;

/// <summary>
/// Static registry of Fannie Mae product profiles sourced from official
/// term sheets at multifamily.fanniemae.com (reviewed 2026-02-26).
/// </summary>
public static class FannieProductProfiles
{
    private static readonly Dictionary<FannieProductType, FannieProductProfile> _profiles = new()
    {
        [FannieProductType.Conventional] = new FannieProductProfile
        {
            ProductType = FannieProductType.Conventional,
            DisplayName = "Conventional Properties",
            MaxLtvPercent = 80m,
            MinDscr = 1.25m,
            MaxAmortizationYears = 30,
            MinOccupancyPercent = 90m,
            Notes = "Stabilized occupancy (90%) for 90 days prior to funding"
        },
        [FannieProductType.SmallLoan] = new FannieProductProfile
        {
            ProductType = FannieProductType.SmallLoan,
            DisplayName = "Small Mortgage Loan",
            MaxLtvPercent = 80m,
            MinDscr = 1.25m,
            MaxAmortizationYears = 30,
            MaxLoanAmount = 9_000_000m,
            Notes = "Streamlined ESA (ASTM E-1528-14); eligible for Conventional, MAH, MHC"
        },
        [FannieProductType.AffordableHousing] = new FannieProductProfile
        {
            ProductType = FannieProductType.AffordableHousing,
            DisplayName = "Affordable Housing Preservation",
            MaxLtvPercent = 80m,
            MinDscr = 1.20m,
            MaxAmortizationYears = 35,
            Notes = "20%+ units ≤50% AMI, or 40%+ units ≤60% AMI, or 20%+ units Section 8 HAP"
        },
        [FannieProductType.SeniorsIL] = new FannieProductProfile
        {
            ProductType = FannieProductType.SeniorsIL,
            DisplayName = "Seniors Housing — Independent Living",
            MaxLtvPercent = 75m,
            MinDscr = 1.30m,
            MaxAmortizationYears = 30,
            MaxSnfNcfPercent = 20m,
            Notes = "Purpose-built; experienced sponsor + operator required; 80% LTV for fixed-rate tax-exempt bonds"
        },
        [FannieProductType.SeniorsAL] = new FannieProductProfile
        {
            ProductType = FannieProductType.SeniorsAL,
            DisplayName = "Seniors Housing — Assisted Living",
            MaxLtvPercent = 75m,
            MinDscr = 1.40m,
            MaxAmortizationYears = 30,
            SeniorsAlDscr = 1.40m,
            MaxSnfNcfPercent = 20m,
            Notes = "Mgmt + operations + regulatory compliance reports required"
        },
        [FannieProductType.SeniorsALZ] = new FannieProductProfile
        {
            ProductType = FannieProductType.SeniorsALZ,
            DisplayName = "Seniors Housing — Alzheimer's/Dementia Care",
            MaxLtvPercent = 75m,
            MinDscr = 1.45m,
            MaxAmortizationYears = 30,
            SeniorsAlzDscr = 1.45m,
            MaxSnfNcfPercent = 20m,
            Notes = "Stand-alone ALZ; highest DSCR requirement"
        },
        [FannieProductType.StudentHousing] = new FannieProductProfile
        {
            ProductType = FannieProductType.StudentHousing,
            DisplayName = "Student Housing",
            MaxLtvPercent = 75m,
            MinDscr = 1.30m,
            MaxAmortizationYears = 30,
            MinStudentPercent = 40m,
            DedicatedMinEnrollment = 10_000,
            Notes = "1.30x fixed / 1.05x variable (subject to Fixed Rate Test); Dedicated = 80%+ student"
        },
        [FannieProductType.ManufacturedHousing] = new FannieProductProfile
        {
            ProductType = FannieProductType.ManufacturedHousing,
            DisplayName = "Manufactured Housing Communities",
            MaxLtvPercent = 80m,
            MinDscr = 1.25m,
            MaxAmortizationYears = 30,
            MinVacancyPercent = 5m,
            MinPadSites = 50,
            MaxTenantOccupiedPercent = 35m,
            Notes = "Quality Level 3-5; ≤12 homes/acre existing, ≤7 new; replacement reserves typically NOT required"
        },
        [FannieProductType.Cooperative] = new FannieProductProfile
        {
            ProductType = FannieProductType.Cooperative,
            DisplayName = "Cooperative Properties",
            MaxLtvPercent = 55m,
            MinDscr = 1.00m, // Actual operations minimum
            MaxAmortizationYears = 30,
            CoopActualDscr = 1.00m,
            CoopMarketRentalDscr = 1.55m,
            FixedRateAvailable = true,
            VariableRateAvailable = false,
            IsAssumable = false,
            Notes = "Dual DSCR: 1.00x actual ops + 1.55x market rental; fixed-rate only; NOT assumable; 10% operating reserve required"
        },
        [FannieProductType.SARM] = new FannieProductProfile
        {
            ProductType = FannieProductType.SARM,
            DisplayName = "Structured Adjustable Rate Mortgage (SARM)",
            MaxLtvPercent = 65m, // 70% for MAH
            MinDscr = 1.05m, // At maximum note rate
            MaxAmortizationYears = 30,
            MinLoanAmount = 25_000_000m,
            RequiresRateCapStressTest = true,
            MinTermYears = 5,
            MaxTermYears = 10,
            Notes = "65% LTV conventional / 70% MAH; 1.05x DSCR at max note rate; 30-day SOFR; borrower must purchase rate cap"
        },
        [FannieProductType.GreenRewards] = new FannieProductProfile
        {
            ProductType = FannieProductType.GreenRewards,
            DisplayName = "Green Rewards",
            MaxLtvPercent = 80m, // Base product LTV; up to +5% additional proceeds
            MinDscr = 1.25m, // Varies by base asset class
            MaxAmortizationYears = 30,
            GreenOwnerSavingsPercent = 75m,
            GreenTenantSavingsPercent = 25m,
            GreenMaxAdditionalProceedsPercent = 5m,
            Notes = "75% owner + 25% tenant projected savings in NCF; ≥30% combined energy/water reduction (≥15% energy); HPB Report paid by Fannie Mae; 125% improvement escrow"
        },
        [FannieProductType.Supplemental] = new FannieProductProfile
        {
            ProductType = FannieProductType.Supplemental,
            DisplayName = "Supplemental Mortgage Loans",
            MaxLtvPercent = 70m,
            MinDscr = 1.30m,
            MaxAmortizationYears = 30,
            RequiresCombinedLoanTest = true,
            Notes = "Available 12 months after senior Fannie Mae loan closing; combined DSCR + LTV tested"
        },
        [FannieProductType.NearStabilization] = new FannieProductProfile
        {
            ProductType = FannieProductType.NearStabilization,
            DisplayName = "Near-Stabilization Execution",
            MaxLtvPercent = 75m,
            MinDscr = 1.25m, // 1.15x for MAH
            MaxAmortizationYears = 30,
            MinLoanAmount = 10_000_000m,
            MinOccupancyPercent = 75m,
            Notes = "Tier 2: 75% LTV, 1.25x DSCR (1.15x MAH); 75% physical occupancy at rate lock; 12-month IO; Strong/Nationwide markets"
        },
        [FannieProductType.ROAR] = new FannieProductProfile
        {
            ProductType = FannieProductType.ROAR,
            DisplayName = "Reduced Occupancy Affordable Rehab (ROAR)",
            MaxLtvPercent = 90m,
            MinDscr = 1.15m, // As stabilized
            MaxAmortizationYears = 35,
            MinLoanAmount = 5_000_000m,
            RoarRehabMinOccupancy = 50m,
            RoarRehabMinDscrIo = 1.00m,
            RoarRehabMinDscrAmortizing = 0.75m,
            RoarMaxPerUnitRehab = 120_000m,
            Notes = "MAH only; 1.15–1.20x stabilized DSCR; 50% min occ during rehab; $120K/unit max; 12–15 month rehab; credit enhancement execution only"
        }
    };

    public static FannieProductProfile Get(FannieProductType type) =>
        _profiles.TryGetValue(type, out var profile)
            ? profile
            : throw new ArgumentOutOfRangeException(nameof(type), $"No profile defined for {type}");

    public static FannieProductProfile? TryGet(FannieProductType? type) =>
        type.HasValue && _profiles.TryGetValue(type.Value, out var profile) ? profile : null;

    public static IReadOnlyDictionary<FannieProductType, FannieProductProfile> All => _profiles;

    /// <summary>
    /// Suggests a default FannieProductType based on PropertyType.
    /// </summary>
    public static FannieProductType SuggestFromPropertyType(PropertyType propertyType) => propertyType switch
    {
        PropertyType.Multifamily => FannieProductType.Conventional,
        PropertyType.AssistedLiving => FannieProductType.SeniorsAL,
        PropertyType.SkilledNursing => FannieProductType.SeniorsIL, // SNF alone not eligible; must be combo
        PropertyType.MemoryCare => FannieProductType.SeniorsALZ,
        PropertyType.CCRC => FannieProductType.SeniorsIL,
        _ => FannieProductType.Conventional
    };

    /// <summary>
    /// Calculates the blended minimum DSCR for a seniors housing property
    /// based on the bed mix of IL, AL, and ALZ beds.
    /// </summary>
    public static decimal CalculateSeniorsBlendedMinDscr(int ilBeds, int alBeds, int alzBeds)
    {
        int totalBeds = ilBeds + alBeds + alzBeds;
        if (totalBeds <= 0) return 1.30m; // Default to IL if no beds specified

        decimal ilWeight = (decimal)ilBeds / totalBeds;
        decimal alWeight = (decimal)alBeds / totalBeds;
        decimal alzWeight = (decimal)alzBeds / totalBeds;

        const decimal ilDscr = 1.30m;
        const decimal alDscr = 1.40m;
        const decimal alzDscr = 1.45m;

        return Math.Round(ilWeight * ilDscr + alWeight * alDscr + alzWeight * alzDscr, 2);
    }
}
