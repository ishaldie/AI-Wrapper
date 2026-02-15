namespace ZSR.Underwriting.Application.DTOs.Report;

/// <summary>
/// Complete assembled underwriting report containing all 10 protocol sections.
/// </summary>
public class UnderwritingReportDto
{
    public Guid DealId { get; init; }
    public string PropertyName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    // The 10 protocol sections in exact order
    public CoreMetricsSection CoreMetrics { get; init; } = new();
    public ExecutiveSummarySection ExecutiveSummary { get; init; } = new();
    public AssumptionsSection Assumptions { get; init; } = new();
    public PropertyCompsSection PropertyComps { get; init; } = new();
    public TenantMarketSection TenantMarket { get; init; } = new();
    public OperationsSection Operations { get; init; } = new();
    public FinancialAnalysisSection FinancialAnalysis { get; init; } = new();
    public ValueCreationSection ValueCreation { get; init; } = new();
    public RiskAssessmentSection RiskAssessment { get; init; } = new();
    public InvestmentDecisionSection InvestmentDecision { get; init; } = new();

    /// <summary>Returns all sections in protocol order (1-10).</summary>
    public IReadOnlyList<ReportSectionBase> GetSectionsInOrder() =>
    [
        CoreMetrics,
        ExecutiveSummary,
        Assumptions,
        PropertyComps,
        TenantMarket,
        Operations,
        FinancialAnalysis,
        ValueCreation,
        RiskAssessment,
        InvestmentDecision
    ];
}
