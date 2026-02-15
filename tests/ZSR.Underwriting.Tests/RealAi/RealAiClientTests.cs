using System.Net;
using System.Text.Json;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.ValueObjects;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.RealAi;

public class RealAiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static RealAiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.realai.com/api/")
        };
        return new RealAiClient(httpClient);
    }

    // --- GetPropertyDataAsync ---

    [Fact]
    public async Task GetPropertyDataAsync_Success_ReturnsPropertyData()
    {
        var payload = new
        {
            inPlaceRent = 1200.50,
            occupancy = 0.95,
            yearBuilt = 1990,
            acreage = 2.5,
            squareFootage = 50000,
            amenities = "Pool,Gym",
            buildingType = "Garden"
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(payload, JsonOptions), HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.GetPropertyDataAsync("123 Main St");

        Assert.NotNull(result);
        Assert.Equal(1200.50m, result!.InPlaceRent);
        Assert.Equal(0.95m, result.Occupancy);
        Assert.Equal(1990, result.YearBuilt);
        Assert.Equal(50000, result.SquareFootage);
        Assert.Equal("Garden", result.BuildingType);
    }

    [Fact]
    public async Task GetPropertyDataAsync_NotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.NotFound);
        var client = CreateClient(handler);

        var result = await client.GetPropertyDataAsync("Unknown Address");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPropertyDataAsync_ServerError_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        var result = await client.GetPropertyDataAsync("123 Main St");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPropertyDataAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
        var client = CreateClient(handler);

        await client.GetPropertyDataAsync("123 Main St, Dallas TX");

        Assert.NotNull(handler.LastRequest);
        var url = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("property", url);
        Assert.Contains("address=", url);
        Assert.Contains("123", url);
    }

    // --- GetTenantMetricsAsync ---

    [Fact]
    public async Task GetTenantMetricsAsync_Success_ReturnsTenantMetrics()
    {
        var payload = new
        {
            subject = new { averageFico = 720, rentToIncomeRatio = 0.28, medianHhi = 55000 },
            zipcode = new { averageFico = 710, rentToIncomeRatio = 0.30, medianHhi = 52000 },
            metro = new { averageFico = 700, rentToIncomeRatio = 0.32, medianHhi = 50000 }
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(payload, JsonOptions), HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.GetTenantMetricsAsync("123 Main St");

        Assert.NotNull(result);
        Assert.Equal(720, result!.Subject.AverageFico);
        Assert.Equal(0.28m, result.Subject.RentToIncomeRatio);
        Assert.Equal(55000m, result.Subject.MedianHhi);
        Assert.Equal(710, result.Zipcode.AverageFico);
    }

    [Fact]
    public async Task GetTenantMetricsAsync_NotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.NotFound);
        var client = CreateClient(handler);

        var result = await client.GetTenantMetricsAsync("Unknown");

        Assert.Null(result);
    }

    // --- GetMarketDataAsync ---

    [Fact]
    public async Task GetMarketDataAsync_Success_ReturnsMarketData()
    {
        var payload = new
        {
            capRate = 0.065,
            rentGrowth = 0.035,
            jobGrowth = 0.025,
            netMigration = 15000,
            permits = 8500
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(payload, JsonOptions), HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.GetMarketDataAsync("123 Main St");

        Assert.NotNull(result);
        Assert.Equal(0.065m, result!.CapRate);
        Assert.Equal(0.035m, result.RentGrowth);
        Assert.Equal(15000, result.NetMigration);
        Assert.Equal(8500, result.Permits);
    }

    [Fact]
    public async Task GetMarketDataAsync_ServerError_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        var result = await client.GetMarketDataAsync("123 Main St");

        Assert.Null(result);
    }

    // --- GetSalesCompsAsync ---

    [Fact]
    public async Task GetSalesCompsAsync_Success_ReturnsList()
    {
        var payload = new[]
        {
            new { address = "100 Oak Ave", pricePerUnit = 95000, saleDate = "2025-06-15", units = 48, condition = "Good" },
            new { address = "200 Elm St", pricePerUnit = 88000, saleDate = "2025-03-10", units = 36, condition = "Fair" }
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(payload, JsonOptions), HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.GetSalesCompsAsync("123 Main St");

        Assert.Equal(2, result.Count);
        Assert.Equal("100 Oak Ave", result[0].Address);
        Assert.Equal(95000m, result[0].PricePerUnit);
        Assert.Equal(48, result[0].Units);
    }

    [Fact]
    public async Task GetSalesCompsAsync_NotFound_ReturnsEmptyList()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.NotFound);
        var client = CreateClient(handler);

        var result = await client.GetSalesCompsAsync("Unknown");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // --- GetTimeSeriesAsync ---

    [Fact]
    public async Task GetTimeSeriesAsync_Success_ReturnsTimeSeries()
    {
        var payload = new
        {
            rentTrend = new[] { new { date = "2025-01-01", value = 1150 }, new { date = "2025-06-01", value = 1200 } },
            occupancyTrend = new[] { new { date = "2025-01-01", value = 0.93 }, new { date = "2025-06-01", value = 0.95 } }
        };
        var handler = new MockHttpMessageHandler(
            JsonSerializer.Serialize(payload, JsonOptions), HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.GetTimeSeriesAsync("123 Main St");

        Assert.NotNull(result);
        Assert.Equal(2, result!.RentTrend.Count);
        Assert.Equal(1150m, result.RentTrend[0].Value);
        Assert.Equal(2, result.OccupancyTrend.Count);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_ServerError_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        var result = await client.GetTimeSeriesAsync("123 Main St");

        Assert.Null(result);
    }

    // --- Invalid JSON ---

    [Fact]
    public async Task GetPropertyDataAsync_InvalidJson_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("not-json!!!", HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.GetPropertyDataAsync("123 Main St");

        Assert.Null(result);
    }
}

/// <summary>
/// Simple mock HttpMessageHandler for unit testing HTTP clients.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _response;
    private readonly HttpStatusCode _statusCode;

    public HttpRequestMessage? LastRequest { get; private set; }

    public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
    {
        _response = response;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
        });
    }
}
