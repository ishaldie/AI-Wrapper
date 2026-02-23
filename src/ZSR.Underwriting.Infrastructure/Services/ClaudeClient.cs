using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Infrastructure.Configuration;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ClaudeClient : IClaudeClient
{
    private readonly HttpClient _httpClient;
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeClient> _logger;

    private const string ApiVersion = "2023-06-01";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClaudeClient(
        HttpClient httpClient,
        IOptions<ClaudeOptions> options,
        ILogger<ClaudeClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken ct = default)
    {
        var apiKey = _options.ResolvedApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Claude API key is not configured. Set Claude:ApiKey in appsettings, user secrets, or the ANTHROPIC_API_KEY environment variable.");

        var maxTokens = request.MaxTokens ?? _options.MaxTokens;

        var payload = new ApiRequest
        {
            Model = _options.Model,
            MaxTokens = maxTokens,
            System = request.SystemPrompt,
            Messages =
            [
                new ApiMessage { Role = "user", Content = request.UserMessage }
            ]
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
            _options.Model, maxTokens);

        var response = await _httpClient.SendAsync(httpRequest, ct);
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
