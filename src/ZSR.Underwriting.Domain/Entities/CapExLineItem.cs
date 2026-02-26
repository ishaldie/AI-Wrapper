namespace ZSR.Underwriting.Domain.Entities;

public class CapExLineItem
{
    public Guid Id { get; private set; }
    public Guid CapExProjectId { get; set; }
    public CapExProject Project { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public string? Vendor { get; set; }
    public decimal Amount { get; set; }
    public DateTime DateIncurred { get; set; }

    // EF Core parameterless constructor
    private CapExLineItem() { }

    public CapExLineItem(Guid capExProjectId, string description, decimal amount, DateTime dateIncurred)
    {
        Id = Guid.NewGuid();
        CapExProjectId = capExProjectId;
        Description = description;
        Amount = amount;
        DateIncurred = dateIncurred;
    }
}
