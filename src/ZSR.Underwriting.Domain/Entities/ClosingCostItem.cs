namespace ZSR.Underwriting.Domain.Entities;

public class ClosingCostItem
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public string Category { get; set; } = string.Empty;  // "Title", "Legal", "Lender", "Survey", "Insurance", "Escrow", "Other"
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public bool IsPaid { get; set; }

    // EF Core parameterless constructor
    private ClosingCostItem() { }

    public ClosingCostItem(Guid dealId, string category, string description, decimal estimatedAmount)
    {
        Id = Guid.NewGuid();
        DealId = dealId;
        Category = category;
        Description = description;
        EstimatedAmount = estimatedAmount;
    }
}
