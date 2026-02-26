using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class RentRollUnit
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public int? SquareFeet { get; set; }
    public decimal MarketRent { get; set; }
    public decimal? ActualRent { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Vacant;

    // Tenant info (if occupied)
    public string? TenantName { get; set; }
    public DateTime? LeaseStart { get; set; }
    public DateTime? LeaseEnd { get; set; }
    public decimal? SecurityDeposit { get; set; }
    public decimal? MonthlyCharges { get; set; }

    public DateTime UpdatedAt { get; set; }

    // EF Core parameterless constructor
    private RentRollUnit() { }

    public RentRollUnit(Guid dealId, string unitNumber, decimal marketRent)
    {
        if (string.IsNullOrWhiteSpace(unitNumber))
            throw new ArgumentException("Unit number cannot be empty.", nameof(unitNumber));

        Id = Guid.NewGuid();
        DealId = dealId;
        UnitNumber = unitNumber;
        MarketRent = marketRent;
        UpdatedAt = DateTime.UtcNow;
    }
}
