using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class TokenBudgetServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TokenBudgetService _service;
    private readonly TokenManagementOptions _options;

    public TokenBudgetServiceTests()
    {
        var dbName = $"TokenBudget_{Guid.NewGuid()}";
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _db = new AppDbContext(dbOptions);
        _options = new TokenManagementOptions
        {
            DailyUserTokenBudget = 500_000,
            DealTokenBudget = 1_000_000
        };
        _service = new TokenBudgetService(_db, Options.Create(_options));
    }

    // --- User daily budget ---

    [Fact]
    public async Task CheckUserBudget_UnderLimit_ReturnsAllowed()
    {
        await SeedUsage("user-1", null, 100_000, 50_000);

        var (allowed, used, limit) = await _service.CheckUserBudgetAsync("user-1");

        Assert.True(allowed);
        Assert.Equal(150_000, used); // input + output
        Assert.Equal(500_000, limit);
    }

    [Fact]
    public async Task CheckUserBudget_OverLimit_ReturnsBlocked()
    {
        await SeedUsage("user-1", null, 300_000, 250_000);

        var (allowed, used, limit) = await _service.CheckUserBudgetAsync("user-1");

        Assert.False(allowed);
        Assert.Equal(550_000, used);
    }

    [Fact]
    public async Task CheckUserBudget_NoUsage_ReturnsAllowed()
    {
        var (allowed, used, limit) = await _service.CheckUserBudgetAsync("new-user");

        Assert.True(allowed);
        Assert.Equal(0, used);
    }

    [Fact]
    public async Task CheckUserBudget_YesterdayUsage_NotCounted()
    {
        // Create a record and backdate it using reflection (InMemory doesn't support raw SQL)
        var record = new TokenUsageRecord("user-1", null, OperationType.Chat, 400_000, 200_000, "model");

        // Use reflection to set CreatedAt to yesterday (private setter)
        typeof(TokenUsageRecord)
            .GetProperty("CreatedAt", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(record, DateTime.UtcNow.AddDays(-1));

        _db.TokenUsageRecords.Add(record);
        await _db.SaveChangesAsync();

        var (allowed, used, _) = await _service.CheckUserBudgetAsync("user-1");

        Assert.True(allowed);
        Assert.Equal(0, used);
    }

    // --- Deal budget ---

    [Fact]
    public async Task CheckDealBudget_UnderLimit_ReturnsAllowed()
    {
        var dealId = Guid.NewGuid();
        await SeedUsage("user-1", dealId, 200_000, 100_000);

        var (allowed, used, limit, warning) = await _service.CheckDealBudgetAsync(dealId);

        Assert.True(allowed);
        Assert.Equal(300_000, used);
        Assert.Equal(1_000_000, limit);
        Assert.False(warning);
    }

    [Fact]
    public async Task CheckDealBudget_At80Percent_ReturnsWarning()
    {
        var dealId = Guid.NewGuid();
        await SeedUsage("user-1", dealId, 500_000, 350_000); // 850K = 85%

        var (allowed, _, _, warning) = await _service.CheckDealBudgetAsync(dealId);

        Assert.True(allowed);
        Assert.True(warning);
    }

    [Fact]
    public async Task CheckDealBudget_OverLimit_ReturnsBlocked()
    {
        var dealId = Guid.NewGuid();
        await SeedUsage("user-1", dealId, 600_000, 500_000); // 1.1M

        var (allowed, used, _, _) = await _service.CheckDealBudgetAsync(dealId);

        Assert.False(allowed);
        Assert.Equal(1_100_000, used);
    }

    [Fact]
    public async Task CheckDealBudget_NoUsage_ReturnsAllowed()
    {
        var (allowed, used, _, warning) = await _service.CheckDealBudgetAsync(Guid.NewGuid());

        Assert.True(allowed);
        Assert.Equal(0, used);
        Assert.False(warning);
    }

    // --- Helper ---

    private async Task SeedUsage(string userId, Guid? dealId, int inputTokens, int outputTokens)
    {
        _db.TokenUsageRecords.Add(
            new TokenUsageRecord(userId, dealId, OperationType.Chat, inputTokens, outputTokens, "model"));
        await _db.SaveChangesAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
