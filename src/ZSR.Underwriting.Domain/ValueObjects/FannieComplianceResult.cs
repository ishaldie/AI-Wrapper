namespace ZSR.Underwriting.Domain.ValueObjects;

/// <summary>
/// Aggregated Fannie Mae compliance test results, stored as JSON on CalculationResult.
/// </summary>
public sealed record FannieComplianceResult
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
    public ComplianceTest? SeniorsBlendedDscrTest { get; init; }
    public ComplianceTest? CoopActualDscrTest { get; init; }
    public ComplianceTest? CoopMarketRentalDscrTest { get; init; }
    public ComplianceTest? SarmStressDscrTest { get; init; }
    public ComplianceTest? SnfNcfCapTest { get; init; }
    public ComplianceTest? MhcVacancyFloorTest { get; init; }
    public ComplianceTest? RoarRehabDscrTest { get; init; }
    public ComplianceTest? SupplementalCombinedDscrTest { get; init; }
    public ComplianceTest? SupplementalCombinedLtvTest { get; init; }

    // Green Rewards NCF adjustment (null when not Green)
    public decimal? GreenNcfAdjustment { get; init; }
    public decimal? AdjustedNcf { get; init; }
}

/// <summary>
/// A single compliance test result: pass/fail with actual vs. required values.
/// </summary>
public sealed record ComplianceTest
{
    public string Name { get; init; } = string.Empty;
    public bool Pass { get; init; }
    public decimal ActualValue { get; init; }
    public decimal RequiredValue { get; init; }
    public string? Notes { get; init; }
}
