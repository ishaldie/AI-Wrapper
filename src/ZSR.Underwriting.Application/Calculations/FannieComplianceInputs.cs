using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Calculations;

/// <summary>
/// Additional inputs needed for Fannie Mae product-specific compliance tests.
/// Populated from Deal entity fields; null when ExecutionType != FannieMae.
/// </summary>
public class FannieComplianceInputs
{
    public FannieProductType ProductType { get; set; }

    // Seniors Housing — bed counts for blended DSCR
    public int IlBeds { get; set; }
    public int AlBeds { get; set; }
    public int AlzBeds { get; set; }

    // Cooperative — market rental NOI for dual DSCR test
    public decimal? MarketRentalNoi { get; set; }

    // SARM — rate cap parameters for stress test
    public decimal? SarmMarginPercent { get; set; }
    public decimal? SarmCapStrikePercent { get; set; }

    // Green Rewards — projected savings for NCF adjustment
    public decimal? OwnerProjectedSavings { get; set; }
    public decimal? TenantProjectedSavings { get; set; }

    // SNF — skilled nursing facility NCF portion
    public decimal? SnfNcf { get; set; }
    public decimal? SnfNcfPercent { get; set; }

    // ROAR — rehab period indicators
    public bool IsRehabPeriod { get; set; }
    public decimal? StabilizedNoi { get; set; }

    // Supplemental — senior loan parameters for combined test
    public decimal? SeniorLoanAmount { get; set; }
    public decimal? SeniorDebtService { get; set; }

    // Risk assessment fields (Phase 3)
    // Student Housing — nearby university enrollment
    public int? NearbyEnrollment { get; set; }

    // MHC — tenant-occupied homes percentage
    public decimal? TenantOccupiedPercent { get; set; }

    // Cooperative — single sponsor ownership concentration
    public decimal? SponsorOwnershipPercent { get; set; }

    // Affordable Housing — subordinate debt combined DSCR
    public decimal? SubDebtCombinedDscr { get; set; }
}
