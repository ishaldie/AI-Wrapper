using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Tests.DTOs;

public class QuickAnalysisProgressTests
{
    [Fact]
    public void TotalSteps_Returns_Three()
    {
        var progress = new QuickAnalysisProgress();
        Assert.Equal(3, progress.TotalSteps);
    }

    [Fact]
    public void CompletedSteps_InitiallyZero()
    {
        var progress = new QuickAnalysisProgress();
        Assert.Equal(0, progress.CompletedSteps);
    }

    [Fact]
    public void CompletedSteps_CountsOnlyCompleteStatus()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            AiResearch = StepStatus.Failed,
            ReportReady = StepStatus.InProgress
        };

        Assert.Equal(1, progress.CompletedSteps);
    }

    [Fact]
    public void IsComplete_TrueWhenAllThreeComplete()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            AiResearch = StepStatus.Complete,
            ReportReady = StepStatus.Complete
        };

        Assert.True(progress.IsComplete);
        Assert.Equal(3, progress.CompletedSteps);
    }

    [Fact]
    public void IsComplete_FalseWhenAnyNotComplete()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            AiResearch = StepStatus.Complete,
            ReportReady = StepStatus.Failed
        };

        Assert.False(progress.IsComplete);
    }

    [Fact]
    public void HasErrors_TrueWhenAnyFailed()
    {
        var progress = new QuickAnalysisProgress
        {
            AiResearch = StepStatus.Failed
        };

        Assert.True(progress.HasErrors);
    }

    [Fact]
    public void HasErrors_FalseWhenNoneFailed()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            AiResearch = StepStatus.InProgress
        };

        Assert.False(progress.HasErrors);
    }

    [Fact]
    public void IsFinished_TrueWhenAllComplete()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            AiResearch = StepStatus.Complete,
            ReportReady = StepStatus.Complete
        };

        Assert.True(progress.IsFinished);
    }

    [Fact]
    public void IsFinished_TrueWhenErrorsAndNoInProgress()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            AiResearch = StepStatus.Failed,
            ReportReady = StepStatus.Complete
        };

        Assert.True(progress.IsFinished);
    }

    [Fact]
    public void IsFinished_FalseWhenErrorsButStillInProgress()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            AiResearch = StepStatus.Failed,
            ReportReady = StepStatus.InProgress
        };

        Assert.False(progress.IsFinished);
    }

    [Fact]
    public void GetStepStatus_ReturnsCorrectStatus()
    {
        var progress = new QuickAnalysisProgress
        {
            AiResearch = StepStatus.InProgress
        };

        Assert.Equal(StepStatus.InProgress, progress.GetStepStatus(AnalysisStep.AiResearch));
        Assert.Equal(StepStatus.Pending, progress.GetStepStatus(AnalysisStep.ReportReady));
    }

    [Fact]
    public void SetStepStatus_UpdatesCorrectStep()
    {
        var progress = new QuickAnalysisProgress();
        progress.SetStepStatus(AnalysisStep.ReportReady, StepStatus.Complete);

        Assert.Equal(StepStatus.Complete, progress.ReportReady);
        Assert.Equal(StepStatus.Pending, progress.DealCreation);
    }

    [Theory]
    [InlineData(AnalysisStep.DealCreation, "Creating deal record")]
    [InlineData(AnalysisStep.AiResearch, "Running AI research & analysis")]
    [InlineData(AnalysisStep.ReportReady, "Finalizing report")]
    public void GetStepLabel_ReturnsExpectedLabel(AnalysisStep step, string expected)
    {
        Assert.Equal(expected, QuickAnalysisProgress.GetStepLabel(step));
    }
}
