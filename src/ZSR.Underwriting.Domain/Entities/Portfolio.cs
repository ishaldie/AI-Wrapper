namespace ZSR.Underwriting.Domain.Entities;

public class Portfolio
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Strategy { get; set; }        // "Value-Add", "Core", "Core-Plus", "Opportunistic"
    public int? VintageYear { get; set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Deal> Deals { get; set; } = new List<Deal>();

    // EF Core parameterless constructor
    private Portfolio() { }

    public Portfolio(string name, string userId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Portfolio name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        Id = Guid.NewGuid();
        Name = name;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }
}
