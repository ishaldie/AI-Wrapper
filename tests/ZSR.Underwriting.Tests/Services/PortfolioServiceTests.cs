using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class PortfolioServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly PortfolioService _service;
    private readonly string _userId = "portfolio-test-user";

    public PortfolioServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"PortfolioTests_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _service = new PortfolioService(_db);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    // === Create ===

    [Fact]
    public async Task Create_ReturnsGuid_And_PersistsPortfolio()
    {
        var id = await _service.CreateAsync("Fund I", _userId, "Value-Add", 2025);

        var portfolio = await _db.Portfolios.FindAsync(id);
        Assert.NotNull(portfolio);
        Assert.Equal("Fund I", portfolio.Name);
        Assert.Equal(_userId, portfolio.UserId);
        Assert.Equal("Value-Add", portfolio.Strategy);
        Assert.Equal(2025, portfolio.VintageYear);
    }

    [Fact]
    public async Task Create_WithMinimalParams_Works()
    {
        var id = await _service.CreateAsync("Simple Portfolio", _userId);

        var portfolio = await _db.Portfolios.FindAsync(id);
        Assert.NotNull(portfolio);
        Assert.Null(portfolio.Strategy);
        Assert.Null(portfolio.VintageYear);
    }

    // === Update ===

    [Fact]
    public async Task Update_ChangesNameAndStrategy()
    {
        var id = await _service.CreateAsync("Old Name", _userId);

        await _service.UpdateAsync(id, "New Name", _userId, "A description", "Core-Plus", 2024);

        var portfolio = await _db.Portfolios.FindAsync(id);
        Assert.Equal("New Name", portfolio!.Name);
        Assert.Equal("A description", portfolio.Description);
        Assert.Equal("Core-Plus", portfolio.Strategy);
        Assert.Equal(2024, portfolio.VintageYear);
    }

    [Fact]
    public async Task Update_WrongUser_Throws()
    {
        var id = await _service.CreateAsync("My Fund", _userId);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(id, "Hacked", "wrong-user"));
    }

    // === Delete ===

    [Fact]
    public async Task Delete_RemovesPortfolio()
    {
        var id = await _service.CreateAsync("To Delete", _userId);
        await _service.DeleteAsync(id, _userId);

        Assert.Null(await _db.Portfolios.FindAsync(id));
    }

    [Fact]
    public async Task Delete_UnassignsDeals()
    {
        var portfolioId = await _service.CreateAsync("Fund", _userId);
        var deal = new Deal("Property", _userId) { PortfolioId = portfolioId };
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        await _service.DeleteAsync(portfolioId, _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Null(updated!.PortfolioId);
    }

    [Fact]
    public async Task Delete_WrongUser_Throws()
    {
        var id = await _service.CreateAsync("My Fund", _userId);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteAsync(id, "wrong-user"));
    }

    // === GetAll ===

    [Fact]
    public async Task GetAll_ReturnsOnlyUserPortfolios()
    {
        await _service.CreateAsync("User Fund", _userId);
        await _service.CreateAsync("Other Fund", "other-user");

        var result = await _service.GetAllAsync(_userId);

        Assert.Single(result);
        Assert.Equal("User Fund", result[0].Name);
    }

    [Fact]
    public async Task GetAll_IncludesDealAggregates()
    {
        var portfolioId = await _service.CreateAsync("Fund", _userId);
        var deal1 = new Deal("Prop 1", _userId) { PortfolioId = portfolioId, UnitCount = 50, PurchasePrice = 5_000_000m };
        deal1.UpdateStatus(DealStatus.Active);
        var deal2 = new Deal("Prop 2", _userId) { PortfolioId = portfolioId, UnitCount = 30, PurchasePrice = 3_000_000m };
        _db.Deals.AddRange(deal1, deal2);
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync(_userId);

        Assert.Single(result);
        Assert.Equal(2, result[0].DealCount);
        Assert.Equal(80, result[0].TotalUnits);
        Assert.Equal(8_000_000m, result[0].TotalAum);
        Assert.Equal(1, result[0].ActiveAssetCount); // Only deal1 is Active
    }

    // === GetById ===

    [Fact]
    public async Task GetById_ReturnsCorrectPortfolio()
    {
        var id = await _service.CreateAsync("Target", _userId, "Opportunistic");

        var result = await _service.GetByIdAsync(id, _userId);

        Assert.NotNull(result);
        Assert.Equal("Target", result.Name);
        Assert.Equal("Opportunistic", result.Strategy);
    }

    [Fact]
    public async Task GetById_WrongUser_ReturnsNull()
    {
        var id = await _service.CreateAsync("Secret", _userId);
        var result = await _service.GetByIdAsync(id, "other-user");
        Assert.Null(result);
    }

    // === Deal Assignment ===

    [Fact]
    public async Task AssignDeal_SetsPortfolioId()
    {
        var portfolioId = await _service.CreateAsync("Fund", _userId);
        var deal = new Deal("Property", _userId);
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        await _service.AssignDealAsync(portfolioId, deal.Id, _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(portfolioId, updated!.PortfolioId);
    }

    [Fact]
    public async Task AssignDeal_WrongUser_Throws()
    {
        var portfolioId = await _service.CreateAsync("Fund", _userId);
        var deal = new Deal("Property", _userId);
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AssignDealAsync(portfolioId, deal.Id, "wrong-user"));
    }

    // === Remove Deal ===

    [Fact]
    public async Task RemoveDeal_ClearsPortfolioId()
    {
        var portfolioId = await _service.CreateAsync("Fund", _userId);
        var deal = new Deal("Property", _userId) { PortfolioId = portfolioId };
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        await _service.RemoveDealAsync(deal.Id, _userId);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Null(updated!.PortfolioId);
    }

    [Fact]
    public async Task RemoveDeal_WrongUser_Throws()
    {
        var deal = new Deal("Property", _userId);
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.RemoveDealAsync(deal.Id, "wrong-user"));
    }
}
