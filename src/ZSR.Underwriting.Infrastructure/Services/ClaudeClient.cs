using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Domain.Exceptions;
using ZSR.Underwriting.Infrastructure.Configuration;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ClaudeClient : IClaudeClient
{
    private readonly HttpClient _httpClient;
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeClient> _logger;
    private readonly IApiKeyResolver? _apiKeyResolver;

    private const string ApiVersion = "2023-06-01";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClaudeClient(
        HttpClient httpClient,
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeClient> logger,
        IApiKeyResolver? apiKeyResolver = null)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _apiKeyResolver = apiKeyResolver;
    }

    public async Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken ct = default)
    {
        // Resolve API key: BYOK per-request if resolver available, else shared key
        string apiKey;
        string model;
        if (_apiKeyResolver is not null && request.UserId is not null)
        {
            var resolution = await _apiKeyResolver.ResolveAsync(request.UserId);
            apiKey = resolution.ApiKey;
            model = resolution.Model ?? _options.Model;
        }
        else
        {
            apiKey = _options.ResolvedApiKey;
            model = _options.Model;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Claude API key is not configured. Set Claude:ApiKey in appsettings, user secrets, or the ANTHROPIC_API_KEY environment variable.");

        var maxTokens = request.MaxTokens ?? _options.MaxTokens;

        var messages = new List<ApiMessage>();

        // Map conversation history into alternating user/assistant messages
        if (request.ConversationHistory is { Count: > 0 })
        {
            foreach (var msg in request.ConversationHistory)
            {
                messages.Add(new ApiMessage { Role = msg.Role, Content = msg.Content });
            }
        }

        // Append current user message as the final entry
        messages.Add(new ApiMessage { Role = "user", Content = request.UserMessage });

        var payload = new ApiRequest
        {
            Model = model,
            MaxTokens = maxTokens,
            System = request.SystemPrompt,
            Messages = messages
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = content
        };
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", ApiVersion);

        _logger.LogInformation("Claude API call: POST v1/messages (model={Model}, max_tokens={MaxTokens})",
            model, maxTokens);

        var response = await _httpClient.SendAsync(httpRequest, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            int? retryAfter = null;
            if (response.Headers.RetryAfter?.Delta is { } delta)
                retryAfter = (int)delta.TotalSeconds;
            else if (response.Headers.TryGetValues("retry-after", out var values) &&
                     int.TryParse(values.FirstOrDefault(), out var seconds))
                retryAfter = seconds;

            throw new ClaudeRateLimitException(retryAfter);
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseBody, JsonOptions)
            ?? throw new JsonException("Failed to deserialize Claude API response.");

        var text = string.Concat(
            apiResponse.Content
                .Where(c => c.Type == "text")
                .Select(c => c.Text));

        _logger.LogInformation(
            "Claude API success: input_tokens={InputTokens}, output_tokens={OutputTokens}, stop_reason={StopReason}",
            apiResponse.Usage?.InputTokens ?? 0,
            apiResponse.Usage?.OutputTokens ?? 0,
            apiResponse.StopReason);

        return new ClaudeResponse
        {
            Content = text,
            Model = apiResponse.Model ?? string.Empty,
            StopReason = apiResponse.StopReason,
            InputTokens = apiResponse.Usage?.InputTokens ?? 0,
            OutputTokens = apiResponse.Usage?.OutputTokens ?? 0
        };
    }

    // --- Internal API DTOs ---

    private class ApiRequest
    {
        public string Model { get; init; } = string.Empty;
        public int MaxTokens { get; init; }
        public string? System { get; init; }
        public List<ApiMessage> Messages { get; init; } = [];
    }

    private class ApiMessage
    {
        public string Role { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
    }

    private class ApiResponse
    {
        public string? Id { get; init; }
        public string? Model { get; init; }
        public string? StopReason { get; init; }
        public List<ContentBlock> Content { get; init; } = [];
        public UsageInfo? Usage { get; init; }
    }

    private class ContentBlock
    {
        public string Type { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
    }

    private class UsageInfo
    {
        public int InputTokens { get; init; }
        public int OutputTokens { get; init; }
    }
}
