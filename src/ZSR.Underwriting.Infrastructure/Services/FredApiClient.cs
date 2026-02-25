using System.Text.Json;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Infrastructure.Services;

public class FredApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // FRED series IDs
    private const string CpiSeriesId = "CPIAUCSL"; // CPI All Items
    private const string TreasurySeriesId = "DGS10"; // 10-Year Treasury
    private const string RentIndexSeriesId = "CUUR0000SEHA"; // Rent of Primary Residence

    public FredApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Fetches latest CPI, 10-Year Treasury rate, and rent index from FRED.
    /// </summary>
    public async Task<FredData?> GetFredDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch CPI as the primary indicator — if this fails, return null
            var cpi = await FetchLatestObservation(CpiSeriesId, cancellationToken);
            if (cpi == null)
                return null;

            // Best-effort for others
            var treasury = await FetchLatestObservation(TreasurySeriesId, cancellationToken);
            var rent = await FetchLatestObservation(RentIndexSeriesId, cancellationToken);

            return new FredData
            {
                CpiAllItems = cpi,
                TreasuryRate10Y = treasury,
                RentIndex = rent
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<decimal?> FetchLatestObservation(string seriesId, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"fred/series/observations?series_id={seriesId}&sort_order=desc&limit=1&file_type=json";
            var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<FredApiResponse>(json, JsonOptions);
            var value = data?.Observations?.FirstOrDefault()?.Value;

            if (value != null && decimal.TryParse(value, out var result))
                return result;

            return null;
        }
        catch
        {
            return null;
        }
    }

    private class FredApiResponse
    {
        public List<FredObservation>? Observations { get; set; }
    }

    private class FredObservation
    {
        public string? Date { get; set; }
        public string? Value { get; set; }
    }
}
