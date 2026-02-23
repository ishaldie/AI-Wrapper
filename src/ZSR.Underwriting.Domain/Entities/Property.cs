namespace ZSR.Underwriting.Domain.Entities;

public class Property
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public string Address { get; set; }
    public int UnitCount { get; set; }
    public int? YearBuilt { get; set; }
    public string? BuildingType { get; set; }
    public decimal? Acreage { get; set; }
    public int? SquareFootage { get; set; }

    // EF Core parameterless constructor
    private Property() { Address = string.Empty; }

    public Property(string address, int unitCount)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty.", nameof(address));
        if (unitCount <= 0)
            throw new ArgumentException("Unit count must be greater than 0.", nameof(unitCount));

        Id = Guid.NewGuid();
        Address = address;
        UnitCount = unitCount;
    }
}
