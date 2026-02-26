using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Infrastructure.Services;

/// <summary>
/// Calls the HUD User Income Limits (IL) API to fetch Area Median Income data.
/// Endpoint: GET il/statedata/{stateCode}
/// Auth: Bearer token via Authorization header (configured on HttpClient).
/// </summary>
public class HudApiClient : IHudApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<HudApiClient> _logger;

    public HudApiClient(HttpClient http, ILogger<HudApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<HudIncomeLimitsDto?> GetIncomeLimitsAsync(
        string stateCode,
        string? countyOrMetro = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fips = GetStateFipsCode(stateCode);
            var url = $"il/statedata/{fips}";
            var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var entries = JsonSerializer.Deserialize<HudIlEntry[]>(json, JsonOptions);
            if (entries == null || entries.Length == 0)
                return null;

            // Try to match county/metro name; fall back to first entry
            var match = entries[0];
            if (!string.IsNullOrWhiteSpace(countyOrMetro))
            {
                var found = entries.FirstOrDefault(e =>
                    (e.County_Name?.Contains(countyOrMetro, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.Town_Name?.Contains(countyOrMetro, StringComparison.OrdinalIgnoreCase) ?? false));
                if (found != null)
                    match = found;
            }

            return MapToDto(match);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch HUD income limits for {StateCode}", stateCode);
            return null;
        }
    }

    private static HudIncomeLimitsDto MapToDto(HudIlEntry e)
    {
        return new HudIncomeLimitsDto
        {
            MedianFamilyIncome = ParseDecimal(e.Median2025),
            AreaName = e.County_Name ?? e.Town_Name ?? "Unknown",
            FipsCode = e.Counties_Msa ?? string.Empty,
            Year = 2025,
            ExtremelyLow = new HudIncomeLevel
            {
                Person1 = ParseInt(e.Extremely_Low_30_1),
                Person2 = ParseInt(e.Extremely_Low_30_2),
                Person3 = ParseInt(e.Extremely_Low_30_3),
                Person4 = ParseInt(e.Extremely_Low_30_4),
                Person5 = ParseInt(e.Extremely_Low_30_5),
                Person6 = ParseInt(e.Extremely_Low_30_6),
                Person7 = ParseInt(e.Extremely_Low_30_7),
                Person8 = ParseInt(e.Extremely_Low_30_8)
            },
            VeryLow = new HudIncomeLevel
            {
                Person1 = ParseInt(e.Very_Low_50_1),
                Person2 = ParseInt(e.Very_Low_50_2),
                Person3 = ParseInt(e.Very_Low_50_3),
                Person4 = ParseInt(e.Very_Low_50_4),
                Person5 = ParseInt(e.Very_Low_50_5),
                Person6 = ParseInt(e.Very_Low_50_6),
                Person7 = ParseInt(e.Very_Low_50_7),
                Person8 = ParseInt(e.Very_Low_50_8)
            },
            Low = new HudIncomeLevel
            {
                Person1 = ParseInt(e.Low_80_1),
                Person2 = ParseInt(e.Low_80_2),
                Person3 = ParseInt(e.Low_80_3),
                Person4 = ParseInt(e.Low_80_4),
                Person5 = ParseInt(e.Low_80_5),
                Person6 = ParseInt(e.Low_80_6),
                Person7 = ParseInt(e.Low_80_7),
                Person8 = ParseInt(e.Low_80_8)
            }
        };
    }

    private static int ParseInt(string? value)
        => int.TryParse(value, out var result) ? result : 0;

    private static decimal ParseDecimal(string? value)
        => decimal.TryParse(value, out var result) ? result : 0m;

    private static string GetStateFipsCode(string state)
    {
        return state.ToUpperInvariant() switch
        {
            "AL" => "01", "AK" => "02", "AZ" => "04", "AR" => "05",
            "CA" => "06", "CO" => "08", "CT" => "09", "DE" => "10",
            "DC" => "11", "FL" => "12", "GA" => "13", "HI" => "15",
            "ID" => "16", "IL" => "17", "IN" => "18", "IA" => "19",
            "KS" => "20", "KY" => "21", "LA" => "22", "ME" => "23",
            "MD" => "24", "MA" => "25", "MI" => "26", "MN" => "27",
            "MS" => "28", "MO" => "29", "MT" => "30", "NE" => "31",
            "NV" => "32", "NH" => "33", "NJ" => "34", "NM" => "35",
            "NY" => "36", "NC" => "37", "ND" => "38", "OH" => "39",
            "OK" => "40", "OR" => "41", "PA" => "42", "RI" => "44",
            "SC" => "45", "SD" => "46", "TN" => "47", "TX" => "48",
            "UT" => "49", "VT" => "50", "VA" => "51", "WA" => "53",
            "WV" => "54", "WI" => "55", "WY" => "56",
            _ => state // Pass through if already a FIPS code
        };
    }

    // HUD IL API response shape — one entry per county/metro area in a state
    private class HudIlEntry
    {
        public string? County_Name { get; set; }
        public string? Counties_Msa { get; set; }
        public string? Town_Name { get; set; }
        public string? Metro_Status { get; set; }
        public string? Median2025 { get; set; }
        // 50% AMI (Very Low)
        public string? Very_Low_50_1 { get; set; }
        public string? Very_Low_50_2 { get; set; }
        public string? Very_Low_50_3 { get; set; }
        public string? Very_Low_50_4 { get; set; }
        public string? Very_Low_50_5 { get; set; }
        public string? Very_Low_50_6 { get; set; }
        public string? Very_Low_50_7 { get; set; }
        public string? Very_Low_50_8 { get; set; }
        // 30% AMI (Extremely Low)
        public string? Extremely_Low_30_1 { get; set; }
        public string? Extremely_Low_30_2 { get; set; }
        public string? Extremely_Low_30_3 { get; set; }
        public string? Extremely_Low_30_4 { get; set; }
        public string? Extremely_Low_30_5 { get; set; }
        public string? Extremely_Low_30_6 { get; set; }
        public string? Extremely_Low_30_7 { get; set; }
        public string? Extremely_Low_30_8 { get; set; }
        // 80% AMI (Low)
        public string? Low_80_1 { get; set; }
        public string? Low_80_2 { get; set; }
        public string? Low_80_3 { get; set; }
        public string? Low_80_4 { get; set; }
        public string? Low_80_5 { get; set; }
        public string? Low_80_6 { get; set; }
        public string? Low_80_7 { get; set; }
        public string? Low_80_8 { get; set; }
    }
}
