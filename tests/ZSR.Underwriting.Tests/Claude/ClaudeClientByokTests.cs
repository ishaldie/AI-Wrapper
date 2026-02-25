using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;

namespace ZSR.Underwriting.Tests.Claude;

public class ClaudeClientByokTests
{
    private static readonly string SuccessResponse = JsonSerializer.Serialize(new
    {
        content = new[] { new { type = "text", text = "OK" } },
        model = "claude-sonnet-4-5-20250514",
        stop_reason = "end_turn",
        usage = new { input_tokens = 10, output_tokens = 5 }
    });

    private static readonly ClaudeOptions DefaultOptions = new()
    {
        ApiKey = "shared-platform-key",
        Model = "claude-opus-4-6-20250918",
        MaxTokens = 4096,
        BaseUrl = "https://api.anthropic.com"
    };

    private static ClaudeClient CreateClient(
        MockHttpMessageHandler handler,
        IApiKeyResolver? resolver = null,
        ClaudeOptions? options = null)
    {
        var opts = options ?? DefaultOptions;
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/")
        };
        return new ClaudeClient(
            httpClient,
            Options.Create(opts),
            NullLogger<ClaudeClient>.Instance,
            resolver);
    }

    [Fact]
    public async Task SendMessageAsync_WithByokUserId_UsesResolvedKey()
    {
        var handler = new MockHttpMessageHandler(SuccessResponse, HttpStatusCode.OK);
        var resolver = new StubApiKeyResolver("sk-ant-api03-byok-key", null, true);
        var client = CreateClient(handler, resolver);

        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System",
            UserMessage = "User",
            UserId = "user-123"
        });

        var request = handler.LastRequest!;
        Assert.Equal("sk-ant-api03-byok-key", request.Headers.GetValues("x-api-key").First());
    }

    [Fact]
    public async Task SendMessageAsync_WithByokPreferredModel_UsesResolvedModel()
    {
        var handler = new MockHttpMessageHandler(SuccessResponse, HttpStatusCode.OK);
        var resolver = new StubApiKeyResolver("sk-ant-api03-byok-key", "claude-haiku-4-5-20251001", true);
        var client = CreateClient(handler, resolver);

        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System",
            UserMessage = "User",
            UserId = "user-123"
        });

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.Equal("claude-haiku-4-5-20251001", json.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task SendMessageAsync_WithoutUserId_UsesSharedKey()
    {
        var handler = new MockHttpMessageHandler(SuccessResponse, HttpStatusCode.OK);
        var resolver = new StubApiKeyResolver("sk-ant-api03-byok-key", null, true);
        var client = CreateClient(handler, resolver);

        // No UserId set
        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System",
            UserMessage = "User"
        });

        var request = handler.LastRequest!;
        Assert.Equal("shared-platform-key", request.Headers.GetValues("x-api-key").First());
    }

    [Fact]
    public async Task SendMessageAsync_NoByokKey_UsesSharedKey()
    {
        var handler = new MockHttpMessageHandler(SuccessResponse, HttpStatusCode.OK);
        var resolver = new StubApiKeyResolver(null, null, false);
        var client = CreateClient(handler, resolver);

        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System",
            UserMessage = "User",
            UserId = "user-123"
        });

        var request = handler.LastRequest!;
        Assert.Equal("shared-platform-key", request.Headers.GetValues("x-api-key").First());
    }

    [Fact]
    public async Task SendMessageAsync_NoResolver_UsesOptionsKey()
    {
        // Backward compatibility: no resolver injected
        var handler = new MockHttpMessageHandler(SuccessResponse, HttpStatusCode.OK);
        var client = CreateClient(handler, resolver: null);

        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System",
            UserMessage = "User",
            UserId = "user-123"
        });

        var request = handler.LastRequest!;
        Assert.Equal("shared-platform-key", request.Headers.GetValues("x-api-key").First());
    }

    [Fact]
    public async Task SendMessageAsync_ByokWithNoPreferredModel_UsesOptionsModel()
    {
        var handler = new MockHttpMessageHandler(SuccessResponse, HttpStatusCode.OK);
        var resolver = new StubApiKeyResolver("sk-ant-api03-byok-key", null, true);
        var client = CreateClient(handler, resolver);

        await client.SendMessageAsync(new ClaudeRequest
        {
            SystemPrompt = "System",
            UserMessage = "User",
            UserId = "user-123"
        });

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.Equal("claude-opus-4-6-20250918", json.RootElement.GetProperty("model").GetString());
    }

    private class StubApiKeyResolver : IApiKeyResolver
    {
        private readonly string? _apiKey;
        private readonly string? _model;
        private readonly bool _isByok;

        public StubApiKeyResolver(string? apiKey, string? model, bool isByok)
        {
            _apiKey = apiKey;
            _model = model;
            _isByok = isByok;
        }

        public Task<ApiKeyResolution> ResolveAsync(string? userId)
        {
            if (userId is null || _apiKey is null)
                return Task.FromResult(new ApiKeyResolution("shared-platform-key", null, false));
            return Task.FromResult(new ApiKeyResolution(_apiKey, _model, _isByok));
        }
    }
}
