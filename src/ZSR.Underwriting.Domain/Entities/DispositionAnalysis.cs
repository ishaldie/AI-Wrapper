namespace ZSR.Underwriting.Domain.Entities;

public class DispositionAnalysis
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    // Current valuation
    public decimal? BrokerOpinionOfValue { get; set; }
    public decimal? CurrentMarketCapRate { get; set; }
    public decimal TrailingTwelveMonthNoi { get; set; }
    public decimal ImpliedValue { get; set; }

    // Scenario outputs
    public string? HoldScenarioJson { get; set; }
    public string? SellScenarioJson { get; set; }
    public string? RefinanceScenarioJson { get; set; }

    // AI-generated recommendation
    public string? Recommendation { get; set; }

    public DateTime AnalyzedAt { get; set; }

    // EF Core parameterless constructor
    private DispositionAnalysis() { }

    public DispositionAnalysis(Guid dealId)
    {
        Id = Guid.NewGuid();
        DealId = dealId;
        AnalyzedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculate implied value from T12 NOI and market cap rate.
    /// </summary>
    public void RecalculateImpliedValue()
    {
        ImpliedValue = CurrentMarketCapRate.HasValue && CurrentMarketCapRate.Value > 0
            ? TrailingTwelveMonthNoi / (CurrentMarketCapRate.Value / 100m)
            : 0;
    }
}
