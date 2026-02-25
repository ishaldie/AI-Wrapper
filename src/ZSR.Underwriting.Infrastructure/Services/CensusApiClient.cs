using System.Text.Json;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Infrastructure.Services;

public class CensusApiClient
{
    private readonly HttpClient _http;

    public CensusApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Fetches ACS 5-Year data: median HHI, total population, median age by zip code.
    /// Endpoint: /data/{year}/acs/acs5?get=B19013_001E,B01003_001E,B01002_001E&for=zip+code+tabulation+area:{zip}
    /// </summary>
    public async Task<CensusData?> GetCensusDataAsync(string zipCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"data/2023/acs/acs5?get=B19013_001E,B01003_001E,B01002_001E&for=zip+code+tabulation+area:{zipCode}";
            var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var rows = JsonSerializer.Deserialize<string[][]>(json);

            // Response format: [[headers], [values]]
            if (rows == null || rows.Length < 2 || rows[1].Length < 3)
                return null;

            var values = rows[1];
            return new CensusData
            {
                MedianHouseholdIncome = decimal.TryParse(values[0], out var hhi) ? hhi : 0m,
                TotalPopulation = int.TryParse(values[1], out var pop) ? pop : 0,
                MedianAge = decimal.TryParse(values[2], out var age) ? age : 0m,
                ZipCode = zipCode
            };
        }
        catch
        {
            return null;
        }
    }
}
