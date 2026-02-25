using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Domain.Exceptions;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;

namespace ZSR.Underwriting.Tests.Claude;

public class ClaudeRateLimitTests
{
    private static readonly ClaudeOptions DefaultOptions = new()
    {
        ApiKey = "test-api-key",
        Model = "claude-sonnet-4-5-20250929",
        MaxTokens = 4096,
        BaseUrl = "https://api.anthropic.com"
    };

    private static ClaudeClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(DefaultOptions.BaseUrl.TrimEnd('/') + "/")
        };
        return new ClaudeClient(
            httpClient,
            Options.Create(DefaultOptions),
            NullLogger<ClaudeClient>.Instance);
    }

    [Fact]
    public async Task SendMessageAsync_429Response_ThrowsClaudeRateLimitException()
    {
        var handler = new MockHttpMessageHandler(
            """{"type":"error","error":{"type":"rate_limit_error","message":"Rate limit exceeded"}}""",
            HttpStatusCode.TooManyRequests);
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ClaudeRateLimitException>(() =>
            client.SendMessageAsync(new ClaudeRequest
            {
                SystemPrompt = "System",
                UserMessage = "User"
            }));
    }

    [Fact]
    public async Task SendMessageAsync_429WithRetryAfterHeader_ParsesRetryAfterSeconds()
    {
        var handler = new MockHttpMessageHandler(
            """{"type":"error","error":{"type":"rate_limit_error","message":"Rate limit exceeded"}}""",
            HttpStatusCode.TooManyRequests)
            .WithResponseHeader("retry-after", "30");
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<ClaudeRateLimitException>(() =>
            client.SendMessageAsync(new ClaudeRequest
            {
                SystemPrompt = "System",
                UserMessage = "User"
            }));

        Assert.Equal(30, ex.RetryAfterSeconds);
    }

    [Fact]
    public async Task SendMessageAsync_429WithoutRetryAfterHeader_RetryAfterIsNull()
    {
        var handler = new MockHttpMessageHandler(
            """{"type":"error","error":{"type":"rate_limit_error","message":"Rate limit exceeded"}}""",
            HttpStatusCode.TooManyRequests);
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<ClaudeRateLimitException>(() =>
            client.SendMessageAsync(new ClaudeRequest
            {
                SystemPrompt = "System",
                UserMessage = "User"
            }));

        Assert.Null(ex.RetryAfterSeconds);
    }

    [Fact]
    public async Task SendMessageAsync_500Response_ThrowsHttpRequestException_Not429Exception()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        // 500 should throw HttpRequestException, NOT ClaudeRateLimitException
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SendMessageAsync(new ClaudeRequest
            {
                SystemPrompt = "System",
                UserMessage = "User"
            }));

        Assert.IsNotType<ClaudeRateLimitException>(ex);
    }

    [Fact]
    public async Task ClaudeRateLimitException_MessageContainsRetryInfo()
    {
        var ex = new ClaudeRateLimitException(45);
        Assert.Contains("45 seconds", ex.Message);

        var exNoRetry = new ClaudeRateLimitException(null);
        Assert.Contains("rate limit", exNoRetry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("seconds", exNoRetry.Message);
    }
}
