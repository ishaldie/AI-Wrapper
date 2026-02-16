using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Tests.Infrastructure;

public class ActivityTrackingDbTests : IDisposable
{
    private readonly AppDbContext _ctx;

    public ActivityTrackingDbTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Has_UserSessions_DbSet()
    {
        Assert.NotNull(_ctx.UserSessions);
    }

    [Fact]
    public void Has_ActivityEvents_DbSet()
    {
        Assert.NotNull(_ctx.ActivityEvents);
    }

    [Fact]
    public async Task Can_Create_And_Read_UserSession()
    {
        var session = new UserSession("user-1");
        _ctx.UserSessions.Add(session);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.UserSessions.FindAsync(session.Id);
        Assert.NotNull(loaded);
        Assert.Equal("user-1", loaded.UserId);
    }

    [Fact]
    public async Task Can_Create_And_Read_ActivityEvent()
    {
        var session = new UserSession("user-2");
        _ctx.UserSessions.Add(session);
        await _ctx.SaveChangesAsync();

        var evt = new ActivityEvent(session.Id, "user-2", ActivityEventType.PageView)
        {
            PageUrl = "/deals"
        };
        _ctx.ActivityEvents.Add(evt);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.ActivityEvents.FindAsync(evt.Id);
        Assert.NotNull(loaded);
        Assert.Equal(ActivityEventType.PageView, loaded.EventType);
        Assert.Equal("/deals", loaded.PageUrl);
    }

    [Fact]
    public async Task EventType_Stored_As_String()
    {
        var session = new UserSession("user-3");
        _ctx.UserSessions.Add(session);
        var evt = new ActivityEvent(session.Id, "user-3", ActivityEventType.SearchPerformed);
        _ctx.ActivityEvents.Add(evt);
        await _ctx.SaveChangesAsync();

        // Verify we can query by string-based EventType
        var result = await _ctx.ActivityEvents
            .Where(e => e.EventType == ActivityEventType.SearchPerformed)
            .ToListAsync();
        Assert.Single(result);
    }

    [Fact]
    public async Task Session_To_Events_Cascade_Delete()
    {
        var session = new UserSession("user-4");
        _ctx.UserSessions.Add(session);
        await _ctx.SaveChangesAsync();

        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "user-4", ActivityEventType.SessionStart));
        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "user-4", ActivityEventType.PageView) { PageUrl = "/search" });
        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "user-4", ActivityEventType.SessionEnd));
        await _ctx.SaveChangesAsync();

        Assert.Equal(3, await _ctx.ActivityEvents.CountAsync(e => e.SessionId == session.Id));

        _ctx.UserSessions.Remove(session);
        await _ctx.SaveChangesAsync();

        Assert.Empty(await _ctx.ActivityEvents.Where(e => e.SessionId == session.Id).ToListAsync());
    }

    [Fact]
    public async Task Session_Navigation_Loads_Events()
    {
        var session = new UserSession("user-5");
        _ctx.UserSessions.Add(session);
        await _ctx.SaveChangesAsync();

        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "user-5", ActivityEventType.SessionStart));
        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "user-5", ActivityEventType.PageView));
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.UserSessions
            .Include(s => s.ActivityEvents)
            .FirstAsync(s => s.Id == session.Id);

        Assert.Equal(2, loaded.ActivityEvents.Count);
    }

    [Fact]
    public async Task Can_Query_Events_By_UserId()
    {
        var session = new UserSession("query-user");
        _ctx.UserSessions.Add(session);
        await _ctx.SaveChangesAsync();

        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "query-user", ActivityEventType.SearchPerformed));
        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "query-user", ActivityEventType.ReportViewed));
        await _ctx.SaveChangesAsync();

        var events = await _ctx.ActivityEvents
            .Where(e => e.UserId == "query-user")
            .ToListAsync();

        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task Can_Query_Events_By_DealId()
    {
        var session = new UserSession("deal-user");
        _ctx.UserSessions.Add(session);
        await _ctx.SaveChangesAsync();

        var dealId = Guid.NewGuid();
        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "deal-user", ActivityEventType.ReportViewed) { DealId = dealId });
        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "deal-user", ActivityEventType.PdfExported) { DealId = dealId });
        _ctx.ActivityEvents.Add(new ActivityEvent(session.Id, "deal-user", ActivityEventType.PageView));
        await _ctx.SaveChangesAsync();

        var events = await _ctx.ActivityEvents
            .Where(e => e.DealId == dealId)
            .ToListAsync();

        Assert.Equal(2, events.Count);
    }
}
