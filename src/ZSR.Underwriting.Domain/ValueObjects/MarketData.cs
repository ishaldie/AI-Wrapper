namespace ZSR.Underwriting.Domain.ValueObjects;

public class MarketData
{
    public decimal? CapRate { get; set; }
    public decimal? RentGrowth { get; set; }
    public decimal? JobGrowth { get; set; }
    public int? NetMigration { get; set; }
    public int? Permits { get; set; }
}
