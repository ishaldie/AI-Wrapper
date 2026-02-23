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
    AiResearch,
    ReportReady
}

public class QuickAnalysisProgress
{
    public Guid DealId { get; set; }
    public string SearchQuery { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }

    public StepStatus DealCreation { get; set; }
    public StepStatus AiResearch { get; set; }
    public StepStatus ReportReady { get; set; }

    /// <summary>
    /// Claude's markdown analysis content, held in-memory alongside progress.
    /// </summary>
    public string? AnalysisContent { get; set; }

    public int TotalSteps => 3;

    public int CompletedSteps
    {
        get
        {
            var count = 0;
            if (DealCreation == StepStatus.Complete) count++;
            if (AiResearch == StepStatus.Complete) count++;
            if (ReportReady == StepStatus.Complete) count++;
            return count;
        }
    }

    public bool IsComplete => CompletedSteps == TotalSteps;

    public bool HasErrors =>
        DealCreation == StepStatus.Failed ||
        AiResearch == StepStatus.Failed ||
        ReportReady == StepStatus.Failed;

    public bool IsFinished => IsComplete || (HasErrors && !HasInProgressSteps);

    private bool HasInProgressSteps =>
        DealCreation == StepStatus.InProgress ||
        AiResearch == StepStatus.InProgress ||
        ReportReady == StepStatus.InProgress;

    public StepStatus GetStepStatus(AnalysisStep step) => step switch
    {
        AnalysisStep.DealCreation => DealCreation,
        AnalysisStep.AiResearch => AiResearch,
        AnalysisStep.ReportReady => ReportReady,
        _ => StepStatus.Pending
    };

    public void SetStepStatus(AnalysisStep step, StepStatus status)
    {
        switch (step)
        {
            case AnalysisStep.DealCreation: DealCreation = status; break;
            case AnalysisStep.AiResearch: AiResearch = status; break;
            case AnalysisStep.ReportReady: ReportReady = status; break;
        }
    }

    public static string GetStepLabel(AnalysisStep step) => step switch
    {
        AnalysisStep.DealCreation => "Creating deal record",
        AnalysisStep.AiResearch => "Running AI research & analysis",
        AnalysisStep.ReportReady => "Finalizing report",
        _ => "Processing"
    };
}
