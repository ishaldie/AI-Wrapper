namespace ZSR.Underwriting.Domain.Entities;

public class AuthorizedSender
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string Email { get; private set; }
    public string? Label { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core parameterless constructor
    private AuthorizedSender()
    {
        UserId = string.Empty;
        Email = string.Empty;
    }

    public AuthorizedSender(string userId, string email, string? label = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        Id = Guid.NewGuid();
        UserId = userId;
        Email = email.Trim().ToLowerInvariant();
        Label = label;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateLabel(string? label)
    {
        Label = label;
    }
}
