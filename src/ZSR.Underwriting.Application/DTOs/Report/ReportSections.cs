using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.DTOs.Report;

/// <summary>Base class for all report sections with common metadata.</summary>
public abstract class ReportSectionBase
{
    public int SectionNumber { get; init; }
    public string Title { get; init; } = string.Empty;
}

public class CoreMetricsSection : ReportSectionBase
{
    public CoreMetricsSection() { SectionNumber = 1; Title = "Core Investment Metrics"; }
    public decimal PurchasePrice { get; init; }
    public int UnitCount { get; init; }
    public decimal PricePerUnit { get; init; }
    public decimal PricePerSf { get; init; }
    public decimal CapRate { get; init; }
    public decimal Noi { get; init; }
    public decimal Egi { get; init; }
    public decimal OpExRatio { get; init; }
    public decimal Dscr { get; init; }
    public decimal LoanAmount { get; init; }
    public decimal LtvPercent { get; init; }
    public decimal CashOnCash { get; init; }
    public decimal Irr { get; init; }
    public decimal EquityMultiple { get; init; }
    public List<MetricRow> Metrics { get; init; } = [];
}

public class MetricRow
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public DataSource Source { get; init; }
}

public class ExecutiveSummarySection : ReportSectionBase
{
    public ExecutiveSummarySection() { SectionNumber = 2; Title = "Executive Summary"; }
    public InvestmentDecisionType Decision { get; init; }
    public string DecisionLabel { get; init; } = string.Empty;
    public string Narrative { get; init; } = string.Empty;
    public List<string> KeyHighlights { get; init; } = [];
    public List<string> KeyRisks { get; init; } = [];
}

public class AssumptionsSection : ReportSectionBase
{
    public AssumptionsSection() { SectionNumber = 3; Title = "Underwriting Assumptions"; }
    public List<AssumptionRow> Assumptions { get; init; } = [];
}

public class AssumptionRow
{
    public string Parameter { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public DataSource Source { get; init; }
}

public class PropertyCompsSection : ReportSectionBase
{
    public PropertyCompsSection() { SectionNumber = 4; Title = "Property & Sales Comparables"; }
    public string Narrative { get; init; } = string.Empty;
    public List<SalesCompRow> Comps { get; init; } = [];
    public List<AdjustmentRow> Adjustments { get; init; } = [];
}

public class SalesCompRow
{
    public string Address { get; init; } = string.Empty;
    public decimal SalePrice { get; init; }
    public int Units { get; init; }
    public decimal PricePerUnit { get; init; }
    public decimal CapRate { get; init; }
    public DateTime SaleDate { get; init; }
    public decimal DistanceMiles { get; init; }
}

public class AdjustmentRow
{
    public string Factor { get; init; } = string.Empty;
    public string Adjustment { get; init; } = string.Empty;
    public string Rationale { get; init; } = string.Empty;
}

public class TenantMarketSection : ReportSectionBase
{
    public TenantMarketSection() { SectionNumber = 5; Title = "Tenant & Market Intelligence"; }
    public string Narrative { get; init; } = string.Empty;
    public List<BenchmarkRow> Benchmarks { get; init; } = [];
    public decimal MarketRentPerUnit { get; init; }
    public decimal SubjectRentPerUnit { get; init; }
    public decimal MarketOccupancy { get; init; }
    public decimal SubjectOccupancy { get; init; }
    public AffordabilityResultDto? Affordability { get; init; }
}

public class BenchmarkRow
{
    public string Metric { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Market { get; init; } = string.Empty;
    public string Variance { get; init; } = string.Empty;
}

public class OperationsSection : ReportSectionBase
{
    public OperationsSection() { SectionNumber = 6; Title = "Operations T12 P&L"; }
    public string Commentary { get; init; } = string.Empty;
    public List<PnlRow> RevenueItems { get; init; } = [];
    public List<PnlRow> ExpenseItems { get; init; } = [];
    public decimal TotalRevenue { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal Noi { get; init; }
    public decimal NoiMargin { get; init; }
}

public class PnlRow
{
    public string LineItem { get; init; } = string.Empty;
    public decimal Annual { get; init; }
    public decimal PerUnit { get; init; }
    public decimal PercentOfEgi { get; init; }
}

public class FinancialAnalysisSection : ReportSectionBase
{
    public FinancialAnalysisSection() { SectionNumber = 7; Title = "Financial Analysis"; }
    public SourcesAndUses SourcesAndUses { get; init; } = new();
    public List<CashFlowYear> FiveYearCashFlow { get; init; } = [];
    public ReturnsAnalysis Returns { get; init; } = new();
    public ExitAnalysis Exit { get; init; } = new();
}

public class SourcesAndUses
{
    public decimal LoanAmount { get; init; }
    public decimal EquityRequired { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal ClosingCosts { get; init; }
    public decimal CapexReserve { get; init; }
    public decimal TotalUses { get; init; }
    public decimal TotalSources { get; init; }

    // Dual-constraint loan sizing
    public decimal LtvBasedLoan { get; init; }
    public decimal DscrBasedLoan { get; init; }
    public string ConstrainingTest { get; init; } = "LTV";
}

public class CashFlowYear
{
    public int Year { get; init; }
    public decimal Egi { get; init; }
    public decimal OpEx { get; init; }
    public decimal Noi { get; init; }
    public decimal DebtService { get; init; }
    public decimal CashFlow { get; init; }
    public decimal CashOnCash { get; init; }
}

public class ReturnsAnalysis
{
    public decimal Irr { get; init; }
    public decimal EquityMultiple { get; init; }
    public decimal AverageCashOnCash { get; init; }
    public decimal TotalProfit { get; init; }
}

public class ExitAnalysis
{
    public decimal ExitCapRate { get; init; }
    public decimal ExitNoi { get; init; }
    public decimal ExitValue { get; init; }
    public decimal LoanBalance { get; init; }
    public decimal NetProceeds { get; init; }
}

public class ValueCreationSection : ReportSectionBase
{
    public ValueCreationSection() { SectionNumber = 8; Title = "Value Creation Strategy"; }
    public string Narrative { get; init; } = string.Empty;
    public List<ValueAddItem> Strategies { get; init; } = [];
    public decimal TotalValueAdd { get; init; }
}

public class ValueAddItem
{
    public string Strategy { get; init; } = string.Empty;
    public string Timeline { get; init; } = string.Empty;
    public decimal EstimatedCost { get; init; }
    public decimal EstimatedValueAdd { get; init; }
}

public class RiskAssessmentSection : ReportSectionBase
{
    public RiskAssessmentSection() { SectionNumber = 9; Title = "Risk Assessment"; }
    public string Narrative { get; init; } = string.Empty;
    public List<RiskItem> Risks { get; init; } = [];
}

public class RiskItem
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public RiskSeverity Severity { get; init; }
    public string Mitigation { get; init; } = string.Empty;
}

public class InvestmentDecisionSection : ReportSectionBase
{
    public InvestmentDecisionSection() { SectionNumber = 10; Title = "Investment Decision"; }
    public InvestmentDecisionType Decision { get; init; }
    public string DecisionLabel { get; init; } = string.Empty;
    public string InvestmentThesis { get; init; } = string.Empty;
    public List<string> Conditions { get; init; } = [];
    public List<string> NextSteps { get; init; } = [];
}
