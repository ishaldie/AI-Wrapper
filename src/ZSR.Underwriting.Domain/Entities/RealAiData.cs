namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// Cached data from RealAI API responses for a deal.
/// </summary>
public class RealAiData
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
    public DateTime FetchedAt { get; private set; }

    // Property data
    public decimal? InPlaceRent { get; set; }
    public decimal? Occupancy { get; set; }
    public int? YearBuilt { get; set; }
    public string? BuildingType { get; set; }
    public decimal? Acreage { get; set; }
    public int? SquareFootage { get; set; }
    public string? Amenities { get; set; }

    // Tenant metrics
    public int? AverageFico { get; set; }
    public decimal? RentToIncomeRatio { get; set; }
    public decimal? MedianHhi { get; set; }

    // Market data
    public decimal? MarketCapRate { get; set; }
    public decimal? RentGrowth { get; set; }
    public decimal? JobGrowth { get; set; }
    public int? NetMigration { get; set; }
    public int? Permits { get; set; }

    // Sales comps (stored as JSON)
    public string? SalesCompsJson { get; set; }

    // Time series (stored as JSON)
    public string? RentTrendJson { get; set; }
    public string? OccupancyTrendJson { get; set; }

    // EF Core parameterless constructor
    private RealAiData() { }

    public RealAiData(Guid dealId)
    {
        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId cannot be empty.", nameof(dealId));

        Id = Guid.NewGuid();
        DealId = dealId;
        FetchedAt = DateTime.UtcNow;
    }
}
