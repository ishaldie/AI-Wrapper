using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class RentRollServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly RentRollService _service;
    private readonly Guid _dealId;

    public RentRollServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"RentRollTests_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _service = new RentRollService(_db);

        // Seed a deal
        _dealId = Guid.NewGuid();
        var deal = new Deal("Test Property", "test-user");
        // Use reflection to set the ID since it's private set
        typeof(Deal).GetProperty("Id")!.SetValue(deal, _dealId);
        _db.Deals.Add(deal);
        _db.SaveChanges();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    // === AddUnit ===

    [Fact]
    public async Task AddUnit_PersistsAndReturnsId()
    {
        var unit = new RentRollUnit(_dealId, "101", 1200m)
        {
            Bedrooms = 2,
            Bathrooms = 1,
            SquareFeet = 850,
            Status = UnitStatus.Occupied,
            TenantName = "John Doe",
            ActualRent = 1150m
        };

        var id = await _service.AddUnitAsync(unit);

        var saved = await _db.RentRollUnits.FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal("101", saved.UnitNumber);
        Assert.Equal(1200m, saved.MarketRent);
        Assert.Equal(1150m, saved.ActualRent);
        Assert.Equal(UnitStatus.Occupied, saved.Status);
        Assert.Equal("John Doe", saved.TenantName);
    }

    // === UpdateUnit ===

    [Fact]
    public async Task UpdateUnit_ChangesFields()
    {
        var unit = new RentRollUnit(_dealId, "102", 1300m);
        _db.RentRollUnits.Add(unit);
        await _db.SaveChangesAsync();

        unit.MarketRent = 1400m;
        unit.Status = UnitStatus.Occupied;
        unit.TenantName = "Jane";
        await _service.UpdateUnitAsync(unit);

        var saved = await _db.RentRollUnits.FindAsync(unit.Id);
        Assert.Equal(1400m, saved!.MarketRent);
        Assert.Equal(UnitStatus.Occupied, saved.Status);
        Assert.Equal("Jane", saved.TenantName);
    }

    // === DeleteUnit ===

    [Fact]
    public async Task DeleteUnit_RemovesFromDb()
    {
        var unit = new RentRollUnit(_dealId, "103", 1100m);
        _db.RentRollUnits.Add(unit);
        await _db.SaveChangesAsync();

        await _service.DeleteUnitAsync(unit.Id);

        Assert.Null(await _db.RentRollUnits.FindAsync(unit.Id));
    }

    [Fact]
    public async Task DeleteUnit_NonExistentId_DoesNotThrow()
    {
        await _service.DeleteUnitAsync(Guid.NewGuid()); // Should not throw
    }

    // === GetUnitsForDeal ===

    [Fact]
    public async Task GetUnitsForDeal_ReturnsOrderedByUnitNumber()
    {
        _db.RentRollUnits.AddRange(
            new RentRollUnit(_dealId, "C-3", 1000m),
            new RentRollUnit(_dealId, "A-1", 1100m),
            new RentRollUnit(_dealId, "B-2", 1200m)
        );
        await _db.SaveChangesAsync();

        var units = await _service.GetUnitsForDealAsync(_dealId);

        Assert.Equal(3, units.Count);
        Assert.Equal("A-1", units[0].UnitNumber);
        Assert.Equal("B-2", units[1].UnitNumber);
        Assert.Equal("C-3", units[2].UnitNumber);
    }

    [Fact]
    public async Task GetUnitsForDeal_DoesNotReturnOtherDealUnits()
    {
        var otherDealId = Guid.NewGuid();
        var otherDeal = new Deal("Other", "test-user");
        typeof(Deal).GetProperty("Id")!.SetValue(otherDeal, otherDealId);
        _db.Deals.Add(otherDeal);

        _db.RentRollUnits.AddRange(
            new RentRollUnit(_dealId, "101", 1000m),
            new RentRollUnit(otherDealId, "201", 1500m)
        );
        await _db.SaveChangesAsync();

        var units = await _service.GetUnitsForDealAsync(_dealId);

        Assert.Single(units);
        Assert.Equal("101", units[0].UnitNumber);
    }

    // === GetSummary ===

    [Fact]
    public async Task GetSummary_EmptyDeal_ReturnsDefaults()
    {
        var summary = await _service.GetSummaryAsync(_dealId);

        Assert.Equal(0, summary.TotalUnits);
        Assert.Equal(0, summary.OccupiedUnits);
        Assert.Equal(0, summary.OccupancyPercent);
    }

    [Fact]
    public async Task GetSummary_CalculatesCorrectMetrics()
    {
        var unit1 = new RentRollUnit(_dealId, "101", 1200m)
        {
            Status = UnitStatus.Occupied,
            ActualRent = 1150m
        };
        var unit2 = new RentRollUnit(_dealId, "102", 1300m)
        {
            Status = UnitStatus.Occupied,
            ActualRent = 1250m
        };
        var unit3 = new RentRollUnit(_dealId, "103", 1100m)
        {
            Status = UnitStatus.Vacant
        };
        _db.RentRollUnits.AddRange(unit1, unit2, unit3);
        await _db.SaveChangesAsync();

        var summary = await _service.GetSummaryAsync(_dealId);

        Assert.Equal(3, summary.TotalUnits);
        Assert.Equal(2, summary.OccupiedUnits);
        Assert.Equal(1, summary.VacantUnits);

        // Occupancy: 2/3 * 100 = 66.67%
        Assert.Equal(66.67m, Math.Round(summary.OccupancyPercent, 2));

        // Average market rent: (1200 + 1300 + 1100) / 3 = 1200
        Assert.Equal(1200m, Math.Round(summary.AverageMarketRent, 2));

        // Average actual rent (only occupied): (1150 + 1250) / 2 = 1200
        Assert.Equal(1200m, Math.Round(summary.AverageActualRent, 2));

        // Total GPR: 1200 + 1300 + 1100 = 3600
        Assert.Equal(3600m, summary.TotalGrossPotentialRent);

        // Total actual rent: 1150 + 1250 = 2400
        Assert.Equal(2400m, summary.TotalActualRent);
    }

    // === BulkAddUnits ===

    [Fact]
    public async Task BulkAddUnits_PersistsAll()
    {
        var units = Enumerable.Range(1, 5).Select(i =>
            new RentRollUnit(_dealId, $"U-{i}", 1000m + i * 100));

        await _service.BulkAddUnitsAsync(units);

        var count = await _db.RentRollUnits.CountAsync(u => u.DealId == _dealId);
        Assert.Equal(5, count);
    }
}
