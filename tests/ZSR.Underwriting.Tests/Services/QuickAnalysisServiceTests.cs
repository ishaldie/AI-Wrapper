using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class QuickAnalysisServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly QuickAnalysisService _sut;
    private readonly string _dbName = $"QuickAnalysisTests_{Guid.NewGuid()}";

    public QuickAnalysisServiceTests()
    {
        var dbName = _dbName;
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        _serviceProvider = services.BuildServiceProvider();

        _sut = new QuickAnalysisService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<QuickAnalysisService>.Instance);
    }

    [Fact]
    public async Task StartAnalysisAsync_CreatesDeal_ReturnsDealId()
    {
        var progress = await _sut.StartAnalysisAsync("123 Main St, Dallas TX", "test-user");

        Assert.NotEqual(Guid.Empty, progress.DealId);
        Assert.Equal("123 Main St, Dallas TX", progress.SearchQuery);
        Assert.Equal(StepStatus.Complete, progress.DealCreation);
    }

    [Fact]
    public async Task StartAnalysisAsync_DealPersistedInDb()
    {
        var progress = await _sut.StartAnalysisAsync("456 Oak Ave, Austin TX", "test-user");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deal = await db.Deals.FindAsync(progress.DealId);

        Assert.NotNull(deal);
        Assert.Equal("456 Oak Ave, Austin TX", deal.PropertyName);
        Assert.Equal("456 Oak Ave, Austin TX", deal.Address);
    }

    [Fact]
    public async Task StartAnalysisAsync_EmitsDealCreatedEvent()
    {
        var tracker = new SpyActivityTracker();

        var progress = await _sut.StartAnalysisAsync("789 Elm St, Houston TX", "test-user", activityTracker: tracker);

        Assert.Single(tracker.TrackedEvents);
        var (eventType, dealId, metadata) = tracker.TrackedEvents[0];
        Assert.Equal(ActivityEventType.DealCreated, eventType);
        Assert.Equal(progress.DealId, dealId);
    }

    [Fact]
    public async Task StartAnalysisAsync_NullTracker_DoesNotThrow()
    {
        var progress = await _sut.StartAnalysisAsync("100 Pine St, San Antonio TX", "test-user", activityTracker: null);

        Assert.NotEqual(Guid.Empty, progress.DealId);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private sealed class SpyActivityTracker : IActivityTracker
    {
        public List<(ActivityEventType EventType, Guid? DealId, string? Metadata)> TrackedEvents { get; } = new();

        public Task<Guid> StartSessionAsync(string userId) => Task.FromResult(Guid.NewGuid());
        public Task TrackPageViewAsync(string pageUrl) => Task.CompletedTask;

        public Task TrackEventAsync(ActivityEventType eventType, Guid? dealId = null, string? metadata = null)
        {
            TrackedEvents.Add((eventType, dealId, metadata));
            return Task.CompletedTask;
        }
    }
}
