using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class CapitalStackItem
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public CapitalSource Source { get; set; }
    public decimal Amount { get; set; }
    public decimal? Rate { get; set; }
    public int? TermYears { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }

    private CapitalStackItem() { }

    public CapitalStackItem(Guid dealId, CapitalSource source, decimal amount)
    {
        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId cannot be empty.", nameof(dealId));

        Id = Guid.NewGuid();
        DealId = dealId;
        Source = source;
        Amount = amount;
    }
}
