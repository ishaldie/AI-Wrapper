using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IActivityTracker
{
    Task<Guid> StartSessionAsync(string userId);
    Task TrackPageViewAsync(string pageUrl);
    Task TrackEventAsync(ActivityEventType eventType, Guid? dealId = null, string? metadata = null);
}
