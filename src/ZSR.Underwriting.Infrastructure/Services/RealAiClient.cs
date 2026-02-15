using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Infrastructure.Services;

public class RealAiClient : IRealAiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RealAiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RealAiClient(HttpClient httpClient, ILogger<RealAiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PropertyData?> GetPropertyDataAsync(string address, CancellationToken ct = default)
    {
        return await FetchAsync<PropertyData>("property", address, ct);
    }

    public async Task<TenantMetrics?> GetTenantMetricsAsync(string address, CancellationToken ct = default)
    {
        return await FetchAsync<TenantMetrics>("tenant-metrics", address, ct);
    }

    public async Task<MarketData?> GetMarketDataAsync(string address, CancellationToken ct = default)
    {
        return await FetchAsync<MarketData>("market", address, ct);
    }

    public async Task<IReadOnlyList<SalesComp>> GetSalesCompsAsync(string address, CancellationToken ct = default)
    {
        try
        {
            var endpoint = $"sales-comps?address={Uri.EscapeDataString(address)}";
            _logger.LogInformation("RealAI API call: GET {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("RealAI API returned {StatusCode} for {Endpoint}", (int)response.StatusCode, endpoint);
                return [];
            }

            var result = await response.Content.ReadFromJsonAsync<List<SalesComp>>(JsonOptions, ct);
            _logger.LogInformation("RealAI API success: {Endpoint} returned {Count} comps", endpoint, result?.Count ?? 0);
            return result?.AsReadOnly() ?? (IReadOnlyList<SalesComp>)[];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "RealAI API HTTP error for sales-comps");
            return [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "RealAI API JSON parse error for sales-comps");
            return [];
        }
    }

    public async Task<TimeSeriesData?> GetTimeSeriesAsync(string address, CancellationToken ct = default)
    {
        return await FetchAsync<TimeSeriesData>("time-series", address, ct);
    }

    private async Task<T?> FetchAsync<T>(string path, string address, CancellationToken ct) where T : class
    {
        try
        {
            var endpoint = $"{path}?address={Uri.EscapeDataString(address)}";
            _logger.LogInformation("RealAI API call: GET {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("RealAI API returned {StatusCode} for {Endpoint}", (int)response.StatusCode, endpoint);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            _logger.LogInformation("RealAI API success: {Endpoint}", endpoint);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "RealAI API HTTP error for {Path}", path);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "RealAI API JSON parse error for {Path}", path);
            return null;
        }
    }
}
