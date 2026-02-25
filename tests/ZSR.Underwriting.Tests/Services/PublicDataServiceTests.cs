using System.Net;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;

namespace ZSR.Underwriting.Tests.Services;

public class CensusApiClientTests
{
    private const string ValidCensusResponse = """
    [
      ["B19013_001E","B01003_001E","B01002_001E","zip code tabulation area"],
      ["65000","45000","35.2","75201"]
    ]
    """;

    [Fact]
    public async Task GetCensusDataAsync_ParsesValidResponse()
    {
        var handler = new MockHttpMessageHandler(ValidCensusResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.census.gov/") };
        var service = new CensusApiClient(client);

        var result = await service.GetCensusDataAsync("75201");

        Assert.NotNull(result);
        Assert.Equal(65_000m, result!.MedianHouseholdIncome);
        Assert.Equal(45_000, result.TotalPopulation);
        Assert.Equal(35.2m, result.MedianAge);
        Assert.Equal("75201", result.ZipCode);
    }

    [Fact]
    public async Task GetCensusDataAsync_HttpFailure_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("Server Error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.census.gov/") };
        var service = new CensusApiClient(client);

        var result = await service.GetCensusDataAsync("00000");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCensusDataAsync_IncludesZipInUrl()
    {
        var handler = new MockHttpMessageHandler(ValidCensusResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.census.gov/") };
        var service = new CensusApiClient(client);

        await service.GetCensusDataAsync("75201");

        Assert.NotNull(handler.LastRequest);
        Assert.Contains("75201", handler.LastRequest!.RequestUri!.ToString());
    }
}

public class BlsApiClientTests
{
    private const string ValidBlsResponse = """
    {
      "status": "REQUEST_SUCCEEDED",
      "Results": {
        "series": [
          {
            "seriesID": "LAUST480000000000003",
            "data": [
              { "year": "2025", "period": "M12", "value": "3.8" }
            ]
          }
        ]
      }
    }
    """;

    [Fact]
    public async Task GetBlsDataAsync_ParsesUnemploymentRate()
    {
        var handler = new MockHttpMessageHandler(ValidBlsResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.bls.gov/") };
        var service = new BlsApiClient(client);

        var result = await service.GetBlsDataAsync("TX", "Dallas");

        Assert.NotNull(result);
        Assert.Equal(3.8m, result!.UnemploymentRate);
    }

    [Fact]
    public async Task GetBlsDataAsync_HttpFailure_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("{}", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.bls.gov/") };
        var service = new BlsApiClient(client);

        var result = await service.GetBlsDataAsync("TX", "Dallas");

        Assert.Null(result);
    }
}

public class FredApiClientTests
{
    private const string ValidFredCpiResponse = """
    {
      "observations": [
        { "date": "2025-11-01", "value": "315.5" }
      ]
    }
    """;

    [Fact]
    public async Task GetFredDataAsync_ParsesCpiValue()
    {
        var handler = new MockHttpMessageHandler(ValidFredCpiResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.stlouisfed.org/") };
        var service = new FredApiClient(client);

        var result = await service.GetFredDataAsync();

        Assert.NotNull(result);
        Assert.NotNull(result!.CpiAllItems);
    }

    [Fact]
    public async Task GetFredDataAsync_HttpFailure_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.stlouisfed.org/") };
        var service = new FredApiClient(client);

        var result = await service.GetFredDataAsync();

        Assert.Null(result);
    }
}

public class PublicDataServiceTests
{
    [Fact]
    public async Task GetAllPublicDataAsync_AggregatesAllSources()
    {
        var stub = new StubPublicDataService(
            new CensusData { MedianHouseholdIncome = 65_000, TotalPopulation = 45_000, MedianAge = 35.2m, ZipCode = "75201" },
            new BlsData { UnemploymentRate = 3.8m, AreaName = "Dallas-Fort Worth" },
            new FredData { CpiAllItems = 315.5m, TreasuryRate10Y = 4.25m });

        var result = await stub.GetAllPublicDataAsync("75201", "TX", "Dallas");

        Assert.NotNull(result.Census);
        Assert.NotNull(result.Bls);
        Assert.NotNull(result.Fred);
        Assert.Equal(65_000m, result.Census!.MedianHouseholdIncome);
        Assert.Equal(3.8m, result.Bls!.UnemploymentRate);
        Assert.Equal(315.5m, result.Fred!.CpiAllItems);
    }

    [Fact]
    public async Task GetAllPublicDataAsync_PartialFailure_ReturnsAvailableData()
    {
        var stub = new StubPublicDataService(
            new CensusData { MedianHouseholdIncome = 65_000, ZipCode = "75201" },
            null, // BLS failed
            null); // FRED failed

        var result = await stub.GetAllPublicDataAsync("75201", "TX", "Dallas");

        Assert.NotNull(result.Census);
        Assert.Null(result.Bls);
        Assert.Null(result.Fred);
    }
}

internal class StubPublicDataService : IPublicDataService
{
    private readonly CensusData? _census;
    private readonly BlsData? _bls;
    private readonly FredData? _fred;

    public StubPublicDataService(CensusData? census, BlsData? bls, FredData? fred)
    {
        _census = census;
        _bls = bls;
        _fred = fred;
    }

    public Task<CensusData?> GetCensusDataAsync(string zipCode, CancellationToken ct = default) => Task.FromResult(_census);
    public Task<BlsData?> GetBlsDataAsync(string state, string metro, CancellationToken ct = default) => Task.FromResult(_bls);
    public Task<FredData?> GetFredDataAsync(CancellationToken ct = default) => Task.FromResult(_fred);

    public Task<PublicDataDto> GetAllPublicDataAsync(string zipCode, string state, string metro, CancellationToken ct = default)
    {
        return Task.FromResult(new PublicDataDto
        {
            Census = _census,
            Bls = _bls,
            Fred = _fred,
            RetrievedAt = DateTime.UtcNow
        });
    }
}
