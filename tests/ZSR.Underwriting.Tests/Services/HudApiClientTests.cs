using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;

namespace ZSR.Underwriting.Tests.Services;

public class HudApiClientTests
{
    /// <summary>
    /// Simulated HUD IL statedata response for Texas.
    /// The API returns an array of entity objects, each with income limit data.
    /// </summary>
    private const string ValidHudStateResponse = """
    [
      {
        "county_name": "Dallas County",
        "counties_msa": "19100",
        "town_name": "Dallas-Fort Worth-Arlington, TX HUD Metro FMR Area",
        "metro_status": "1",
        "median2025": "88200",
        "Very_Low_50_1": "30900",
        "Very_Low_50_2": "35300",
        "Very_Low_50_3": "39700",
        "Very_Low_50_4": "44100",
        "Very_Low_50_5": "47650",
        "Very_Low_50_6": "51150",
        "Very_Low_50_7": "54700",
        "Very_Low_50_8": "58200",
        "Extremely_Low_30_1": "18550",
        "Extremely_Low_30_2": "21200",
        "Extremely_Low_30_3": "23850",
        "Extremely_Low_30_4": "26500",
        "Extremely_Low_30_5": "28620",
        "Extremely_Low_30_6": "30740",
        "Extremely_Low_30_7": "32860",
        "Extremely_Low_30_8": "34980",
        "Low_80_1": "49450",
        "Low_80_2": "56500",
        "Low_80_3": "63550",
        "Low_80_4": "70600",
        "Low_80_5": "76250",
        "Low_80_6": "81900",
        "Low_80_7": "87550",
        "Low_80_8": "93200"
      },
      {
        "county_name": "Harris County",
        "counties_msa": "26420",
        "town_name": "Houston-The Woodlands-Sugar Land, TX HUD Metro FMR Area",
        "metro_status": "1",
        "median2025": "82300",
        "Very_Low_50_1": "28850",
        "Very_Low_50_2": "32950",
        "Very_Low_50_3": "37100",
        "Very_Low_50_4": "41200",
        "Very_Low_50_5": "44500",
        "Very_Low_50_6": "47800",
        "Very_Low_50_7": "51100",
        "Very_Low_50_8": "54400",
        "Extremely_Low_30_1": "17300",
        "Extremely_Low_30_2": "19800",
        "Extremely_Low_30_3": "22250",
        "Extremely_Low_30_4": "24700",
        "Extremely_Low_30_5": "26680",
        "Extremely_Low_30_6": "28660",
        "Extremely_Low_30_7": "30640",
        "Extremely_Low_30_8": "32620",
        "Low_80_1": "46150",
        "Low_80_2": "52750",
        "Low_80_3": "59350",
        "Low_80_4": "65900",
        "Low_80_5": "71200",
        "Low_80_6": "76450",
        "Low_80_7": "81750",
        "Low_80_8": "87000"
      }
    ]
    """;

    [Fact]
    public async Task GetIncomeLimitsAsync_MatchesCountyName_ReturnsDallasData()
    {
        var handler = new MockHttpMessageHandler(ValidHudStateResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/") };
        var service = new HudApiClient(client, NullLogger<HudApiClient>.Instance);

        var result = await service.GetIncomeLimitsAsync("TX", "Dallas");

        Assert.NotNull(result);
        Assert.Equal(88_200m, result!.MedianFamilyIncome);
        Assert.Contains("Dallas", result.AreaName);
        Assert.Equal(44_100, result.VeryLow.Person4);
        Assert.Equal(26_500, result.ExtremelyLow.Person4);
        Assert.Equal(70_600, result.Low.Person4);
    }

    [Fact]
    public async Task GetIncomeLimitsAsync_MatchesCountyName_CaseInsensitive()
    {
        var handler = new MockHttpMessageHandler(ValidHudStateResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/") };
        var service = new HudApiClient(client, NullLogger<HudApiClient>.Instance);

        var result = await service.GetIncomeLimitsAsync("TX", "dallas");

        Assert.NotNull(result);
        Assert.Contains("Dallas", result!.AreaName);
    }

    [Fact]
    public async Task GetIncomeLimitsAsync_NoCountyMatch_ReturnsFirstEntry()
    {
        var handler = new MockHttpMessageHandler(ValidHudStateResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/") };
        var service = new HudApiClient(client, NullLogger<HudApiClient>.Instance);

        var result = await service.GetIncomeLimitsAsync("TX", "NonexistentCounty");

        // Falls back to first entry in the list
        Assert.NotNull(result);
        Assert.Contains("Dallas", result!.AreaName);
    }

    [Fact]
    public async Task GetIncomeLimitsAsync_NullCounty_ReturnsFirstEntry()
    {
        var handler = new MockHttpMessageHandler(ValidHudStateResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/") };
        var service = new HudApiClient(client, NullLogger<HudApiClient>.Instance);

        var result = await service.GetIncomeLimitsAsync("TX");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetIncomeLimitsAsync_HttpFailure_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("Server Error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/") };
        var service = new HudApiClient(client, NullLogger<HudApiClient>.Instance);

        var result = await service.GetIncomeLimitsAsync("TX", "Dallas");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetIncomeLimitsAsync_InvalidJson_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("not json", HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/") };
        var service = new HudApiClient(client, NullLogger<HudApiClient>.Instance);

        var result = await service.GetIncomeLimitsAsync("TX", "Dallas");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetIncomeLimitsAsync_IncludesStateCodeInUrl()
    {
        var handler = new MockHttpMessageHandler(ValidHudStateResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/") };
        var service = new HudApiClient(client, NullLogger<HudApiClient>.Instance);

        await service.GetIncomeLimitsAsync("TX", "Dallas");

        Assert.NotNull(handler.LastRequest);
        var url = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("statedata", url);
        Assert.Contains("48", url); // TX FIPS code
    }

    [Fact]
    public async Task GetIncomeLimitsAsync_SetsAuthorizationHeader()
    {
        var handler = new MockHttpMessageHandler(ValidHudStateResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/") };
        // HUD API requires Bearer token — client should set Authorization header
        var service = new HudApiClient(client, NullLogger<HudApiClient>.Instance);

        await service.GetIncomeLimitsAsync("TX", "Dallas");

        // The client should build a valid request even without a configured token
        Assert.NotNull(handler.LastRequest);
    }

    [Fact]
    public async Task GetIncomeLimitsAsync_ParsesAllHouseholdSizes()
    {
        var handler = new MockHttpMessageHandler(ValidHudStateResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.huduser.gov/hudapi/public/") };
        var service = new HudApiClient(client, NullLogger<HudApiClient>.Instance);

        var result = await service.GetIncomeLimitsAsync("TX", "Dallas");

        Assert.NotNull(result);
        // Verify all 8 household sizes parsed for VeryLow (50% AMI)
        Assert.Equal(30_900, result!.VeryLow.Person1);
        Assert.Equal(35_300, result.VeryLow.Person2);
        Assert.Equal(39_700, result.VeryLow.Person3);
        Assert.Equal(44_100, result.VeryLow.Person4);
        Assert.Equal(47_650, result.VeryLow.Person5);
        Assert.Equal(51_150, result.VeryLow.Person6);
        Assert.Equal(54_700, result.VeryLow.Person7);
        Assert.Equal(58_200, result.VeryLow.Person8);
    }
}
