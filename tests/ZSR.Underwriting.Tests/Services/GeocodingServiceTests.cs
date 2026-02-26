using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;

namespace ZSR.Underwriting.Tests.Services;

public class GeocodingServiceTests
{
    private static GoogleGeocodingService CreateService(MockHttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var options = Options.Create(new GoogleMapsOptions { ApiKey = "test-key" });
        return new GoogleGeocodingService(client, options, NullLogger<GoogleGeocodingService>.Instance);
    }

    [Fact]
    public async Task GeocodeAsync_Returns_Coordinates_On_Success()
    {
        var json = """
        {
            "status": "OK",
            "results": [{
                "formatted_address": "1600 Amphitheatre Parkway, Mountain View, CA 94043, USA",
                "geometry": {
                    "location": { "lat": 37.4224764, "lng": -122.0842499 }
                }
            }]
        }
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
        var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
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
        var json = """
        {
            "status": "ZERO_RESULTS",
            "results": []
        }
        """;
        var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
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
    public async Task GeocodeAsync_Includes_Api_Key_In_Request()
    {
        var json = """{"status": "ZERO_RESULTS", "results": []}""";
        var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
        var service = CreateService(handler);

        await service.GeocodeAsync("123 Main St");

        Assert.NotNull(handler.LastRequest);
        Assert.Contains("key=test-key", handler.LastRequest.RequestUri!.ToString());
    }
}
