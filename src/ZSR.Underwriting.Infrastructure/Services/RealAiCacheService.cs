using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Mapping;

namespace ZSR.Underwriting.Infrastructure.Services;

/// <summary>
/// Caches RealAI API responses per deal with a 24-hour TTL.
/// Orchestrates fetching from IRealAiClient and mapping to RealAiData entity.
/// </summary>
public class RealAiCacheService
{
    private readonly IRealAiClient _client;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RealAiCacheService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public RealAiCacheService(
        IRealAiClient client,
        IMemoryCache cache,
        ILogger<RealAiCacheService> logger)
    {
        _client = client;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Returns cached RealAiData for a deal, or null if not cached.
    /// </summary>
    public Task<RealAiData?> GetCachedDataAsync(Guid dealId, CancellationToken ct = default)
    {
        var key = CacheKey(dealId);
        _cache.TryGetValue(key, out RealAiData? cached);
        return Task.FromResult(cached);
    }

    /// <summary>
    /// Returns cached data if available, otherwise fetches from RealAI API,
    /// maps to entity, and caches with 24-hour TTL.
    /// </summary>
    public async Task<RealAiData> GetOrFetchAsync(Guid dealId, string address, CancellationToken ct = default)
    {
        var key = CacheKey(dealId);

        if (_cache.TryGetValue(key, out RealAiData? cached) && cached is not null)
        {
            _logger.LogDebug("RealAI cache hit for deal {DealId}", dealId);
            return cached;
        }

        _logger.LogInformation("RealAI cache miss for deal {DealId}, fetching from API for address {Address}", dealId, address);

        var property = await _client.GetPropertyDataAsync(address, ct);
        var tenant = await _client.GetTenantMetricsAsync(address, ct);
        var market = await _client.GetMarketDataAsync(address, ct);
        var comps = await _client.GetSalesCompsAsync(address, ct);
        var timeSeries = await _client.GetTimeSeriesAsync(address, ct);

        var entity = RealAiDataMapper.Map(dealId, property, tenant, market, comps, timeSeries);

        _cache.Set(key, entity, CacheTtl);
        _logger.LogInformation("RealAI data cached for deal {DealId}, TTL {Ttl}h", dealId, CacheTtl.TotalHours);

        return entity;
    }

    /// <summary>
    /// Removes cached data for a deal, forcing a fresh fetch on next access.
    /// </summary>
    public void Invalidate(Guid dealId)
    {
        var key = CacheKey(dealId);
        _cache.Remove(key);
        _logger.LogInformation("RealAI cache invalidated for deal {DealId}", dealId);
    }

    private static string CacheKey(Guid dealId) => $"realai:{dealId}";
}
