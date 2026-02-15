namespace ZSR.Underwriting.Domain.ValueObjects;

public class TimeSeriesData
{
    public List<DataPoint> RentTrend { get; set; } = new();
    public List<DataPoint> OccupancyTrend { get; set; } = new();
}

public class DataPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}
