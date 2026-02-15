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
        var deal = new Deal("Test Deal");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(deal.Id);

        Assert.NotNull(result);
        Assert.Equal("Test Deal", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // --- IDealRepository.GetByIdWithDetailsAsync ---

    [Fact]
    public async Task GetByIdWithDetailsAsync_Includes_All_Navigation_Properties()
    {
        var deal = new Deal("Full Deal");
        deal.Property = new Property("100 Main St", 50) { DealId = deal.Id };
        deal.UnderwritingInput = new UnderwritingInput(5_000_000m) { DealId = deal.Id };
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var calc = new CalculationResult(deal.Id) { NetOperatingIncome = 400_000m };
        _ctx.CalculationResults.Add(calc);
        var report = new UnderwritingReport(deal.Id) { ExecutiveSummary = "Good." };
        _ctx.UnderwritingReports.Add(report);
        await _ctx.SaveChangesAsync();

        var result = await _repo.GetByIdWithDetailsAsync(deal.Id);

        Assert.NotNull(result);
        Assert.NotNull(result.Property);
        Assert.NotNull(result.UnderwritingInput);
        Assert.NotNull(result.CalculationResult);
        Assert.NotNull(result.Report);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_Returns_Null_When_Not_Found()
    {
        var result = await _repo.GetByIdWithDetailsAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // --- IDealRepository.GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_Returns_All_Deals_Ordered_By_CreatedAt_Desc()
    {
        var dealA = new Deal("Deal A");
        _ctx.Deals.Add(dealA);
        await _ctx.SaveChangesAsync();

        var dealB = new Deal("Deal B");
        _ctx.Deals.Add(dealB);
        await _ctx.SaveChangesAsync();

        var results = await _repo.GetAllAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal("Deal B", results[0].Name);
        Assert.Equal("Deal A", results[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Empty_When_No_Deals()
    {
        var results = await _repo.GetAllAsync();
        Assert.Empty(results);
    }

    // --- IDealRepository.GetByStatusAsync ---

    [Fact]
    public async Task GetByStatusAsync_Filters_By_Status()
    {
        var draft = new Deal("Draft Deal");
        _ctx.Deals.Add(draft);

        var inProgress = new Deal("Active Deal");
        inProgress.UpdateStatus(DealStatus.InProgress);
        _ctx.Deals.Add(inProgress);

        var complete = new Deal("Done Deal");
        complete.UpdateStatus(DealStatus.Complete);
        _ctx.Deals.Add(complete);

        await _ctx.SaveChangesAsync();

        var results = await _repo.GetByStatusAsync(DealStatus.InProgress);

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
