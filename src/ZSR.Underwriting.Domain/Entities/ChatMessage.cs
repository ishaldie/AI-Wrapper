namespace ZSR.Underwriting.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid DealId { get; private set; }
    public string Role { get; private set; } // "user" or "assistant"
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }

    // Navigation
    public Deal Deal { get; set; } = null!;

    private ChatMessage() { Role = string.Empty; Content = string.Empty; }

    public ChatMessage(Guid dealId, string role, string content)
    {
        Id = Guid.NewGuid();
        DealId = dealId;
        Role = role;
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }
}
