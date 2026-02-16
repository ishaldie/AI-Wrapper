using ZSR.Underwriting.Application.DTOs.Report;

namespace ZSR.Underwriting.Application.DTOs;

/// <summary>
/// All AI-generated prose sections for an underwriting report.
/// </summary>
public class GeneratedProse
{
    public string ExecutiveSummaryNarrative { get; init; } = string.Empty;
    public List<string> KeyHighlights { get; init; } = [];
    public List<string> KeyRisks { get; init; } = [];

    public string MarketContextNarrative { get; init; } = string.Empty;

    public string ValueCreationNarrative { get; init; } = string.Empty;

    public string RiskAssessmentNarrative { get; init; } = string.Empty;
    public List<RiskItem> Risks { get; init; } = [];

    public InvestmentDecisionType Decision { get; init; }
    public string InvestmentThesis { get; init; } = string.Empty;
    public List<string> Conditions { get; init; } = [];
    public List<string> NextSteps { get; init; } = [];

    public string PropertyOverviewNarrative { get; init; } = string.Empty;

    /// <summary>Total input tokens used across all API calls.</summary>
    public int TotalInputTokens { get; init; }

    /// <summary>Total output tokens used across all API calls.</summary>
    public int TotalOutputTokens { get; init; }

    /// <summary>Sections that failed to generate (null = all succeeded).</summary>
    public List<string>? FailedSections { get; init; }

    public bool HasFailures => FailedSections is { Count: > 0 };
}
