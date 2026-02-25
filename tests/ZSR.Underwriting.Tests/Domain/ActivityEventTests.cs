using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Domain;

public class ActivityEventTests
{
    [Fact]
    public void Constructor_Sets_Required_Properties()
    {
        var sessionId = Guid.NewGuid();
        var evt = new ActivityEvent(sessionId, "user-123", ActivityEventType.PageView);

        Assert.NotEqual(Guid.Empty, evt.Id);
        Assert.Equal(sessionId, evt.SessionId);
        Assert.Equal("user-123", evt.UserId);
        Assert.Equal(ActivityEventType.PageView, evt.EventType);
    }

    [Fact]
    public void Constructor_Sets_OccurredAt()
    {
        var before = DateTime.UtcNow;
        var evt = new ActivityEvent(Guid.NewGuid(), "user-123", ActivityEventType.SessionStart);
        var after = DateTime.UtcNow;

        Assert.InRange(evt.OccurredAt, before, after);
    }

    [Fact]
    public void Optional_Properties_Default_To_Null()
    {
        var evt = new ActivityEvent(Guid.NewGuid(), "user-123", ActivityEventType.SearchPerformed);

        Assert.Null(evt.PageUrl);
        Assert.Null(evt.DealId);
        Assert.Null(evt.Metadata);
        Assert.Null(evt.IpAddress);
    }

    [Fact]
    public void Optional_Properties_Can_Be_Set()
    {
        var dealId = Guid.NewGuid();
        var evt = new ActivityEvent(Guid.NewGuid(), "user-123", ActivityEventType.ReportViewed)
        {
            PageUrl = "/deals/report",
            DealId = dealId,
            Metadata = "{\"key\":\"value\"}"
        };

        Assert.Equal("/deals/report", evt.PageUrl);
        Assert.Equal(dealId, evt.DealId);
        Assert.Equal("{\"key\":\"value\"}", evt.Metadata);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_When_UserId_Is_Empty(string? userId)
    {
        Assert.Throws<ArgumentException>(() => new ActivityEvent(Guid.NewGuid(), userId!, ActivityEventType.PageView));
    }

    [Fact]
    public void ActivityEventType_Has_All_Expected_Values()
    {
        var values = Enum.GetValues<ActivityEventType>();
        Assert.Contains(ActivityEventType.SessionStart, values);
        Assert.Contains(ActivityEventType.SessionEnd, values);
        Assert.Contains(ActivityEventType.PageView, values);
        Assert.Contains(ActivityEventType.SearchPerformed, values);
        Assert.Contains(ActivityEventType.QuickAnalysisStarted, values);
        Assert.Contains(ActivityEventType.WizardStarted, values);
        Assert.Contains(ActivityEventType.WizardCompleted, values);
        Assert.Contains(ActivityEventType.ReportViewed, values);
        Assert.Contains(ActivityEventType.PdfExported, values);
        Assert.Contains(ActivityEventType.DocumentUploaded, values);
        Assert.Contains(ActivityEventType.DealCreated, values);
        Assert.Contains(ActivityEventType.DealDeleted, values);
    }
}
