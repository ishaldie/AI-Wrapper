using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ActivityTracker : IActivityTracker, IAsyncDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActivityTracker> _logger;
    private Guid _sessionId;
    private string? _userId;

    public ActivityTracker(IServiceScopeFactory scopeFactory, ILogger<ActivityTracker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Guid> StartSessionAsync(string userId)
    {
        try
        {
            _userId = userId;
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = new UserSession(userId);
            _sessionId = session.Id;

            db.UserSessions.Add(session);

            var startEvent = new ActivityEvent(_sessionId, userId, ActivityEventType.SessionStart);
            db.ActivityEvents.Add(startEvent);

            await db.SaveChangesAsync();
            return _sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start tracking session for user {UserId}", userId);
            return Guid.Empty;
        }
    }

    public async Task TrackPageViewAsync(string pageUrl)
    {
        if (_sessionId == Guid.Empty || _userId is null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var evt = new ActivityEvent(_sessionId, _userId, ActivityEventType.PageView)
            {
                PageUrl = pageUrl
            };
            db.ActivityEvents.Add(evt);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track page view for {PageUrl}", pageUrl);
        }
    }

    public async Task TrackEventAsync(ActivityEventType eventType, Guid? dealId = null, string? metadata = null)
    {
        if (_sessionId == Guid.Empty || _userId is null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var evt = new ActivityEvent(_sessionId, _userId, eventType)
            {
                DealId = dealId,
                Metadata = metadata
            };
            db.ActivityEvents.Add(evt);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to track event {EventType}", eventType);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_sessionId == Guid.Empty || _userId is null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = await db.UserSessions.FindAsync(_sessionId);
            if (session is not null)
            {
                session.MarkDisconnected();
            }

            var endEvent = new ActivityEvent(_sessionId, _userId, ActivityEventType.SessionEnd);
            db.ActivityEvents.Add(endEvent);

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to close tracking session {SessionId}", _sessionId);
        }
    }
}
