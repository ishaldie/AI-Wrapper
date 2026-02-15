using System.Net.Http.Json;
using System.Text.Json;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Infrastructure.Services;

public class RealAiClient : IRealAiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RealAiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PropertyData?> GetPropertyDataAsync(string address, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"property?address={Uri.EscapeDataString(address)}", ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<PropertyData>(JsonOptions, ct);
        }
        catch (HttpRequestException) { return null; }
        catch (JsonException) { return null; }
    }

    public async Task<TenantMetrics?> GetTenantMetricsAsync(string address, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"tenant-metrics?address={Uri.EscapeDataString(address)}", ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TenantMetrics>(JsonOptions, ct);
        }
        catch (HttpRequestException) { return null; }
        catch (JsonException) { return null; }
    }

    public async Task<MarketData?> GetMarketDataAsync(string address, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"market?address={Uri.EscapeDataString(address)}", ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<MarketData>(JsonOptions, ct);
        }
        catch (HttpRequestException) { return null; }
        catch (JsonException) { return null; }
    }

    public async Task<IReadOnlyList<SalesComp>> GetSalesCompsAsync(string address, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"sales-comps?address={Uri.EscapeDataString(address)}", ct);
            if (!response.IsSuccessStatusCode) return [];
            var result = await response.Content.ReadFromJsonAsync<List<SalesComp>>(JsonOptions, ct);
            return result?.AsReadOnly() ?? (IReadOnlyList<SalesComp>)[];
        }
        catch (HttpRequestException) { return []; }
        catch (JsonException) { return []; }
    }

    public async Task<TimeSeriesData?> GetTimeSeriesAsync(string address, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"time-series?address={Uri.EscapeDataString(address)}", ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TimeSeriesData>(JsonOptions, ct);
        }
        catch (HttpRequestException) { return null; }
        catch (JsonException) { return null; }
    }
}
