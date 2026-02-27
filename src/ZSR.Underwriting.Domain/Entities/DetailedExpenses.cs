namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// Line-item expense breakdown for detailed underwriting.
/// All values are annual totals. When provided, these override ratio-based OpEx.
/// </summary>
public class DetailedExpenses
{
    public decimal? RealEstateTaxes { get; set; }
    public decimal? Insurance { get; set; }
    public decimal? Utilities { get; set; }
    public decimal? RepairsAndMaintenance { get; set; }
    public decimal? Payroll { get; set; }
    public decimal? Marketing { get; set; }
    public decimal? GeneralAndAdmin { get; set; }
    public decimal? ManagementFee { get; set; }
    public decimal? ReplacementReserves { get; set; }
    public decimal? OtherExpenses { get; set; }

    /// <summary>
    /// Management fee expressed as a percentage of EGI (0-100).
    /// When set, ManagementFee dollar amount is calculated from EGI × this rate.
    /// </summary>
    public decimal? ManagementFeePct { get; set; }

    /// <summary>
    /// Returns true if at least one expense line item has been provided.
    /// </summary>
    public bool HasAnyValues =>
        RealEstateTaxes.HasValue || Insurance.HasValue || Utilities.HasValue ||
        RepairsAndMaintenance.HasValue || Payroll.HasValue || Marketing.HasValue ||
        GeneralAndAdmin.HasValue || ManagementFee.HasValue || ManagementFeePct.HasValue ||
        ReplacementReserves.HasValue || OtherExpenses.HasValue;

    /// <summary>
    /// Sum of all provided line items.
    /// </summary>
    public decimal Total =>
        (RealEstateTaxes ?? 0) + (Insurance ?? 0) + (Utilities ?? 0) +
        (RepairsAndMaintenance ?? 0) + (Payroll ?? 0) + (Marketing ?? 0) +
        (GeneralAndAdmin ?? 0) + (ManagementFee ?? 0) +
        (ReplacementReserves ?? 0) + (OtherExpenses ?? 0);
}
