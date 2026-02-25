namespace ZSR.Underwriting.Domain.Models;

/// <summary>
/// A single message in a conversation.
/// </summary>
public class ConversationMessage
{
    public string Role { get; init; } = string.Empty; // "user" or "assistant"
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Request to send to the Claude API.
/// </summary>
public class ClaudeRequest
{
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserMessage { get; init; } = string.Empty;
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Full conversation history for multi-turn chats.
    /// When set, UserMessage is ignored and these messages are sent instead.
    /// </summary>
    public List<ConversationMessage>? ConversationHistory { get; init; }

    /// <summary>
    /// Optional user ID for BYOK key resolution. When set, the ClaudeClient
    /// will attempt to use the user's own API key instead of the shared key.
    /// </summary>
    public string? UserId { get; init; }
}

/// <summary>
/// Response received from the Claude API.
/// </summary>
public class ClaudeResponse
{
    public string Content { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public string Model { get; init; } = string.Empty;
    public string? StopReason { get; init; }
}
