namespace ZSR.Underwriting.Domain.Entities;

public class UserSession
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public DateTime ConnectedAt { get; private set; }
    public DateTime? DisconnectedAt { get; private set; }
    public ICollection<ActivityEvent> ActivityEvents { get; set; } = new List<ActivityEvent>();

    // EF Core parameterless constructor
    private UserSession() { UserId = string.Empty; }

    public UserSession(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        Id = Guid.NewGuid();
        UserId = userId;
        ConnectedAt = DateTime.UtcNow;
    }

    public void MarkDisconnected()
    {
        DisconnectedAt = DateTime.UtcNow;
    }
}
