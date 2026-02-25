namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// All computed underwriting metrics for a deal.
/// </summary>
public class CalculationResult
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
    public DateTime CalculatedAt { get; private set; }

    // Revenue metrics
    public decimal? GrossPotentialRent { get; set; }
    public decimal? VacancyLoss { get; set; }
    public decimal? EffectiveGrossIncome { get; set; }
    public decimal? OtherIncome { get; set; }

    // Expense metrics
    public decimal? OperatingExpenses { get; set; }
    public decimal? NetOperatingIncome { get; set; }
    public decimal? NoiMargin { get; set; }

    // Cap rate metrics
    public decimal? GoingInCapRate { get; set; }
    public decimal? ExitCapRate { get; set; }
    public decimal? PricePerUnit { get; set; }

    // Debt metrics
    public decimal? LoanAmount { get; set; }
    public decimal? AnnualDebtService { get; set; }
    public decimal? DebtServiceCoverageRatio { get; set; }

    // Return metrics
    public decimal? CashOnCashReturn { get; set; }
    public decimal? InternalRateOfReturn { get; set; }
    public decimal? EquityMultiple { get; set; }

    // Exit metrics
    public decimal? ExitValue { get; set; }
    public decimal? TotalProfit { get; set; }

    // Cash flow projections (stored as JSON)
    public string? CashFlowProjectionsJson { get; set; }

    // Sensitivity analysis (stored as JSON)
    public string? SensitivityAnalysisJson { get; set; }

    // HUD Affordability metrics
    public int? AffordabilityPercentAmi { get; set; }
    public string? AffordabilityTier { get; set; }
    public string? AffordabilityDataJson { get; set; }

    // EF Core parameterless constructor
    private CalculationResult() { }

    public CalculationResult(Guid dealId)
    {
        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId cannot be empty.", nameof(dealId));

        Id = Guid.NewGuid();
        DealId = dealId;
        CalculatedAt = DateTime.UtcNow;
    }
}
