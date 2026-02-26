namespace ZSR.Underwriting.Application.DTOs;

public class PortfolioSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Strategy { get; set; }
    public int? VintageYear { get; set; }
    public int DealCount { get; set; }
    public int ActiveAssetCount { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalAum { get; set; }
    public decimal? WeightedAvgCapRate { get; set; }
    public decimal? AggregateNoi { get; set; }
}
