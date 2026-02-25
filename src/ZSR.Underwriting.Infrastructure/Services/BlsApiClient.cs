using System.Text.Json;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Infrastructure.Services;

public class BlsApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BlsApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Fetches the latest unemployment rate from BLS for a given state/metro area.
    /// Uses the Local Area Unemployment Statistics (LAUS) series.
    /// </summary>
    public async Task<BlsData?> GetBlsDataAsync(string state, string metro, CancellationToken cancellationToken = default)
    {
        try
        {
            var stateCode = GetStateFipsCode(state);
            var seriesId = $"LAUST{stateCode}0000000000003"; // Statewide unemployment rate
            var url = $"publicAPI/v2/timeseries/data/{seriesId}?latest=true";

            var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var blsResponse = JsonSerializer.Deserialize<BlsApiResponse>(json, JsonOptions);

            var latestValue = blsResponse?.Results?.Series?.FirstOrDefault()?.Data?.FirstOrDefault()?.Value;
            if (latestValue == null || !decimal.TryParse(latestValue, out var rate))
                return null;

            return new BlsData
            {
                UnemploymentRate = rate,
                AreaName = $"{metro}, {state}"
            };
        }
        catch
        {
            return null;
        }
    }

    private static string GetStateFipsCode(string state)
    {
        // Common state FIPS codes for multifamily markets
        return state.ToUpperInvariant() switch
        {
            "AL" => "01", "AZ" => "04", "CA" => "06", "CO" => "08",
            "CT" => "09", "FL" => "12", "GA" => "13", "IL" => "17",
            "IN" => "18", "KY" => "21", "LA" => "22", "MD" => "24",
            "MA" => "25", "MI" => "26", "MN" => "27", "MO" => "29",
            "NJ" => "34", "NY" => "36", "NC" => "37", "OH" => "39",
            "OK" => "40", "OR" => "41", "PA" => "42", "SC" => "45",
            "TN" => "47", "TX" => "48", "UT" => "49", "VA" => "51",
            "WA" => "53", "WI" => "55", "DC" => "11",
            _ => "00"
        };
    }

    // BLS API response shape
    private class BlsApiResponse
    {
        public string? Status { get; set; }
        public BlsResults? Results { get; set; }
    }

    private class BlsResults
    {
        public List<BlsSeries>? Series { get; set; }
    }

    private class BlsSeries
    {
        public string? SeriesID { get; set; }
        public List<BlsDataPoint>? Data { get; set; }
    }

    private class BlsDataPoint
    {
        public string? Year { get; set; }
        public string? Period { get; set; }
        public string? Value { get; set; }
    }
}
