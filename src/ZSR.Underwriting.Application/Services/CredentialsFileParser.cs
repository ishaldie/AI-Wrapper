using System.Text.Json;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Services;

public static class CredentialsFileParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public static CredentialsFile Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Credentials JSON cannot be null or empty.", nameof(json));

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format.", nameof(json), ex);
        }

        using (doc)
        {
            return ExtractFromDocument(doc);
        }
    }

    public static async Task<CredentialsFile> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON format.", nameof(stream), ex);
        }

        using (doc)
        {
            return ExtractFromDocument(doc);
        }
    }

    private static CredentialsFile ExtractFromDocument(JsonDocument doc)
    {
        var root = doc.RootElement;

        string? apiKey = null;
        string? model = null;
        string? label = null;

        if (root.TryGetProperty("api_key", out var apiKeyProp))
            apiKey = apiKeyProp.GetString();

        if (root.TryGetProperty("model", out var modelProp))
            model = modelProp.GetString();

        if (root.TryGetProperty("label", out var labelProp))
            label = labelProp.GetString();

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Credentials file must contain a non-empty 'api_key' field.");

        return new CredentialsFile(apiKey, model, label);
    }
}
