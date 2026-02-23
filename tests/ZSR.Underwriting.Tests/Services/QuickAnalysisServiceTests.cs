using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Application.DTOs;
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

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
