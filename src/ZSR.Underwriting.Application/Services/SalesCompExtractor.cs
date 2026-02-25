using System.Text;
using System.Text.Json;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Application.Services;

public class SalesCompExtractor : ISalesCompExtractor
{
    private readonly IClaudeClient _claude;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SalesCompExtractor(IClaudeClient claude)
    {
        _claude = claude;
    }

    public async Task<SalesCompResult> ExtractCompsAsync(
        MarketContextDto marketContext,
        string subjectAddress,
        decimal subjectPricePerUnit,
        int subjectUnits,
        CancellationToken cancellationToken = default)
    {
        if (marketContext.ComparableTransactions == null || marketContext.ComparableTransactions.Count == 0)
            return new SalesCompResult();

        var prompt = BuildExtractionPrompt(marketContext.ComparableTransactions, subjectAddress, subjectPricePerUnit, subjectUnits);

        try
        {
            var response = await _claude.SendMessageAsync(new ClaudeRequest
            {
                SystemPrompt = "You are a commercial real estate analyst. Extract structured data from comparable transaction descriptions. Always respond with valid JSON only, no markdown formatting.",
                UserMessage = prompt,
                MaxTokens = 2000
            }, cancellationToken);

            return ParseResponse(response.Content);
        }
        catch
        {
            return new SalesCompResult();
        }
    }

    private static string BuildExtractionPrompt(
        List<MarketDataItem> comps, string subjectAddress, decimal subjectPricePerUnit, int subjectUnits)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Extract structured comparable sales data from the following transaction descriptions.");
        sb.AppendLine($"Subject property: {subjectAddress}, {subjectUnits} units, ${subjectPricePerUnit:N0}/unit.");
        sb.AppendLine();
        sb.AppendLine("Comparable transactions:");
        foreach (var comp in comps)
        {
            sb.AppendLine($"- {comp.Name}: {comp.Description}");
        }
        sb.AppendLine();
        sb.AppendLine("""
            Respond with JSON in this exact format:
            {
              "comps": [
                {
                  "address": "street address",
                  "salePrice": 0,
                  "units": 0,
                  "pricePerUnit": 0,
                  "capRate": 0.0,
                  "saleDate": "YYYY-MM-DD",
                  "distanceMiles": 0.0
                }
              ],
              "adjustments": [
                {
                  "factor": "adjustment factor name",
                  "adjustment": "+/-X%",
                  "rationale": "brief explanation"
                }
              ]
            }
            """);

        return sb.ToString();
    }

    private static SalesCompResult ParseResponse(string content)
    {
        try
        {
            // Strip markdown code fences if present
            var json = content.Trim();
            if (json.StartsWith("```"))
            {
                var firstNewline = json.IndexOf('\n');
                if (firstNewline > 0)
                    json = json[(firstNewline + 1)..];
                if (json.EndsWith("```"))
                    json = json[..^3];
                json = json.Trim();
            }

            var parsed = JsonSerializer.Deserialize<CompExtractionResponse>(json, JsonOptions);
            if (parsed == null)
                return new SalesCompResult();

            var comps = parsed.Comps?.Select(c => new SalesCompRow
            {
                Address = c.Address ?? string.Empty,
                SalePrice = c.SalePrice,
                Units = c.Units,
                PricePerUnit = c.PricePerUnit,
                CapRate = c.CapRate,
                SaleDate = DateTime.TryParse(c.SaleDate, out var d) ? d : DateTime.MinValue,
                DistanceMiles = c.DistanceMiles
            }).ToList() ?? [];

            var adjustments = parsed.Adjustments?.Select(a => new AdjustmentRow
            {
                Factor = a.Factor ?? string.Empty,
                Adjustment = a.Adjustment ?? string.Empty,
                Rationale = a.Rationale ?? string.Empty
            }).ToList() ?? [];

            return new SalesCompResult { Comps = comps, Adjustments = adjustments };
        }
        catch
        {
            return new SalesCompResult();
        }
    }

    private class CompExtractionResponse
    {
        public List<CompDto>? Comps { get; set; }
        public List<AdjustmentDto>? Adjustments { get; set; }
    }

    private class CompDto
    {
        public string? Address { get; set; }
        public decimal SalePrice { get; set; }
        public int Units { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal CapRate { get; set; }
        public string? SaleDate { get; set; }
        public decimal DistanceMiles { get; set; }
    }

    private class AdjustmentDto
    {
        public string? Factor { get; set; }
        public string? Adjustment { get; set; }
        public string? Rationale { get; set; }
    }
}
