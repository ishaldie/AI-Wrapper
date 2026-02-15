using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.RealAi;

public class RealAiClientContractTests
{
    [Fact]
    public void IRealAiClient_HasAllRequiredMethods()
    {
        var type = typeof(IRealAiClient);

        Assert.NotNull(type.GetMethod("GetPropertyDataAsync"));
        Assert.NotNull(type.GetMethod("GetTenantMetricsAsync"));
        Assert.NotNull(type.GetMethod("GetMarketDataAsync"));
        Assert.NotNull(type.GetMethod("GetSalesCompsAsync"));
        Assert.NotNull(type.GetMethod("GetTimeSeriesAsync"));
    }

    [Fact]
    public async Task StubClient_ReturnsPropertyData()
    {
        IRealAiClient client = new StubRealAiClient();

        var result = await client.GetPropertyDataAsync("123 Main St");

        Assert.NotNull(result);
        Assert.Equal(1200m, result.InPlaceRent);
        Assert.Equal(0.94m, result.Occupancy);
        Assert.Equal(1985, result.YearBuilt);
        Assert.Equal("Garden", result.BuildingType);
    }

    [Fact]
    public async Task StubClient_ReturnsTenantMetrics()
    {
        IRealAiClient client = new StubRealAiClient();

        var result = await client.GetTenantMetricsAsync("123 Main St");

        Assert.NotNull(result);
        Assert.NotNull(result.Subject);
        Assert.NotNull(result.Zipcode);
        Assert.NotNull(result.Metro);
        Assert.Equal(680, result.Subject.AverageFico);
    }

    [Fact]
    public async Task StubClient_ReturnsMarketData()
    {
        IRealAiClient client = new StubRealAiClient();

        var result = await client.GetMarketDataAsync("123 Main St");

        Assert.NotNull(result);
        Assert.Equal(0.055m, result.CapRate);
    }

    [Fact]
    public async Task StubClient_ReturnsSalesComps()
    {
        IRealAiClient client = new StubRealAiClient();

        var result = await client.GetSalesCompsAsync("123 Main St");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(150_000m, result[0].PricePerUnit);
    }

    [Fact]
    public async Task StubClient_ReturnsTimeSeries()
    {
        IRealAiClient client = new StubRealAiClient();

        var result = await client.GetTimeSeriesAsync("123 Main St");

        Assert.NotNull(result);
        Assert.NotEmpty(result.RentTrend);
        Assert.NotEmpty(result.OccupancyTrend);
    }

    private class StubRealAiClient : IRealAiClient
    {
        public Task<PropertyData?> GetPropertyDataAsync(string address, CancellationToken ct = default)
        {
            return Task.FromResult<PropertyData?>(new PropertyData
            {
                InPlaceRent = 1200m,
                Occupancy = 0.94m,
                YearBuilt = 1985,
                Acreage = 5.2m,
                SquareFootage = 120_000,
                Amenities = "Pool, Fitness Center, Clubhouse",
                BuildingType = "Garden"
            });
        }

        public Task<TenantMetrics?> GetTenantMetricsAsync(string address, CancellationToken ct = default)
        {
            return Task.FromResult<TenantMetrics?>(new TenantMetrics
            {
                Subject = new MetricLevel { AverageFico = 680, RentToIncomeRatio = 0.30m, MedianHhi = 52_000m },
                Zipcode = new MetricLevel { AverageFico = 700, RentToIncomeRatio = 0.28m, MedianHhi = 58_000m },
                Metro = new MetricLevel { AverageFico = 710, RentToIncomeRatio = 0.27m, MedianHhi = 62_000m }
            });
        }

        public Task<MarketData?> GetMarketDataAsync(string address, CancellationToken ct = default)
        {
            return Task.FromResult<MarketData?>(new MarketData
            {
                CapRate = 0.055m,
                RentGrowth = 0.03m,
                JobGrowth = 0.02m,
                NetMigration = 5000,
                Permits = 1200
            });
        }

        public Task<IReadOnlyList<SalesComp>> GetSalesCompsAsync(string address, CancellationToken ct = default)
        {
            var comps = new List<SalesComp>
            {
                new()
                {
                    Address = "456 Oak Ave",
                    PricePerUnit = 150_000m,
                    SaleDate = new DateTime(2025, 6, 15),
                    Units = 120,
                    Condition = "B"
                }
            };
            return Task.FromResult<IReadOnlyList<SalesComp>>(comps);
        }

        public Task<TimeSeriesData?> GetTimeSeriesAsync(string address, CancellationToken ct = default)
        {
            return Task.FromResult<TimeSeriesData?>(new TimeSeriesData
            {
                RentTrend = new List<DataPoint>
                {
                    new() { Date = new DateTime(2025, 1, 1), Value = 1100m },
                    new() { Date = new DateTime(2025, 7, 1), Value = 1200m }
                },
                OccupancyTrend = new List<DataPoint>
                {
                    new() { Date = new DateTime(2025, 1, 1), Value = 0.92m },
                    new() { Date = new DateTime(2025, 7, 1), Value = 0.94m }
                }
            });
        }
    }
}
