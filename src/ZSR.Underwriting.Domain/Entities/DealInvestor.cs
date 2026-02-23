namespace ZSR.Underwriting.Domain.Entities;

public class DealInvestor
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public string Name { get; set; }
    public string? Company { get; set; }
    public string? Role { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal? NetWorth { get; set; }
    public decimal? Liquidity { get; set; }
    public string? Notes { get; set; }

    private DealInvestor() { Name = string.Empty; }

    public DealInvestor(Guid dealId, string name)
    {
        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId cannot be empty.", nameof(dealId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Id = Guid.NewGuid();
        DealId = dealId;
        Name = name;
    }
}
