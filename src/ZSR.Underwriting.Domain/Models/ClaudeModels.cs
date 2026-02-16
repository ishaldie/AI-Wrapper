namespace ZSR.Underwriting.Domain.Models;

/// <summary>
/// Request to send to the Claude API.
/// </summary>
public class ClaudeRequest
{
    public string SystemPrompt { get; init; } = string.Empty;
    public string UserMessage { get; init; } = string.Empty;
    public int? MaxTokens { get; init; }
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
