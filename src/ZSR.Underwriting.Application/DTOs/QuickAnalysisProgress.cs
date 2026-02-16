namespace ZSR.Underwriting.Application.DTOs;

public enum StepStatus
{
    Pending,
    InProgress,
    Complete,
    Failed
}

public enum AnalysisStep
{
    DealCreation,
    PropertyData,
    TenantMetrics,
    MarketData,
    SalesComps,
    TimeSeries,
    MarketContext,
    ReportAssembly,
    AiProse
}

public class QuickAnalysisProgress
{
    public Guid DealId { get; set; }
    public string SearchQuery { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }

    public StepStatus DealCreation { get; set; }
    public StepStatus PropertyData { get; set; }
    public StepStatus TenantMetrics { get; set; }
    public StepStatus MarketData { get; set; }
    public StepStatus SalesComps { get; set; }
    public StepStatus TimeSeries { get; set; }
    public StepStatus MarketContext { get; set; }
    public StepStatus ReportAssembly { get; set; }
    public StepStatus AiProse { get; set; }

    public int TotalSteps => 9;

    public int CompletedSteps
    {
        get
        {
            var count = 0;
            if (DealCreation == StepStatus.Complete) count++;
            if (PropertyData == StepStatus.Complete) count++;
            if (TenantMetrics == StepStatus.Complete) count++;
            if (MarketData == StepStatus.Complete) count++;
            if (SalesComps == StepStatus.Complete) count++;
            if (TimeSeries == StepStatus.Complete) count++;
            if (MarketContext == StepStatus.Complete) count++;
            if (ReportAssembly == StepStatus.Complete) count++;
            if (AiProse == StepStatus.Complete) count++;
            return count;
        }
    }

    public bool IsComplete => CompletedSteps == TotalSteps;

    public bool HasErrors =>
        DealCreation == StepStatus.Failed ||
        PropertyData == StepStatus.Failed ||
        TenantMetrics == StepStatus.Failed ||
        MarketData == StepStatus.Failed ||
        SalesComps == StepStatus.Failed ||
        TimeSeries == StepStatus.Failed ||
        MarketContext == StepStatus.Failed ||
        ReportAssembly == StepStatus.Failed ||
        AiProse == StepStatus.Failed;

    public bool IsFinished => IsComplete || (HasErrors && !HasInProgressSteps);

    private bool HasInProgressSteps =>
        DealCreation == StepStatus.InProgress ||
        PropertyData == StepStatus.InProgress ||
        TenantMetrics == StepStatus.InProgress ||
        MarketData == StepStatus.InProgress ||
        SalesComps == StepStatus.InProgress ||
        TimeSeries == StepStatus.InProgress ||
        MarketContext == StepStatus.InProgress ||
        ReportAssembly == StepStatus.InProgress ||
        AiProse == StepStatus.InProgress;

    public StepStatus GetStepStatus(AnalysisStep step) => step switch
    {
        AnalysisStep.DealCreation => DealCreation,
        AnalysisStep.PropertyData => PropertyData,
        AnalysisStep.TenantMetrics => TenantMetrics,
        AnalysisStep.MarketData => MarketData,
        AnalysisStep.SalesComps => SalesComps,
        AnalysisStep.TimeSeries => TimeSeries,
        AnalysisStep.MarketContext => MarketContext,
        AnalysisStep.ReportAssembly => ReportAssembly,
        AnalysisStep.AiProse => AiProse,
        _ => StepStatus.Pending
    };

    public void SetStepStatus(AnalysisStep step, StepStatus status)
    {
        switch (step)
        {
            case AnalysisStep.DealCreation: DealCreation = status; break;
            case AnalysisStep.PropertyData: PropertyData = status; break;
            case AnalysisStep.TenantMetrics: TenantMetrics = status; break;
            case AnalysisStep.MarketData: MarketData = status; break;
            case AnalysisStep.SalesComps: SalesComps = status; break;
            case AnalysisStep.TimeSeries: TimeSeries = status; break;
            case AnalysisStep.MarketContext: MarketContext = status; break;
            case AnalysisStep.ReportAssembly: ReportAssembly = status; break;
            case AnalysisStep.AiProse: AiProse = status; break;
        }
    }

    public static string GetStepLabel(AnalysisStep step) => step switch
    {
        AnalysisStep.DealCreation => "Creating deal record",
        AnalysisStep.PropertyData => "Fetching property data",
        AnalysisStep.TenantMetrics => "Analyzing tenant metrics",
        AnalysisStep.MarketData => "Pulling market data",
        AnalysisStep.SalesComps => "Finding sales comparables",
        AnalysisStep.TimeSeries => "Loading time series trends",
        AnalysisStep.MarketContext => "Researching market context",
        AnalysisStep.ReportAssembly => "Assembling underwriting report",
        AnalysisStep.AiProse => "Generating AI analysis",
        _ => "Processing"
    };
}
