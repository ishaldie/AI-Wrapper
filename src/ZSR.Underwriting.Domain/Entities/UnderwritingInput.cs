namespace ZSR.Underwriting.Domain.Entities;

public class UnderwritingInput
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    // Required
    public decimal PurchasePrice { get; private set; }

    // Loan fields (preferred inputs)
    public decimal? LoanLtv { get; set; }
    public decimal? LoanRate { get; set; }
    public bool IsInterestOnly { get; set; }
    public int? AmortizationYears { get; set; }
    public int? LoanTermYears { get; set; }

    // Financial summaries (preferred inputs)
    public decimal? RentRollSummary { get; set; }
    public decimal? T12Summary { get; set; }

    // Optional inputs
    public int? HoldPeriodYears { get; set; }
    public decimal? CapexBudget { get; set; }
    public decimal? TargetOccupancy { get; set; }
    public string? ValueAddPlans { get; set; }

    // EF Core parameterless constructor
    private UnderwritingInput() { }

    public UnderwritingInput(decimal purchasePrice)
    {
        if (purchasePrice <= 0)
            throw new ArgumentException("Purchase price must be greater than 0.", nameof(purchasePrice));

        Id = Guid.NewGuid();
        PurchasePrice = purchasePrice;
    }
}
