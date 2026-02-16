using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Tests.DTOs;

public class QuickAnalysisProgressTests
{
    [Fact]
    public void TotalSteps_Returns_Nine()
    {
        var progress = new QuickAnalysisProgress();
        Assert.Equal(9, progress.TotalSteps);
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
            PropertyData = StepStatus.Complete,
            TenantMetrics = StepStatus.Failed,
            MarketData = StepStatus.InProgress,
            SalesComps = StepStatus.Pending
        };

        Assert.Equal(2, progress.CompletedSteps);
    }

    [Fact]
    public void IsComplete_TrueWhenAllNineComplete()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            PropertyData = StepStatus.Complete,
            TenantMetrics = StepStatus.Complete,
            MarketData = StepStatus.Complete,
            SalesComps = StepStatus.Complete,
            TimeSeries = StepStatus.Complete,
            MarketContext = StepStatus.Complete,
            ReportAssembly = StepStatus.Complete,
            AiProse = StepStatus.Complete
        };

        Assert.True(progress.IsComplete);
        Assert.Equal(9, progress.CompletedSteps);
    }

    [Fact]
    public void IsComplete_FalseWhenAnyNotComplete()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            PropertyData = StepStatus.Complete,
            TenantMetrics = StepStatus.Complete,
            MarketData = StepStatus.Complete,
            SalesComps = StepStatus.Complete,
            TimeSeries = StepStatus.Complete,
            MarketContext = StepStatus.Complete,
            ReportAssembly = StepStatus.Complete,
            AiProse = StepStatus.Failed
        };

        Assert.False(progress.IsComplete);
    }

    [Fact]
    public void HasErrors_TrueWhenAnyFailed()
    {
        var progress = new QuickAnalysisProgress
        {
            PropertyData = StepStatus.Failed
        };

        Assert.True(progress.HasErrors);
    }

    [Fact]
    public void HasErrors_FalseWhenNoneFailed()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            PropertyData = StepStatus.InProgress
        };

        Assert.False(progress.HasErrors);
    }

    [Fact]
    public void IsFinished_TrueWhenCompleteEvenWithNoErrors()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            PropertyData = StepStatus.Complete,
            TenantMetrics = StepStatus.Complete,
            MarketData = StepStatus.Complete,
            SalesComps = StepStatus.Complete,
            TimeSeries = StepStatus.Complete,
            MarketContext = StepStatus.Complete,
            ReportAssembly = StepStatus.Complete,
            AiProse = StepStatus.Complete
        };

        Assert.True(progress.IsFinished);
    }

    [Fact]
    public void IsFinished_TrueWhenErrorsAndNoInProgress()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            PropertyData = StepStatus.Failed,
            TenantMetrics = StepStatus.Complete,
            MarketData = StepStatus.Complete,
            SalesComps = StepStatus.Complete,
            TimeSeries = StepStatus.Complete,
            MarketContext = StepStatus.Complete,
            ReportAssembly = StepStatus.Complete,
            AiProse = StepStatus.Complete
        };

        Assert.True(progress.IsFinished);
    }

    [Fact]
    public void IsFinished_FalseWhenErrorsButStillInProgress()
    {
        var progress = new QuickAnalysisProgress
        {
            DealCreation = StepStatus.Complete,
            PropertyData = StepStatus.Failed,
            TenantMetrics = StepStatus.InProgress
        };

        Assert.False(progress.IsFinished);
    }

    [Fact]
    public void GetStepStatus_ReturnsCorrectStatus()
    {
        var progress = new QuickAnalysisProgress
        {
            MarketData = StepStatus.InProgress
        };

        Assert.Equal(StepStatus.InProgress, progress.GetStepStatus(AnalysisStep.MarketData));
        Assert.Equal(StepStatus.Pending, progress.GetStepStatus(AnalysisStep.AiProse));
    }

    [Fact]
    public void SetStepStatus_UpdatesCorrectStep()
    {
        var progress = new QuickAnalysisProgress();
        progress.SetStepStatus(AnalysisStep.ReportAssembly, StepStatus.Complete);

        Assert.Equal(StepStatus.Complete, progress.ReportAssembly);
        Assert.Equal(StepStatus.Pending, progress.DealCreation);
    }

    [Theory]
    [InlineData(AnalysisStep.DealCreation, "Creating deal record")]
    [InlineData(AnalysisStep.PropertyData, "Fetching property data")]
    [InlineData(AnalysisStep.AiProse, "Generating AI analysis")]
    public void GetStepLabel_ReturnsExpectedLabel(AnalysisStep step, string expected)
    {
        Assert.Equal(expected, QuickAnalysisProgress.GetStepLabel(step));
    }
}
