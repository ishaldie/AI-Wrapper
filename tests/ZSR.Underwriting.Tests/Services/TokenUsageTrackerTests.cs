using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class TokenUsageTrackerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TokenUsageTracker _tracker;

    public TokenUsageTrackerTests()
    {
        var dbName = $"TokenTracker_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _db = new AppDbContext(options);
        _tracker = new TokenUsageTracker(_db, NullLogger<TokenUsageTracker>.Instance);
    }

    [Fact]
    public async Task RecordUsageAsync_PersistsRecord()
    {
        await _tracker.RecordUsageAsync(
            "user-1", Guid.NewGuid(), OperationType.Chat, 100, 50, "claude-sonnet");

        var records = await _db.TokenUsageRecords.ToListAsync();
        Assert.Single(records);

        var r = records[0];
        Assert.Equal("user-1", r.UserId);
        Assert.Equal(OperationType.Chat, r.OperationType);
        Assert.Equal(100, r.InputTokens);
        Assert.Equal(50, r.OutputTokens);
        Assert.Equal("claude-sonnet", r.Model);
    }

    [Fact]
    public async Task RecordUsageAsync_WithNullDealId_PersistsRecord()
    {
        await _tracker.RecordUsageAsync(
            "user-1", null, OperationType.QuickAnalysis, 200, 100, "claude-sonnet");

        var record = await _db.TokenUsageRecords.SingleAsync();
        Assert.Null(record.DealId);
        Assert.Equal(OperationType.QuickAnalysis, record.OperationType);
    }

    [Fact]
    public async Task RecordUsageAsync_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;

        await _tracker.RecordUsageAsync(
            "user-1", null, OperationType.Chat, 10, 5, "claude-sonnet");

        var record = await _db.TokenUsageRecords.SingleAsync();
        Assert.True(record.CreatedAt >= before);
        Assert.True(record.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task RecordUsageAsync_AllOperationTypes_Persist()
    {
        var dealId = Guid.NewGuid();
        foreach (var opType in Enum.GetValues<OperationType>())
        {
            await _tracker.RecordUsageAsync("user-1", dealId, opType, 10, 5, "model");
        }

        var count = await _db.TokenUsageRecords.CountAsync();
        Assert.Equal(Enum.GetValues<OperationType>().Length, count);
    }

    [Fact]
    public async Task RecordUsageAsync_DoesNotThrow_OnDbFailure()
    {
        // Dispose the context to simulate a failure scenario
        _db.Dispose();

        // Should not throw — fire-and-forget pattern
        var exception = await Record.ExceptionAsync(() =>
            _tracker.RecordUsageAsync("user-1", null, OperationType.Chat, 10, 5, "model"));

        Assert.Null(exception);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
