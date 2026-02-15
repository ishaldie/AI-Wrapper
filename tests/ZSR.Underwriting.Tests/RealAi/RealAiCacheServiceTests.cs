using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.ValueObjects;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.RealAi;

public class RealAiCacheServiceTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly ILogger<RealAiCacheService> _logger = NullLogger<RealAiCacheService>.Instance;
    private readonly Guid _dealId = Guid.NewGuid();
    private const string TestAddress = "123 Main St, Dallas TX";

    public void Dispose() => _cache.Dispose();

    private RealAiCacheService CreateService(IRealAiClient client) =>
        new(client, _cache, _logger);

    // --- Cache Miss ---

    [Fact]
    public async Task GetOrFetchAsync_CacheMiss_CallsClientAndReturnsData()
    {
        var client = new FakeRealAiClient(hasData: true);
        var service = CreateService(client);

        var result = await service.GetOrFetchAsync(_dealId, TestAddress);

        Assert.NotNull(result);
        Assert.Equal(_dealId, result.DealId);
        Assert.Equal(1200m, result.InPlaceRent);
        Assert.Equal(720, result.AverageFico);
        Assert.Equal(0.065m, result.MarketCapRate);
        Assert.NotNull(result.SalesCompsJson);
        Assert.NotNull(result.RentTrendJson);
        Assert.Equal(1, client.PropertyCallCount);
    }

    // --- Cache Hit ---

    [Fact]
    public async Task GetOrFetchAsync_CacheHit_DoesNotCallClientAgain()
    {
        var client = new FakeRealAiClient(hasData: true);
        var service = CreateService(client);

        var first = await service.GetOrFetchAsync(_dealId, TestAddress);
        var second = await service.GetOrFetchAsync(_dealId, TestAddress);

        Assert.Same(first, second);
        Assert.Equal(1, client.PropertyCallCount);
    }

    // --- Cache Invalidation ---

    [Fact]
    public async Task Invalidate_ClearsCache_NextCallFetchesFresh()
    {
        var client = new FakeRealAiClient(hasData: true);
        var service = CreateService(client);

        await service.GetOrFetchAsync(_dealId, TestAddress);
        Assert.Equal(1, client.PropertyCallCount);

        service.Invalidate(_dealId);

        await service.GetOrFetchAsync(_dealId, TestAddress);
        Assert.Equal(2, client.PropertyCallCount);
    }

    // --- Partial Data ---

    [Fact]
    public async Task GetOrFetchAsync_PartialData_ReturnsWithNulls()
    {
        var client = new FakeRealAiClient(hasData: false);
        var service = CreateService(client);

        var result = await service.GetOrFetchAsync(_dealId, TestAddress);

        Assert.NotNull(result);
        Assert.Null(result.InPlaceRent);
        Assert.Null(result.AverageFico);
        Assert.Null(result.MarketCapRate);
        Assert.Null(result.SalesCompsJson);
    }

    // --- Different Deals ---

    [Fact]
    public async Task GetOrFetchAsync_DifferentDeals_CachedSeparately()
    {
        var client = new FakeRealAiClient(hasData: true);
        var service = CreateService(client);
        var dealId2 = Guid.NewGuid();

        await service.GetOrFetchAsync(_dealId, TestAddress);
        await service.GetOrFetchAsync(dealId2, "456 Oak Ave");

        Assert.Equal(2, client.PropertyCallCount);
    }

    // --- GetCachedDataAsync ---

    [Fact]
    public async Task GetCachedDataAsync_NoCache_ReturnsNull()
    {
        var client = new FakeRealAiClient(hasData: true);
        var service = CreateService(client);

        var result = await service.GetCachedDataAsync(_dealId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCachedDataAsync_AfterFetch_ReturnsCachedData()
    {
        var client = new FakeRealAiClient(hasData: true);
        var service = CreateService(client);

        await service.GetOrFetchAsync(_dealId, TestAddress);
        var cached = await service.GetCachedDataAsync(_dealId);

        Assert.NotNull(cached);
        Assert.Equal(_dealId, cached!.DealId);
    }
}

/// <summary>
/// Fake IRealAiClient for cache service tests.
/// </summary>
internal class FakeRealAiClient : IRealAiClient
{
    private readonly bool _hasData;
    public int PropertyCallCount { get; private set; }

    public FakeRealAiClient(bool hasData) => _hasData = hasData;

    public Task<PropertyData?> GetPropertyDataAsync(string address, CancellationToken ct = default)
    {
        PropertyCallCount++;
        if (!_hasData) return Task.FromResult<PropertyData?>(null);
        return Task.FromResult<PropertyData?>(new PropertyData
        {
            InPlaceRent = 1200m, Occupancy = 0.95m, YearBuilt = 1990,
            Acreage = 2.5m, SquareFootage = 50000, Amenities = "Pool", BuildingType = "Garden"
        });
    }

    public Task<TenantMetrics?> GetTenantMetricsAsync(string address, CancellationToken ct = default)
    {
        if (!_hasData) return Task.FromResult<TenantMetrics?>(null);
        return Task.FromResult<TenantMetrics?>(new TenantMetrics
        {
            Subject = new MetricLevel { AverageFico = 720, RentToIncomeRatio = 0.28m, MedianHhi = 55000m }
        });
    }

    public Task<MarketData?> GetMarketDataAsync(string address, CancellationToken ct = default)
    {
        if (!_hasData) return Task.FromResult<MarketData?>(null);
        return Task.FromResult<MarketData?>(new MarketData
        {
            CapRate = 0.065m, RentGrowth = 0.035m, JobGrowth = 0.025m, NetMigration = 15000, Permits = 8500
        });
    }

    public Task<IReadOnlyList<SalesComp>> GetSalesCompsAsync(string address, CancellationToken ct = default)
    {
        if (!_hasData) return Task.FromResult<IReadOnlyList<SalesComp>>([]);
        return Task.FromResult<IReadOnlyList<SalesComp>>(new List<SalesComp>
        {
            new() { Address = "100 Oak Ave", PricePerUnit = 95000m, Units = 48 }
        }.AsReadOnly());
    }

    public Task<TimeSeriesData?> GetTimeSeriesAsync(string address, CancellationToken ct = default)
    {
        if (!_hasData) return Task.FromResult<TimeSeriesData?>(null);
        return Task.FromResult<TimeSeriesData?>(new TimeSeriesData
        {
            RentTrend = [new DataPoint { Date = DateTime.Today, Value = 1150m }],
            OccupancyTrend = [new DataPoint { Date = DateTime.Today, Value = 0.93m }]
        });
    }
}
