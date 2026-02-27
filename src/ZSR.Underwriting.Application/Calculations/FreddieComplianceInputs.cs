using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Calculations;

/// <summary>
/// Additional inputs needed for Freddie Mac product-specific compliance tests.
/// Populated from Deal entity fields; null when ExecutionType != FreddieMac.
/// </summary>
public class FreddieComplianceInputs
{
    public FreddieProductType ProductType { get; set; }

    // SBL — market tier for tiered LTV/DSCR
    public string? SblMarketTier { get; set; }

    // Seniors Housing — bed counts for blended DSCR
    public int IlBeds { get; set; }
    public int AlBeds { get; set; }
    public int SnBeds { get; set; }

    // SNF — skilled nursing NOI portion
    public decimal? SnfNoi { get; set; }
    public decimal? SnfNoiPercent { get; set; }

    // MHC — rental homes percentage
    public decimal? RentalHomesPercent { get; set; }

    // Floating Rate — whether a rate cap is in place
    public bool HasRateCap { get; set; }

    // Value-Add / Moderate Rehab — rehab period indicators
    public bool IsRehabPeriod { get; set; }

    // Lease-Up — occupancy and leased percentages
    public decimal? PhysicalOccupancyPercent { get; set; }
    public decimal? LeasedPercent { get; set; }

    // Supplemental — senior loan parameters for combined test
    public decimal? SeniorLoanAmount { get; set; }
    public decimal? SeniorDebtService { get; set; }

    // Risk assessment fields
    public int? NearbyEnrollment { get; set; }
}
