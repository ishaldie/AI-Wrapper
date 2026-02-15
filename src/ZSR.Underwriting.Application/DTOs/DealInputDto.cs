namespace ZSR.Underwriting.Application.DTOs;

public class DealInputDto
{
    // Step 1: Required Inputs
    public string PropertyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int? UnitCount { get; set; }
    public decimal? PurchasePrice { get; set; }

    // Step 2: Preferred Inputs
    public decimal? RentRollSummary { get; set; }
    public decimal? T12Summary { get; set; }
    public decimal? LoanLtv { get; set; }
    public decimal? LoanRate { get; set; }
    public bool IsInterestOnly { get; set; }
    public int? AmortizationYears { get; set; }
    public int? LoanTermYears { get; set; }

    // Step 3: Optional Inputs (with protocol defaults)
    public int? HoldPeriodYears { get; set; }
    public decimal? CapexBudget { get; set; }
    public decimal? TargetOccupancy { get; set; }
    public string ValueAddPlans { get; set; } = string.Empty;
}
