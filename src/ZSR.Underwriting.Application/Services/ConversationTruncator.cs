using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Application.Services;

/// <summary>
/// Truncates conversation history to stay within message count and token limits.
/// Keeps the most recent messages, dropping oldest first.
/// </summary>
public static class ConversationTruncator
{
    /// <summary>
    /// Truncate conversation history to fit within the given constraints.
    /// Returns a new list containing the most recent messages that fit.
    /// </summary>
    public static List<ConversationMessage> Truncate(
        List<ConversationMessage>? messages,
        int maxMessages,
        int maxTokens)
    {
        if (messages is not { Count: > 0 })
            return new List<ConversationMessage>();

        // First: cap by message count (take from the end)
        var capped = messages.Count <= maxMessages
            ? messages
            : messages.Skip(messages.Count - maxMessages).ToList();

        // Second: cap by token estimate (walk backwards, accumulating tokens)
        var totalTokens = 0;
        var startIndex = capped.Count; // will walk backwards

        for (int i = capped.Count - 1; i >= 0; i--)
        {
            var msgTokens = EstimateTokens(capped[i].Content);
            if (totalTokens + msgTokens > maxTokens && startIndex < capped.Count)
                break; // would exceed budget, stop (but always include at least one)

            totalTokens += msgTokens;
            startIndex = i;
        }

        return capped.Skip(startIndex).ToList();
    }

    /// <summary>
    /// Rough token estimate: characters / 4.
    /// </summary>
    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return text.Length / 4;
    }
}
