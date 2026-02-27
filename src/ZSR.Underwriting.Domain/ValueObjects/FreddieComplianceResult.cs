namespace ZSR.Underwriting.Domain.ValueObjects;

/// <summary>
/// Aggregated Freddie Mac compliance test results, stored as JSON on CalculationResult.
/// </summary>
public sealed record FreddieComplianceResult
{
    public bool OverallPass { get; init; }
    public decimal ProductMinDscr { get; init; }
    public decimal ProductMaxLtvPercent { get; init; }
    public int ProductMaxAmortYears { get; init; }

    // Core compliance tests (always present)
    public ComplianceTest DscrTest { get; init; } = null!;
    public ComplianceTest LtvTest { get; init; } = null!;
    public ComplianceTest AmortizationTest { get; init; } = null!;

    // Product-specific tests (null when not applicable)
    public ComplianceTest? SblMarketTierTest { get; init; }
    public ComplianceTest? SeniorsBlendedDscrTest { get; init; }
    public ComplianceTest? SnfNoiCapTest { get; init; }
    public ComplianceTest? MhcRentalHomesCapTest { get; init; }
    public ComplianceTest? FloatingRateCapTest { get; init; }
    public ComplianceTest? ValueAddRehabDscrTest { get; init; }
    public ComplianceTest? LeaseUpOccupancyTest { get; init; }
    public ComplianceTest? LeaseUpLeasedTest { get; init; }
    public ComplianceTest? SupplementalCombinedDscrTest { get; init; }
    public ComplianceTest? SupplementalCombinedLtvTest { get; init; }
}
