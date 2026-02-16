using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Domain;

public class UserSessionTests
{
    [Fact]
    public void Constructor_Sets_UserId()
    {
        var session = new UserSession("user-123");
        Assert.Equal("user-123", session.UserId);
    }

    [Fact]
    public void Constructor_Sets_NonEmpty_Id()
    {
        var session = new UserSession("user-123");
        Assert.NotEqual(Guid.Empty, session.Id);
    }

    [Fact]
    public void Constructor_Sets_ConnectedAt()
    {
        var before = DateTime.UtcNow;
        var session = new UserSession("user-123");
        var after = DateTime.UtcNow;

        Assert.InRange(session.ConnectedAt, before, after);
    }

    [Fact]
    public void Constructor_DisconnectedAt_Is_Null()
    {
        var session = new UserSession("user-123");
        Assert.Null(session.DisconnectedAt);
    }

    [Fact]
    public void Constructor_ActivityEvents_Is_Empty()
    {
        var session = new UserSession("user-123");
        Assert.NotNull(session.ActivityEvents);
        Assert.Empty(session.ActivityEvents);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_When_UserId_Is_Empty(string? userId)
    {
        Assert.Throws<ArgumentException>(() => new UserSession(userId!));
    }

    [Fact]
    public void MarkDisconnected_Sets_Timestamp()
    {
        var session = new UserSession("user-123");
        Assert.Null(session.DisconnectedAt);

        var before = DateTime.UtcNow;
        session.MarkDisconnected();
        var after = DateTime.UtcNow;

        Assert.NotNull(session.DisconnectedAt);
        Assert.InRange(session.DisconnectedAt.Value, before, after);
    }
}
