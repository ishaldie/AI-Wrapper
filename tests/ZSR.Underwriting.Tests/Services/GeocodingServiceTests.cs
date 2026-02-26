using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;

namespace ZSR.Underwriting.Tests.Services;

public class GeocodingServiceTests
{
    private static NominatimGeocodingService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        return new NominatimGeocodingService(client, NullLogger<NominatimGeocodingService>.Instance);
    }

    [Fact]
    public async Task GeocodeAsync_Returns_Coordinates_On_Success()
    {
        var json = """
        [{
            "lat": "37.4224764",
            "lon": "-122.0842499",
            "display_name": "1600 Amphitheatre Parkway, Mountain View, CA 94043, USA"
        }]
        """;
        var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("1600 Amphitheatre Parkway");

        Assert.NotNull(result);
        Assert.Equal(37.4224764, result.Latitude);
        Assert.Equal(-122.0842499, result.Longitude);
        Assert.Equal("1600 Amphitheatre Parkway, Mountain View, CA 94043, USA", result.FormattedAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GeocodeAsync_Returns_Null_For_Empty_Address(string? address)
    {
        var handler = new MockHttpMessageHandler("[]", HttpStatusCode.OK);
        var service = CreateService(handler);

        var result = await service.GeocodeAsync(address!);

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_Returns_Null_On_Http_Error()
    {
        var handler = new MockHttpMessageHandler("error", HttpStatusCode.InternalServerError);
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("123 Main St");

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_Returns_Null_When_No_Results()
    {
        var handler = new MockHttpMessageHandler("[]", HttpStatusCode.OK);
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("zzzzz nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_Returns_Null_On_Invalid_Json()
    {
        var handler = new MockHttpMessageHandler("not json at all", HttpStatusCode.OK);
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("123 Main St");

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_Sends_UserAgent_Header()
    {
        var json = """[{"lat": "40.7", "lon": "-74.0", "display_name": "NYC"}]""";
        var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
        var service = CreateService(handler);

        await service.GeocodeAsync("New York");

        Assert.NotNull(handler.LastRequest);
        Assert.Contains("ZSR-Underwriting", handler.LastRequest.Headers.UserAgent.ToString());
    }
}
