using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;

namespace ZSR.Underwriting.Tests.Services;

public class QuickAnalysisTrackerTests
{
    [Fact]
    public void Register_And_GetProgress_ReturnsProgress()
    {
        var dealId = Guid.NewGuid();
        var progress = new QuickAnalysisProgress { DealId = dealId, SearchQuery = "123 Main St" };

        QuickAnalysisTracker.Register(dealId, progress);
        var result = QuickAnalysisTracker.GetProgress(dealId);

        Assert.NotNull(result);
        Assert.Equal("123 Main St", result.SearchQuery);

        // Cleanup
        QuickAnalysisTracker.Remove(dealId);
    }

    [Fact]
    public void GetProgress_ReturnsNull_WhenNotRegistered()
    {
        var result = QuickAnalysisTracker.GetProgress(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void Remove_ReturnsTrue_WhenExists()
    {
        var dealId = Guid.NewGuid();
        QuickAnalysisTracker.Register(dealId, new QuickAnalysisProgress { DealId = dealId });

        var removed = QuickAnalysisTracker.Remove(dealId);

        Assert.True(removed);
        Assert.Null(QuickAnalysisTracker.GetProgress(dealId));
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenNotExists()
    {
        var removed = QuickAnalysisTracker.Remove(Guid.NewGuid());
        Assert.False(removed);
    }

    [Fact]
    public void Register_OverwritesExisting()
    {
        var dealId = Guid.NewGuid();
        var first = new QuickAnalysisProgress { DealId = dealId, SearchQuery = "First" };
        var second = new QuickAnalysisProgress { DealId = dealId, SearchQuery = "Second" };

        QuickAnalysisTracker.Register(dealId, first);
        QuickAnalysisTracker.Register(dealId, second);

        var result = QuickAnalysisTracker.GetProgress(dealId);
        Assert.Equal("Second", result?.SearchQuery);

        // Cleanup
        QuickAnalysisTracker.Remove(dealId);
    }
}
