using System.Text.Json;
using System.Text.RegularExpressions;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Services;

public static partial class DealUpdateParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [GeneratedRegex(@"```deal-update\s*\n([\s\S]*?)\n\s*```", RegexOptions.Multiline)]
    private static partial Regex DealUpdateBlockRegex();

    public static DealUpdateDto? Parse(string response)
    {
        var match = DealUpdateBlockRegex().Match(response);
        if (!match.Success) return null;

        try
        {
            var json = match.Groups[1].Value.Trim();
            return JsonSerializer.Deserialize<DealUpdateDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string StripBlocks(string response)
    {
        return DealUpdateBlockRegex().Replace(response, "").Trim();
    }
}
