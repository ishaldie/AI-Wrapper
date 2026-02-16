using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.ValueObjects;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class QuickAnalysisServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly QuickAnalysisService _sut;
    private readonly string _dbName;

    public QuickAnalysisServiceTests()
    {
        _dbName = $"QuickAnalysisTests_{Guid.NewGuid()}";

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddScoped<IRealAiClient, StubRealAiClient>();
        services.AddScoped<IReportAssembler, StubReportAssembler>();
        services.AddSingleton<ILogger<QuickAnalysisService>>(
            NullLogger<QuickAnalysisService>.Instance);

        _serviceProvider = services.BuildServiceProvider();

        _sut = new QuickAnalysisService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<QuickAnalysisService>.Instance);
    }

    [Fact]
    public async Task StartAnalysisAsync_CreatesDeal_ReturnsDealId()
    {
        var progress = await _sut.StartAnalysisAsync("123 Main St, Dallas TX");

        Assert.NotEqual(Guid.Empty, progress.DealId);
        Assert.Equal("123 Main St, Dallas TX", progress.SearchQuery);
        Assert.Equal(StepStatus.Complete, progress.DealCreation);
    }

    [Fact]
    public async Task StartAnalysisAsync_DealPersistedInDb()
    {
        var progress = await _sut.StartAnalysisAsync("456 Oak Ave, Austin TX");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deal = await db.Deals.FindAsync(progress.DealId);

        Assert.NotNull(deal);
        Assert.Equal("456 Oak Ave, Austin TX", deal.PropertyName);
        Assert.Equal("456 Oak Ave, Austin TX", deal.Address);
    }

    [Fact]
    public async Task StartAnalysisAsync_RegistersProgressInTracker()
    {
        var progress = await _sut.StartAnalysisAsync("789 Elm St, Houston TX");

        var tracked = QuickAnalysisTracker.GetProgress(progress.DealId);
        Assert.NotNull(tracked);
        Assert.Same(progress, tracked);

        // Cleanup
        QuickAnalysisTracker.Remove(progress.DealId);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    // Stub implementations for testing
    private class StubRealAiClient : IRealAiClient
    {
        public Task<PropertyData?> GetPropertyDataAsync(string address, CancellationToken ct = default)
            => Task.FromResult<PropertyData?>(new PropertyData { InPlaceRent = 1200m, Occupancy = 94m });

        public Task<TenantMetrics?> GetTenantMetricsAsync(string address, CancellationToken ct = default)
            => Task.FromResult<TenantMetrics?>(null);

        public Task<MarketData?> GetMarketDataAsync(string address, CancellationToken ct = default)
            => Task.FromResult<MarketData?>(null);

        public Task<IReadOnlyList<SalesComp>> GetSalesCompsAsync(string address, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SalesComp>>(Array.Empty<SalesComp>());

        public Task<TimeSeriesData?> GetTimeSeriesAsync(string address, CancellationToken ct = default)
            => Task.FromResult<TimeSeriesData?>(null);
    }

    private class StubReportAssembler : IReportAssembler
    {
        public Task<Application.DTOs.Report.UnderwritingReportDto> AssembleReportAsync(
            Guid dealId, CancellationToken cancellationToken = default)
            => Task.FromResult(new Application.DTOs.Report.UnderwritingReportDto());
    }
}
