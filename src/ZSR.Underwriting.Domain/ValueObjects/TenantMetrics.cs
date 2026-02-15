namespace ZSR.Underwriting.Domain.ValueObjects;

public class TenantMetrics
{
    public MetricLevel Subject { get; set; } = new();
    public MetricLevel Zipcode { get; set; } = new();
    public MetricLevel Metro { get; set; } = new();
}

public class MetricLevel
{
    public int? AverageFico { get; set; }
    public decimal? RentToIncomeRatio { get; set; }
    public decimal? MedianHhi { get; set; }
}
