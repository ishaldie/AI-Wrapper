using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Domain.Interfaces;

public interface IRealAiClient
{
    Task<PropertyData?> GetPropertyDataAsync(string address, CancellationToken ct = default);
    Task<TenantMetrics?> GetTenantMetricsAsync(string address, CancellationToken ct = default);
    Task<MarketData?> GetMarketDataAsync(string address, CancellationToken ct = default);
    Task<IReadOnlyList<SalesComp>> GetSalesCompsAsync(string address, CancellationToken ct = default);
    Task<TimeSeriesData?> GetTimeSeriesAsync(string address, CancellationToken ct = default);
}
