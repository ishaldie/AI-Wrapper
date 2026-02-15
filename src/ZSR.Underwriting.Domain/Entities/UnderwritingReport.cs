namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// Final assembled underwriting report with all 10 sections and GO/NO GO decision.
/// </summary>
public class UnderwritingReport
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
    public DateTime GeneratedAt { get; private set; }

    // 10 report sections (Claude AI generated prose)
    public string? ExecutiveSummary { get; set; }
    public string? PropertyOverview { get; set; }
    public string? MarketAnalysis { get; set; }
    public string? FinancialAnalysis { get; set; }
    public string? RentAnalysis { get; set; }
    public string? ExpenseAnalysis { get; set; }
    public string? DebtAnalysis { get; set; }
    public string? ReturnAnalysis { get; set; }
    public string? RiskAssessment { get; set; }
    public string? InvestmentThesis { get; set; }

    // Final decision
    public bool? IsGoDecision { get; set; }
    public string? DecisionRationale { get; set; }

    // EF Core parameterless constructor
    private UnderwritingReport() { }

    public UnderwritingReport(Guid dealId)
    {
        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId cannot be empty.", nameof(dealId));

        Id = Guid.NewGuid();
        DealId = dealId;
        GeneratedAt = DateTime.UtcNow;
    }
}
