using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class ActivityEvent
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public string UserId { get; private set; }
    public ActivityEventType EventType { get; private set; }
    public string? PageUrl { get; set; }
    public Guid? DealId { get; set; }
    public string? Metadata { get; set; }
    public DateTime OccurredAt { get; private set; }

    // EF Core parameterless constructor
    private ActivityEvent() { UserId = string.Empty; }

    public ActivityEvent(Guid sessionId, string userId, ActivityEventType eventType)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        Id = Guid.NewGuid();
        SessionId = sessionId;
        UserId = userId;
        EventType = eventType;
        OccurredAt = DateTime.UtcNow;
    }
}
