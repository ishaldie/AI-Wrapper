namespace ZSR.Underwriting.Application.DTOs;

public class MarketContextDto
{
    public List<MarketDataItem> MajorEmployers { get; set; } = new();
    public List<MarketDataItem> ConstructionPipeline { get; set; } = new();
    public List<MarketDataItem> EconomicDrivers { get; set; } = new();
    public List<MarketDataItem> InfrastructureProjects { get; set; } = new();
    public List<MarketDataItem> ComparableTransactions { get; set; } = new();
    public decimal? CurrentFannieMaeRate { get; set; }
    public Dictionary<string, List<string>> SourceUrls { get; set; } = new();
    public DateTime RetrievedAt { get; set; }
}

public class MarketDataItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}
