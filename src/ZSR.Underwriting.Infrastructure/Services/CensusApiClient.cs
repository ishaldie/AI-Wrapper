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

    /// <summary>
    /// Fetches ACS tenant demographics: median HHI, median gross rent, avg household size,
    /// rent burden (>=30% HHI), renter-occupied units.
    /// Variables: B19013_001E, B25064_001E, B25010_001E, B25070_010E, B25003_003E
    /// </summary>
    public async Task<TenantDemographicsDto?> GetTenantDemographicsAsync(string zipCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"data/2023/acs/acs5?get=B19013_001E,B25064_001E,B25010_001E,B25070_010E,B25003_003E&for=zip+code+tabulation+area:{zipCode}";
            var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var rows = JsonSerializer.Deserialize<string[][]>(json);

            if (rows == null || rows.Length < 2 || rows[1].Length < 5)
                return null;

            var v = rows[1];
            var rentBurdenHouseholds = decimal.TryParse(v[3], out var rb) ? rb : 0m;
            var renterUnits = int.TryParse(v[4], out var ru) ? ru : 0;
            var rentBurdenPercent = renterUnits > 0 ? Math.Round(rentBurdenHouseholds / renterUnits * 100m, 0) : 0m;

            return new TenantDemographicsDto
            {
                MedianHouseholdIncome = decimal.TryParse(v[0], out var hhi) ? hhi : 0m,
                MedianGrossRent = decimal.TryParse(v[1], out var rent) ? rent : 0m,
                AverageHouseholdSize = decimal.TryParse(v[2], out var hs) ? hs : 0m,
                RentBurdenPercent = rentBurdenPercent,
                RenterOccupiedUnits = renterUnits,
                ZipCode = zipCode
            };
        }
        catch
        {
            return null;
        }
    }
}
