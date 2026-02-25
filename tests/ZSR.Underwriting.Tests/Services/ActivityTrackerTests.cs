using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class ActivityTrackerTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _dbName;

    public ActivityTrackerTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public void Dispose() => _provider.Dispose();

    private ActivityTracker CreateTracker(IPAddress? remoteIp = null)
    {
        var scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
        var logger = _provider.GetRequiredService<ILogger<ActivityTracker>>();
        var httpContextAccessor = new FakeHttpContextAccessor(remoteIp);
        return new ActivityTracker(scopeFactory, logger, httpContextAccessor);
    }

    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }

        public FakeHttpContextAccessor(IPAddress? remoteIp)
        {
            if (remoteIp is not null)
            {
                var context = new DefaultHttpContext();
                context.Connection.RemoteIpAddress = remoteIp;
                HttpContext = context;
            }
        }
    }

    [Fact]
    public async Task StartSession_Creates_Session_And_Event()
    {
        await using var tracker = CreateTracker();
        var sessionId = await tracker.StartSessionAsync("user-1");

        Assert.NotEqual(Guid.Empty, sessionId);

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await db.UserSessions.FindAsync(sessionId);
        Assert.NotNull(session);
        Assert.Equal("user-1", session.UserId);
        Assert.Null(session.DisconnectedAt);

        var events = await db.ActivityEvents.Where(e => e.SessionId == sessionId).ToListAsync();
        Assert.Single(events);
        Assert.Equal(ActivityEventType.SessionStart, events[0].EventType);
    }

    [Fact]
    public async Task TrackPageView_Creates_PageView_Event()
    {
        await using var tracker = CreateTracker();
        await tracker.StartSessionAsync("user-2");

        await tracker.TrackPageViewAsync("/search");

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pageViews = await db.ActivityEvents
            .Where(e => e.EventType == ActivityEventType.PageView)
            .ToListAsync();

        Assert.Single(pageViews);
        Assert.Equal("/search", pageViews[0].PageUrl);
        Assert.Equal("user-2", pageViews[0].UserId);
    }

    [Fact]
    public async Task TrackEvent_Creates_Event_With_DealId_And_Metadata()
    {
        await using var tracker = CreateTracker();
        await tracker.StartSessionAsync("user-3");
        var dealId = Guid.NewGuid();

        await tracker.TrackEventAsync(ActivityEventType.ReportViewed, dealId: dealId, metadata: "test-meta");

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var evt = await db.ActivityEvents
            .FirstAsync(e => e.EventType == ActivityEventType.ReportViewed);

        Assert.Equal(dealId, evt.DealId);
        Assert.Equal("test-meta", evt.Metadata);
    }

    [Fact]
    public async Task TrackPageView_NoOp_Before_StartSession()
    {
        await using var tracker = CreateTracker();
        // Don't call StartSessionAsync

        await tracker.TrackPageViewAsync("/search");

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.ActivityEvents.ToListAsync());
    }

    [Fact]
    public async Task TrackEvent_NoOp_Before_StartSession()
    {
        await using var tracker = CreateTracker();
        // Don't call StartSessionAsync

        await tracker.TrackEventAsync(ActivityEventType.SearchPerformed);

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.ActivityEvents.ToListAsync());
    }

    [Fact]
    public async Task DisposeAsync_Closes_Session()
    {
        Guid sessionId;
        {
            await using var tracker = CreateTracker();
            sessionId = await tracker.StartSessionAsync("user-4");
        } // DisposeAsync called here

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var session = await db.UserSessions.FindAsync(sessionId);
        Assert.NotNull(session);
        Assert.NotNull(session.DisconnectedAt);

        var endEvents = await db.ActivityEvents
            .Where(e => e.SessionId == sessionId && e.EventType == ActivityEventType.SessionEnd)
            .ToListAsync();
        Assert.Single(endEvents);
    }

    [Fact]
    public async Task DisposeAsync_NoOp_When_No_Session()
    {
        // Should not throw
        await using var tracker = CreateTracker();
        // No StartSession, just dispose — should be safe
    }

    [Fact]
    public async Task StartSession_Captures_IpAddress()
    {
        var ip = IPAddress.Parse("192.168.1.42");
        await using var tracker = CreateTracker(remoteIp: ip);
        var sessionId = await tracker.StartSessionAsync("user-ip");

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var evt = await db.ActivityEvents.FirstAsync(e => e.SessionId == sessionId);
        Assert.Equal("192.168.1.42", evt.IpAddress);
    }

    [Fact]
    public async Task TrackEvent_Captures_IpAddress()
    {
        var ip = IPAddress.Parse("10.0.0.1");
        await using var tracker = CreateTracker(remoteIp: ip);
        await tracker.StartSessionAsync("user-ip2");
        await tracker.TrackEventAsync(ActivityEventType.SearchPerformed);

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var evt = await db.ActivityEvents.FirstAsync(e => e.EventType == ActivityEventType.SearchPerformed);
        Assert.Equal("10.0.0.1", evt.IpAddress);
    }

    [Fact]
    public async Task TrackEvent_Handles_Null_HttpContext_Gracefully()
    {
        // No remote IP (simulates background job)
        await using var tracker = CreateTracker(remoteIp: null);
        var sessionId = await tracker.StartSessionAsync("bg-user");

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var evt = await db.ActivityEvents.FirstAsync(e => e.SessionId == sessionId);
        Assert.Null(evt.IpAddress);
    }
}
