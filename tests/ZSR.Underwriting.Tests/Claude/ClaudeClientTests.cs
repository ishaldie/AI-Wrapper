using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;

namespace ZSR.Underwriting.Tests.Claude;

public class ClaudeClientTests
{
    private static readonly ClaudeOptions DefaultOptions = new()
    {
        ApiKey = "test-api-key",
        Model = "claude-sonnet-4-5-20250929",
        MaxTokens = 4096,
        BaseUrl = "https://api.anthropic.com"
    };

    private static ClaudeClient CreateClient(MockHttpMessageHandler handler, ClaudeOptions? options = null)
    {
        var opts = options ?? DefaultOptions;
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/")
        };
        return new ClaudeClient(
            httpClient,
            Options.Create(opts),
            NullLogger<ClaudeClient>.Instance);
    }

    // --- SendMessageAsync: Success ---

    [Fact]
    public async Task SendMessageAsync_Success_ReturnsContent()
    {
        var apiResponse = new
        {
            id = "msg_test123",
            type = "message",
            role = "assistant",
            content = new[] { new { type = "text", text = "Generated prose content." } },
            model = "claude-sonnet-4-5-20250929",
            stop_reason = "end_turn",
            usage = new { input_tokens = 150, output_tokens = 50 }
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(apiResponse), HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "You are an underwriting analyst.",
            UserMessage = "Write an executive summary."
        });

        Assert.Equal("Generated prose content.", result.Content);
        Assert.Equal("claude-sonnet-4-5-20250929", result.Model);
        Assert.Equal("end_turn", result.StopReason);
        Assert.Equal(150, result.InputTokens);
        Assert.Equal(50, result.OutputTokens);
    }

    // --- Request structure ---

    [Fact]
    public async Task SendMessageAsync_SendsCorrectApiEndpoint()
    {
        var apiResponse = new
        {
            content = new[] { new { type = "text", text = "OK" } },
            model = "claude-sonnet-4-5-20250929",
            stop_reason = "end_turn",
            usage = new { input_tokens = 10, output_tokens = 5 }
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(apiResponse), HttpStatusCode.OK);
        var client = CreateClient(handler);

        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System prompt.",
            UserMessage = "User message."
        });

        Assert.NotNull(handler.LastRequest);
        var url = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("v1/messages", url);
    }

    [Fact]
    public async Task SendMessageAsync_SendsRequiredHeaders()
    {
        var apiResponse = new
        {
            content = new[] { new { type = "text", text = "OK" } },
            model = "claude-sonnet-4-5-20250929",
            stop_reason = "end_turn",
            usage = new { input_tokens = 10, output_tokens = 5 }
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(apiResponse), HttpStatusCode.OK);
        var client = CreateClient(handler);

        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System",
            UserMessage = "User"
        });

        var request = handler.LastRequest!;
        Assert.True(request.Headers.Contains("x-api-key"));
        Assert.Equal("test-api-key", request.Headers.GetValues("x-api-key").First());
        Assert.True(request.Headers.Contains("anthropic-version"));
    }

    [Fact]
    public async Task SendMessageAsync_SendsCorrectRequestBody()
    {
        var apiResponse = new
        {
            content = new[] { new { type = "text", text = "OK" } },
            model = "claude-sonnet-4-5-20250929",
            stop_reason = "end_turn",
            usage = new { input_tokens = 10, output_tokens = 5 }
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(apiResponse), HttpStatusCode.OK);
        var client = CreateClient(handler);

        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "You are an analyst.",
            UserMessage = "Analyze this deal.",
            MaxTokens = 2048
        });

        var request = handler.LastRequest!;
        var body = await request.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.Equal("claude-sonnet-4-5-20250929", root.GetProperty("model").GetString());
        Assert.Equal(2048, root.GetProperty("max_tokens").GetInt32());
        Assert.Equal("You are an analyst.", root.GetProperty("system").GetString());

        var messages = root.GetProperty("messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.Equal("Analyze this deal.", messages[0].GetProperty("content").GetString());
    }

    [Fact]
    public async Task SendMessageAsync_UsesDefaultMaxTokensFromOptions()
    {
        var apiResponse = new
        {
            content = new[] { new { type = "text", text = "OK" } },
            model = "claude-sonnet-4-5-20250929",
            stop_reason = "end_turn",
            usage = new { input_tokens = 10, output_tokens = 5 }
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(apiResponse), HttpStatusCode.OK);
        var client = CreateClient(handler);

        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System",
            UserMessage = "User"
            // MaxTokens not set — should use ClaudeOptions.MaxTokens (4096)
        });

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.Equal(4096, json.RootElement.GetProperty("max_tokens").GetInt32());
    }

    // --- Error handling ---

    [Fact]
    public async Task SendMessageAsync_ApiReturns401_ThrowsHttpRequestException()
    {
        var handler = new MockHttpMessageHandler(
            """{"type":"error","error":{"type":"authentication_error","message":"Invalid API key"}}""",
            HttpStatusCode.Unauthorized);
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SendMessageAsync(new ClaudeRequest
            {
                SystemPrompt = "System",
                UserMessage = "User"
            }));
    }

    [Fact]
    public async Task SendMessageAsync_ApiReturns500_ThrowsHttpRequestException()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SendMessageAsync(new ClaudeRequest
            {
                SystemPrompt = "System",
                UserMessage = "User"
            }));
    }

    [Fact]
    public async Task SendMessageAsync_InvalidJsonResponse_ThrowsJsonException()
    {
        var handler = new MockHttpMessageHandler("not-valid-json!!!", HttpStatusCode.OK);
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<JsonException>(() =>
            client.SendMessageAsync(new ClaudeRequest
            {
                SystemPrompt = "System",
                UserMessage = "User"
            }));
    }

    // --- Multiple text blocks ---

    [Fact]
    public async Task SendMessageAsync_MultipleContentBlocks_ConcatenatesText()
    {
        var apiResponse = new
        {
            content = new[]
            {
                new { type = "text", text = "First paragraph. " },
                new { type = "text", text = "Second paragraph." }
            },
            model = "claude-sonnet-4-5-20250929",
            stop_reason = "end_turn",
            usage = new { input_tokens = 20, output_tokens = 15 }
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(apiResponse), HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System",
            UserMessage = "User"
        });

        Assert.Equal("First paragraph. Second paragraph.", result.Content);
    }
}
