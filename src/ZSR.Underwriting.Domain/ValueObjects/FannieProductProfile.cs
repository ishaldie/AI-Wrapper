using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.ValueObjects;

/// <summary>
/// Immutable underwriting profile for a Fannie Mae product type,
/// sourced from official term sheets at multifamily.fanniemae.com.
/// </summary>
public sealed record FannieProductProfile
{
    public FannieProductType ProductType { get; init; }
    public string DisplayName { get; init; } = string.Empty;

    // Leverage constraints
    public decimal MaxLtvPercent { get; init; }
    public decimal MinDscr { get; init; }
    public int MaxAmortizationYears { get; init; }

    // Loan sizing
    public decimal? MinLoanAmount { get; init; }
    public decimal? MaxLoanAmount { get; init; }

    // Occupancy
    public decimal? MinOccupancyPercent { get; init; }
    public decimal? MinVacancyPercent { get; init; }

    // Term range
    public int MinTermYears { get; init; } = 5;
    public int MaxTermYears { get; init; } = 30;

    // Rate structure
    public bool FixedRateAvailable { get; init; } = true;
    public bool VariableRateAvailable { get; init; } = true;

    // Seniors Housing: secondary DSCR thresholds for blending
    public decimal? SeniorsAlDscr { get; init; }
    public decimal? SeniorsAlzDscr { get; init; }

    // Cooperative: dual DSCR
    public decimal? CoopActualDscr { get; init; }
    public decimal? CoopMarketRentalDscr { get; init; }

    // SARM: stress test at max rate
    public bool RequiresRateCapStressTest { get; init; }

    // Green: NCF adjustment
    public decimal? GreenOwnerSavingsPercent { get; init; }
    public decimal? GreenTenantSavingsPercent { get; init; }
    public decimal? GreenMaxAdditionalProceedsPercent { get; init; }

    // ROAR: rehab period
    public decimal? RoarRehabMinOccupancy { get; init; }
    public decimal? RoarRehabMinDscrIo { get; init; }
    public decimal? RoarRehabMinDscrAmortizing { get; init; }
    public decimal? RoarMaxPerUnitRehab { get; init; }

    // Supplemental: combined test
    public bool RequiresCombinedLoanTest { get; init; }

    // Skilled Nursing cap
    public decimal? MaxSnfNcfPercent { get; init; }

    // MHC
    public int? MinPadSites { get; init; }
    public decimal? MaxTenantOccupiedPercent { get; init; }

    // Student Housing
    public decimal? MinStudentPercent { get; init; }
    public int? DedicatedMinEnrollment { get; init; }

    // Assumability
    public bool IsAssumable { get; init; } = true;

    // Additional notes for display
    public string? Notes { get; init; }
}
