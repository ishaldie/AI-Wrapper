using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class ContractServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly ContractService _service;
    private readonly Guid _dealId;

    public ContractServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ContractTests_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _service = new ContractService(_db);

        // Seed a deal
        _dealId = Guid.NewGuid();
        var deal = new Deal("Contract Test Property", "test-user");
        typeof(Deal).GetProperty("Id")!.SetValue(deal, _dealId);
        _db.Deals.Add(deal);
        _db.SaveChanges();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    // === GetOrCreateTimeline ===

    [Fact]
    public async Task GetOrCreateTimeline_CreatesNew_WhenNoneExists()
    {
        var timeline = await _service.GetOrCreateTimelineAsync(_dealId);

        Assert.NotNull(timeline);
        Assert.Equal(_dealId, timeline.DealId);
        Assert.Single(await _db.ContractTimelines.ToListAsync());
    }

    [Fact]
    public async Task GetOrCreateTimeline_ReturnsExisting_WhenAlreadyCreated()
    {
        var first = await _service.GetOrCreateTimelineAsync(_dealId);
        var second = await _service.GetOrCreateTimelineAsync(_dealId);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(await _db.ContractTimelines.ToListAsync());
    }

    // === UpdateTimeline ===

    [Fact]
    public async Task UpdateTimeline_PersistsChanges()
    {
        var timeline = await _service.GetOrCreateTimelineAsync(_dealId);

        timeline.PsaExecutedDate = new DateTime(2026, 1, 15);
        timeline.ClosingDate = new DateTime(2026, 3, 1);
        timeline.EarnestMoneyDeposit = 50_000m;
        timeline.LenderName = "Freddie Mac";
        timeline.TitleCompany = "First American";

        await _service.UpdateTimelineAsync(timeline);

        var saved = await _db.ContractTimelines.FindAsync(timeline.Id);
        Assert.Equal(new DateTime(2026, 1, 15), saved!.PsaExecutedDate);
        Assert.Equal(new DateTime(2026, 3, 1), saved.ClosingDate);
        Assert.Equal(50_000m, saved.EarnestMoneyDeposit);
        Assert.Equal("Freddie Mac", saved.LenderName);
        Assert.Equal("First American", saved.TitleCompany);
    }

    // === Closing Costs CRUD ===

    [Fact]
    public async Task AddClosingCost_PersistsAndReturnsId()
    {
        var item = new ClosingCostItem(_dealId, "Title", "Title Insurance", 5_000m);
        var id = await _service.AddClosingCostAsync(item);

        var saved = await _db.ClosingCostItems.FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal("Title", saved.Category);
        Assert.Equal("Title Insurance", saved.Description);
        Assert.Equal(5_000m, saved.EstimatedAmount);
    }

    [Fact]
    public async Task GetClosingCosts_ReturnsOrderedByCategoryThenDescription()
    {
        _db.ClosingCostItems.AddRange(
            new ClosingCostItem(_dealId, "Survey", "Alta Survey", 3_000m),
            new ClosingCostItem(_dealId, "Legal", "Closing Attorney", 2_500m),
            new ClosingCostItem(_dealId, "Legal", "Contract Review", 1_000m),
            new ClosingCostItem(_dealId, "Title", "Title Insurance", 5_000m)
        );
        await _db.SaveChangesAsync();

        var costs = await _service.GetClosingCostsAsync(_dealId);

        Assert.Equal(4, costs.Count);
        Assert.Equal("Legal", costs[0].Category);
        Assert.Equal("Closing Attorney", costs[0].Description);
        Assert.Equal("Legal", costs[1].Category);
        Assert.Equal("Contract Review", costs[1].Description);
        Assert.Equal("Survey", costs[2].Category);
        Assert.Equal("Title", costs[3].Category);
    }

    [Fact]
    public async Task UpdateClosingCost_PersistsChanges()
    {
        var item = new ClosingCostItem(_dealId, "Lender", "Origination Fee", 10_000m);
        _db.ClosingCostItems.Add(item);
        await _db.SaveChangesAsync();

        item.ActualAmount = 9_500m;
        item.IsPaid = true;
        await _service.UpdateClosingCostAsync(item);

        var saved = await _db.ClosingCostItems.FindAsync(item.Id);
        Assert.Equal(9_500m, saved!.ActualAmount);
        Assert.True(saved.IsPaid);
    }

    [Fact]
    public async Task DeleteClosingCost_RemovesItem()
    {
        var item = new ClosingCostItem(_dealId, "Other", "Misc", 500m);
        _db.ClosingCostItems.Add(item);
        await _db.SaveChangesAsync();

        await _service.DeleteClosingCostAsync(item.Id);

        Assert.Null(await _db.ClosingCostItems.FindAsync(item.Id));
    }

    [Fact]
    public async Task DeleteClosingCost_NonExistent_DoesNotThrow()
    {
        await _service.DeleteClosingCostAsync(Guid.NewGuid());
    }

    // === Totals ===

    [Fact]
    public async Task GetTotalEstimatedClosingCosts_SumsAll()
    {
        _db.ClosingCostItems.AddRange(
            new ClosingCostItem(_dealId, "Title", "Title Insurance", 5_000m),
            new ClosingCostItem(_dealId, "Legal", "Attorney", 2_500m),
            new ClosingCostItem(_dealId, "Survey", "Alta Survey", 3_000m)
        );
        await _db.SaveChangesAsync();

        var total = await _service.GetTotalEstimatedClosingCostsAsync(_dealId);

        Assert.Equal(10_500m, total);
    }

    [Fact]
    public async Task GetTotalActualClosingCosts_OnlySumsNonNull()
    {
        var item1 = new ClosingCostItem(_dealId, "Title", "Title Insurance", 5_000m) { ActualAmount = 4_800m };
        var item2 = new ClosingCostItem(_dealId, "Legal", "Attorney", 2_500m) { ActualAmount = 2_500m };
        var item3 = new ClosingCostItem(_dealId, "Survey", "Survey", 3_000m); // No actual yet
        _db.ClosingCostItems.AddRange(item1, item2, item3);
        await _db.SaveChangesAsync();

        var total = await _service.GetTotalActualClosingCostsAsync(_dealId);

        Assert.Equal(7_300m, total);
    }

    [Fact]
    public async Task GetTotalEstimated_NoCosts_ReturnsZero()
    {
        var total = await _service.GetTotalEstimatedClosingCostsAsync(_dealId);
        Assert.Equal(0m, total);
    }

    // === ContractTimeline.GetNextDeadline ===

    [Fact]
    public void GetNextDeadline_ReturnsNearestFuture()
    {
        var timeline = new ContractTimeline(_dealId)
        {
            InspectionDeadline = DateTime.UtcNow.AddDays(-5),  // Past
            FinancingContingencyDate = DateTime.UtcNow.AddDays(10),
            AppraisalDeadline = DateTime.UtcNow.AddDays(5),    // Nearest future
            ClosingDate = DateTime.UtcNow.AddDays(30)
        };

        var next = timeline.GetNextDeadline();

        Assert.NotNull(next);
        Assert.Equal("Appraisal", next.Value.Name);
    }

    [Fact]
    public void GetNextDeadline_AllPast_ReturnsNull()
    {
        var timeline = new ContractTimeline(_dealId)
        {
            InspectionDeadline = DateTime.UtcNow.AddDays(-10),
            ClosingDate = DateTime.UtcNow.AddDays(-1)
        };

        Assert.Null(timeline.GetNextDeadline());
    }

    [Fact]
    public void GetNextDeadline_AllNull_ReturnsNull()
    {
        var timeline = new ContractTimeline(_dealId);
        Assert.Null(timeline.GetNextDeadline());
    }
}
