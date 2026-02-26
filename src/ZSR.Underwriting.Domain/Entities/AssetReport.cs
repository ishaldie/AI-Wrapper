namespace ZSR.Underwriting.Domain.Entities;

public class AssetReport
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public AssetReportType Type { get; set; }
    public int Year { get; set; }
    public int? Month { get; set; }
    public int? Quarter { get; set; }

    // AI-generated narratives
    public string? PerformanceSummary { get; set; }
    public string? VarianceAnalysis { get; set; }
    public string? MarketUpdate { get; set; }
    public string? OutlookAndRecommendations { get; set; }

    // Snapshot metrics at time of report
    public string? MetricsSnapshotJson { get; set; }

    public DateTime GeneratedAt { get; set; }

    // EF Core parameterless constructor
    private AssetReport() { }

    public AssetReport(Guid dealId, AssetReportType type, int year)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year));

        Id = Guid.NewGuid();
        DealId = dealId;
        Type = type;
        Year = year;
        GeneratedAt = DateTime.UtcNow;
    }

    /// <summary>Period label for display (e.g., "Jan 2025", "Q1 2025", "2025").</summary>
    public string PeriodLabel => Type switch
    {
        AssetReportType.Monthly when Month.HasValue =>
            new DateTime(Year, Month.Value, 1).ToString("MMM yyyy"),
        AssetReportType.Quarterly when Quarter.HasValue =>
            $"Q{Quarter} {Year}",
        _ => Year.ToString()
    };
}

public enum AssetReportType
{
    Monthly,
    Quarterly,
    Annual
}
