using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class DealStatusTransitionTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly DealService _service;
    private readonly string _userId = "test-user";

    public DealStatusTransitionTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TransitionTests_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        var logger = LoggerFactory.Create(b => { }).CreateLogger<DealService>();
        _service = new DealService(_db, logger);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task<Deal> SeedDeal(DealStatus initialStatus = DealStatus.Draft)
    {
        var deal = new Deal("Test Property", _userId);
        deal.PropertyName = "Transition Test";
        deal.Address = "1 Test St";
        if (initialStatus != DealStatus.Draft)
            deal.UpdateStatus(initialStatus);
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();
        return deal;
    }

    // === Valid transitions succeed ===

    [Fact]
    public async Task SetStatus_Draft_To_Screening_Succeeds()
    {
        var deal = await SeedDeal();
        await _service.SetStatusAsync(deal.Id, "Screening", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.Screening, updated!.Status);
        Assert.Equal(DealPhase.Acquisition, updated.Phase);
    }

    [Fact]
    public async Task SetStatus_Screening_To_Complete_Succeeds()
    {
        var deal = await SeedDeal(DealStatus.Screening);
        await _service.SetStatusAsync(deal.Id, "Complete", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.Complete, updated!.Status);
    }

    [Fact]
    public async Task SetStatus_Complete_To_UnderContract_Succeeds()
    {
        var deal = await SeedDeal(DealStatus.Complete);
        await _service.SetStatusAsync(deal.Id, "UnderContract", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.UnderContract, updated!.Status);
        Assert.Equal(DealPhase.Contract, updated.Phase);
    }

    [Fact]
    public async Task SetStatus_UnderContract_To_Closed_Succeeds()
    {
        var deal = await SeedDeal(DealStatus.UnderContract);
        await _service.SetStatusAsync(deal.Id, "Closed", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.Closed, updated!.Status);
        Assert.Equal(DealPhase.Ownership, updated.Phase);
    }

    [Fact]
    public async Task SetStatus_Closed_To_Active_Succeeds()
    {
        var deal = await SeedDeal(DealStatus.Closed);
        await _service.SetStatusAsync(deal.Id, "Active", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.Active, updated!.Status);
        Assert.Equal(DealPhase.Ownership, updated.Phase);
    }

    [Fact]
    public async Task SetStatus_Active_To_Disposition_Succeeds()
    {
        var deal = await SeedDeal(DealStatus.Active);
        await _service.SetStatusAsync(deal.Id, "Disposition", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.Disposition, updated!.Status);
        Assert.Equal(DealPhase.Exit, updated.Phase);
    }

    [Fact]
    public async Task SetStatus_Disposition_To_Sold_Succeeds()
    {
        var deal = await SeedDeal(DealStatus.Disposition);
        await _service.SetStatusAsync(deal.Id, "Sold", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.Sold, updated!.Status);
        Assert.Equal(DealPhase.Exit, updated.Phase);
    }

    // === Full lifecycle walk-through ===

    [Fact]
    public async Task FullLifecycle_DraftToSold_Succeeds()
    {
        var deal = await SeedDeal();

        await _service.SetStatusAsync(deal.Id, "Screening", _userId);
        await _service.SetStatusAsync(deal.Id, "Complete", _userId);
        await _service.SetStatusAsync(deal.Id, "UnderContract", _userId);
        await _service.SetStatusAsync(deal.Id, "Closed", _userId);
        await _service.SetStatusAsync(deal.Id, "Active", _userId);
        await _service.SetStatusAsync(deal.Id, "Disposition", _userId);
        await _service.SetStatusAsync(deal.Id, "Sold", _userId);

        var final = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.Sold, final!.Status);
        Assert.Equal(DealPhase.Exit, final.Phase);
    }

    // === Invalid transitions throw ===

    [Fact]
    public async Task SetStatus_Draft_To_Active_Throws()
    {
        var deal = await SeedDeal();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SetStatusAsync(deal.Id, "Active", _userId));
    }

    [Fact]
    public async Task SetStatus_Draft_To_Sold_Throws()
    {
        var deal = await SeedDeal();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SetStatusAsync(deal.Id, "Sold", _userId));
    }

    [Fact]
    public async Task SetStatus_Active_To_Draft_Throws()
    {
        var deal = await SeedDeal(DealStatus.Active);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SetStatusAsync(deal.Id, "Draft", _userId));
    }

    // === Archived from any status ===

    [Theory]
    [InlineData(DealStatus.Draft)]
    [InlineData(DealStatus.Screening)]
    [InlineData(DealStatus.Complete)]
    [InlineData(DealStatus.UnderContract)]
    [InlineData(DealStatus.Active)]
    public async Task SetStatus_ToArchived_FromAny_Succeeds(DealStatus from)
    {
        var deal = await SeedDeal(from);
        await _service.SetStatusAsync(deal.Id, "Archived", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.Archived, updated!.Status);
    }

    // === Re-activate from Archived ===

    [Fact]
    public async Task SetStatus_Archived_To_Draft_Succeeds()
    {
        var deal = await SeedDeal(DealStatus.Archived);
        await _service.SetStatusAsync(deal.Id, "Draft", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.Draft, updated!.Status);
    }

    // === Wrong user cannot transition ===

    [Fact]
    public async Task SetStatus_WrongUser_Throws()
    {
        var deal = await SeedDeal();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SetStatusAsync(deal.Id, "Screening", "wrong-user"));
    }

    // === Backward compat: InProgress still works ===

    [Fact]
    public async Task SetStatus_Draft_To_InProgress_Succeeds()
    {
        var deal = await SeedDeal();
        await _service.SetStatusAsync(deal.Id, "InProgress", _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(DealStatus.InProgress, updated!.Status);
        Assert.Equal(DealPhase.Acquisition, updated.Phase);
    }
}
