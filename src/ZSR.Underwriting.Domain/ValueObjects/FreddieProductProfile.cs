using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.ValueObjects;

/// <summary>
/// Immutable underwriting profile for a Freddie Mac product type,
/// sourced from official term sheets at mf.freddiemac.com.
/// </summary>
public sealed record FreddieProductProfile
{
    public FreddieProductType ProductType { get; init; }
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

    // Term range
    public int MinTermYears { get; init; } = 5;
    public int MaxTermYears { get; init; } = 30;

    // Rate structure
    public bool FixedRateAvailable { get; init; } = true;
    public bool VariableRateAvailable { get; init; } = true;

    // SBL market tier (Small Balance Loan)
    public string? SblMarketTier { get; init; }
    public decimal? SblTierMaxLtv { get; init; }
    public decimal? SblTierMinDscr { get; init; }

    // Seniors Housing: secondary DSCR thresholds for blending
    public decimal? SeniorsAlDscr { get; init; }
    public decimal? SeniorsSnDscr { get; init; }

    // SNF NOI cap
    public decimal? MaxSnfNoiPercent { get; init; }

    // Manufactured Housing
    public int? MinPadSites { get; init; }
    public decimal? MaxRentalHomesPercent { get; init; }

    // Floating Rate
    public bool RequiresRateCap { get; init; }
    public decimal? RateCapLtvThreshold { get; init; }

    // Value-Add / Moderate Rehab
    public decimal? MinRehabPerUnit { get; init; }
    public decimal? MaxRehabPerUnit { get; init; }
    public decimal? RehabMinDscrIo { get; init; }
    public decimal? RehabMinDscrAmortizing { get; init; }

    // Lease-Up
    public decimal? LeaseUpMinOccupancy { get; init; }
    public decimal? LeaseUpMinLeased { get; init; }

    // Supplemental: combined test
    public bool RequiresCombinedLoanTest { get; init; }

    // Nonprofit required (NOAH Preservation)
    public bool NonprofitRequired { get; init; }

    // Student Housing
    public decimal? MinStudentPercent { get; init; }
    public int? DedicatedMinEnrollment { get; init; }

    // Assumability
    public bool IsAssumable { get; init; } = true;

    // Additional notes for display
    public string? Notes { get; init; }
}
