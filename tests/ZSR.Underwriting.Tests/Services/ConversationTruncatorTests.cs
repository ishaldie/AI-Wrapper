using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Tests.Services;

public class ConversationTruncatorTests
{
    // --- Truncation by message count ---

    [Fact]
    public void Truncate_UnderMessageLimit_ReturnsAllMessages()
    {
        var messages = CreateMessages(6); // 3 user + 3 assistant = 6

        var result = ConversationTruncator.Truncate(messages, maxMessages: 20, maxTokens: 150_000);

        Assert.Equal(6, result.Count);
    }

    [Fact]
    public void Truncate_OverMessageLimit_KeepsMostRecent()
    {
        var messages = CreateMessages(30); // 15 pairs = 30 messages

        var result = ConversationTruncator.Truncate(messages, maxMessages: 10, maxTokens: 150_000);

        Assert.Equal(10, result.Count);
        // Should keep the last 10 messages (most recent)
        Assert.Equal(messages[20].Content, result[0].Content);
        Assert.Equal(messages[29].Content, result[9].Content);
    }

    [Fact]
    public void Truncate_ExactlyAtMessageLimit_ReturnsAll()
    {
        var messages = CreateMessages(20);

        var result = ConversationTruncator.Truncate(messages, maxMessages: 20, maxTokens: 150_000);

        Assert.Equal(20, result.Count);
    }

    // --- Truncation by token estimate ---

    [Fact]
    public void Truncate_OverTokenLimit_KeepsMostRecentWithinBudget()
    {
        // Each message has ~100 chars = ~25 tokens (chars/4)
        // 40 messages * 25 tokens = ~1000 tokens total
        // With maxTokens = 500, should keep roughly the last 20 messages
        var messages = new List<ConversationMessage>();
        for (int i = 0; i < 40; i++)
        {
            messages.Add(new ConversationMessage
            {
                Role = i % 2 == 0 ? "user" : "assistant",
                Content = new string('x', 100) // ~25 tokens each
            });
        }

        var result = ConversationTruncator.Truncate(messages, maxMessages: 100, maxTokens: 500);

        // Should keep messages that fit within 500 tokens
        Assert.True(result.Count < 40);
        Assert.True(result.Count > 0);
        // Last message should be the most recent
        Assert.Equal(messages[39].Content, result[^1].Content);
    }

    [Fact]
    public void Truncate_SingleLargeMessage_StillReturnsAtLeastOne()
    {
        // One message that exceeds the token limit on its own
        var messages = new List<ConversationMessage>
        {
            new() { Role = "user", Content = new string('x', 800_000) } // ~200K tokens
        };

        var result = ConversationTruncator.Truncate(messages, maxMessages: 20, maxTokens: 150_000);

        // Should still return at least the most recent message even if over budget
        Assert.Single(result);
    }

    // --- Keeps most recent messages ---

    [Fact]
    public void Truncate_KeepsMostRecentMessages_DropsOldest()
    {
        var messages = new List<ConversationMessage>
        {
            new() { Role = "user", Content = "oldest message" },
            new() { Role = "assistant", Content = "old response" },
            new() { Role = "user", Content = "middle message" },
            new() { Role = "assistant", Content = "middle response" },
            new() { Role = "user", Content = "newest message" },
            new() { Role = "assistant", Content = "newest response" }
        };

        var result = ConversationTruncator.Truncate(messages, maxMessages: 4, maxTokens: 150_000);

        Assert.Equal(4, result.Count);
        Assert.Equal("middle message", result[0].Content);
        Assert.Equal("middle response", result[1].Content);
        Assert.Equal("newest message", result[2].Content);
        Assert.Equal("newest response", result[3].Content);
    }

    // --- Empty history ---

    [Fact]
    public void Truncate_EmptyList_ReturnsEmpty()
    {
        var result = ConversationTruncator.Truncate(
            new List<ConversationMessage>(), maxMessages: 20, maxTokens: 150_000);

        Assert.Empty(result);
    }

    [Fact]
    public void Truncate_NullList_ReturnsEmpty()
    {
        var result = ConversationTruncator.Truncate(null, maxMessages: 20, maxTokens: 150_000);

        Assert.Empty(result);
    }

    // --- Token estimation ---

    [Fact]
    public void EstimateTokens_ReturnsCharactersDividedByFour()
    {
        // 400 characters / 4 = 100 tokens
        Assert.Equal(100, ConversationTruncator.EstimateTokens("x".PadRight(400)));
    }

    [Fact]
    public void EstimateTokens_EmptyString_ReturnsZero()
    {
        Assert.Equal(0, ConversationTruncator.EstimateTokens(""));
    }

    // --- Helper ---

    private static List<ConversationMessage> CreateMessages(int count)
    {
        var messages = new List<ConversationMessage>();
        for (int i = 0; i < count; i++)
        {
            messages.Add(new ConversationMessage
            {
                Role = i % 2 == 0 ? "user" : "assistant",
                Content = $"Message {i}"
            });
        }
        return messages;
    }
}
