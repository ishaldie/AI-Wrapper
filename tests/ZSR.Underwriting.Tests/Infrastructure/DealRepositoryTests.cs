using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Repositories;

namespace ZSR.Underwriting.Tests.Infrastructure;

public class DealRepositoryTests : IDisposable
{
    private readonly AppDbContext _ctx;
    private readonly IDealRepository _repo;
    private readonly IUnitOfWork _uow;

    public DealRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
        _repo = new DealRepository(_ctx);
        _uow = new UnitOfWork(_ctx);
    }

    public void Dispose() => _ctx.Dispose();

    // --- IDealRepository.GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_Returns_Deal_When_Exists()
    {
        var deal = new Deal("Test Deal", "test-user");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(deal.Id, "test-user");

        Assert.NotNull(result);
        Assert.Equal("Test Deal", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid(), "test-user");
        Assert.Null(result);
    }

    // --- IDealRepository.GetByIdWithDetailsAsync ---

    [Fact]
    public async Task GetByIdWithDetailsAsync_Includes_All_Navigation_Properties()
    {
        var deal = new Deal("Full Deal", "test-user");
        deal.Property = new Property("100 Main St", 50) { DealId = deal.Id };
        deal.UnderwritingInput = new UnderwritingInput(5_000_000m) { DealId = deal.Id };
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var calc = new CalculationResult(deal.Id) { NetOperatingIncome = 400_000m };
        _ctx.CalculationResults.Add(calc);
        var report = new UnderwritingReport(deal.Id) { ExecutiveSummary = "Good." };
        _ctx.UnderwritingReports.Add(report);
        await _ctx.SaveChangesAsync();

        var result = await _repo.GetByIdWithDetailsAsync(deal.Id, "test-user");

        Assert.NotNull(result);
        Assert.NotNull(result.Property);
        Assert.NotNull(result.UnderwritingInput);
        Assert.NotNull(result.CalculationResult);
        Assert.NotNull(result.Report);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_Returns_Null_When_Not_Found()
    {
        var result = await _repo.GetByIdWithDetailsAsync(Guid.NewGuid(), "test-user");
        Assert.Null(result);
    }

    // --- IDealRepository.GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_Returns_All_Deals_Ordered_By_CreatedAt_Desc()
    {
        var dealA = new Deal("Deal A", "test-user");
        _ctx.Deals.Add(dealA);
        await _ctx.SaveChangesAsync();

        var dealB = new Deal("Deal B", "test-user");
        _ctx.Deals.Add(dealB);
        await _ctx.SaveChangesAsync();

        var results = await _repo.GetAllAsync("test-user");

        Assert.Equal(2, results.Count);
        Assert.Equal("Deal B", results[0].Name);
        Assert.Equal("Deal A", results[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Empty_When_No_Deals()
    {
        var results = await _repo.GetAllAsync("test-user");
        Assert.Empty(results);
    }

    // --- IDealRepository.GetByStatusAsync ---

    [Fact]
    public async Task GetByStatusAsync_Filters_By_Status()
    {
        var draft = new Deal("Draft Deal", "test-user");
        _ctx.Deals.Add(draft);

        var inProgress = new Deal("Active Deal", "test-user");
        inProgress.UpdateStatus(DealStatus.InProgress);
        _ctx.Deals.Add(inProgress);

        var complete = new Deal("Done Deal", "test-user");
        complete.UpdateStatus(DealStatus.Complete);
        _ctx.Deals.Add(complete);

        await _ctx.SaveChangesAsync();

        var results = await _repo.GetByStatusAsync(DealStatus.InProgress, "test-user");

        Assert.Single(results);
        Assert.Equal("Active Deal", results[0].Name);
    }

    // --- IDealRepository.Add / Remove ---

    [Fact]
    public async Task Add_And_SaveChanges_Persists_Deal()
    {
        var deal = new Deal("New Deal");
        _repo.Add(deal);
        await _uow.SaveChangesAsync();

        Assert.NotNull(await _ctx.Deals.FindAsync(deal.Id));
    }

    [Fact]
    public async Task Remove_And_SaveChanges_Deletes_Deal()
    {
        var deal = new Deal("Doomed Deal");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        _repo.Remove(deal);
        await _uow.SaveChangesAsync();

        Assert.Null(await _ctx.Deals.FindAsync(deal.Id));
    }

    // --- Multi-tenant isolation ---

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Deal_Belongs_To_Different_User()
    {
        var deal = new Deal("Other User Deal", "user-a");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(deal.Id, "user-b");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Deal_When_UserId_Matches()
    {
        var deal = new Deal("My Deal", "user-a");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(deal.Id, "user-a");
        Assert.NotNull(result);
        Assert.Equal("My Deal", result.Name);
    }

    [Fact]
    public async Task GetAllAsync_Only_Returns_Deals_For_Specified_User()
    {
        _ctx.Deals.Add(new Deal("User A Deal 1", "user-a"));
        _ctx.Deals.Add(new Deal("User B Deal", "user-b"));
        _ctx.Deals.Add(new Deal("User A Deal 2", "user-a"));
        await _ctx.SaveChangesAsync();

        var results = await _repo.GetAllAsync("user-a");
        Assert.Equal(2, results.Count);
        Assert.All(results, d => Assert.Equal("user-a", d.UserId));
    }

    [Fact]
    public async Task GetByStatusAsync_Filters_By_Both_Status_And_UserId()
    {
        var draftA = new Deal("Draft A", "user-a");
        _ctx.Deals.Add(draftA);

        var draftB = new Deal("Draft B", "user-b");
        _ctx.Deals.Add(draftB);

        var activeA = new Deal("Active A", "user-a");
        activeA.UpdateStatus(DealStatus.InProgress);
        _ctx.Deals.Add(activeA);

        await _ctx.SaveChangesAsync();

        var results = await _repo.GetByStatusAsync(DealStatus.Draft, "user-a");
        Assert.Single(results);
        Assert.Equal("Draft A", results[0].Name);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_Returns_Null_When_UserId_Mismatch()
    {
        var deal = new Deal("Other Deal", "user-a");
        deal.Property = new Property("100 Main", 10) { DealId = deal.Id };
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var result = await _repo.GetByIdWithDetailsAsync(deal.Id, "user-b");
        Assert.Null(result);
    }

    // --- IUnitOfWork ---

    [Fact]
    public async Task UnitOfWork_SaveChanges_Returns_Affected_Count()
    {
        var deal = new Deal("Count Deal");
        _repo.Add(deal);

        var count = await _uow.SaveChangesAsync();

        Assert.True(count > 0);
    }

    [Fact]
    public void UnitOfWork_Exposes_DealRepository()
    {
        Assert.NotNull(_uow.Deals);
    }
}
