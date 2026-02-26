using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class CapExServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly CapExService _service;
    private readonly Guid _dealId;

    public CapExServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CapExTests_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _service = new CapExService(_db);

        _dealId = Guid.NewGuid();
        var deal = new Deal("CapEx Test", "test-user");
        typeof(Deal).GetProperty("Id")!.SetValue(deal, _dealId);
        _db.Deals.Add(deal);
        _db.SaveChanges();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    // === Projects CRUD ===

    [Fact]
    public async Task AddProject_PersistsAndReturnsId()
    {
        var project = new CapExProject(_dealId, "Unit Renovations", 250_000m)
        {
            Description = "Interior upgrades",
            UnitsAffected = 20,
            ExpectedRentIncrease = 150m
        };

        var id = await _service.AddProjectAsync(project);

        var saved = await _db.CapExProjects.FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal("Unit Renovations", saved.Name);
        Assert.Equal(250_000m, saved.BudgetAmount);
        Assert.Equal(CapExStatus.Planned, saved.Status);
    }

    [Fact]
    public async Task GetProjects_ReturnsOrderedWithLineItems()
    {
        var p1 = new CapExProject(_dealId, "Roof Replacement", 100_000m);
        var p2 = new CapExProject(_dealId, "Exterior Paint", 50_000m);
        _db.CapExProjects.AddRange(p1, p2);
        await _db.SaveChangesAsync();

        var li = new CapExLineItem(p2.Id, "Paint supplies", 5_000m, DateTime.UtcNow);
        _db.CapExLineItems.Add(li);
        await _db.SaveChangesAsync();

        var projects = await _service.GetProjectsAsync(_dealId);

        Assert.Equal(2, projects.Count);
        Assert.Equal("Exterior Paint", projects[0].Name); // Alphabetical
        Assert.Single(projects[0].LineItems);
    }

    [Fact]
    public async Task UpdateProject_PersistsChanges()
    {
        var project = new CapExProject(_dealId, "HVAC", 80_000m);
        _db.CapExProjects.Add(project);
        await _db.SaveChangesAsync();

        project.Status = CapExStatus.InProgress;
        project.StartDate = new DateTime(2026, 2, 1);
        await _service.UpdateProjectAsync(project);

        var saved = await _db.CapExProjects.FindAsync(project.Id);
        Assert.Equal(CapExStatus.InProgress, saved!.Status);
        Assert.Equal(new DateTime(2026, 2, 1), saved.StartDate);
    }

    [Fact]
    public async Task DeleteProject_RemovesProjectAndLineItems()
    {
        var project = new CapExProject(_dealId, "Landscaping", 30_000m);
        _db.CapExProjects.Add(project);
        await _db.SaveChangesAsync();

        var li = new CapExLineItem(project.Id, "Trees", 5_000m, DateTime.UtcNow);
        _db.CapExLineItems.Add(li);
        await _db.SaveChangesAsync();

        await _service.DeleteProjectAsync(project.Id);

        Assert.Null(await _db.CapExProjects.FindAsync(project.Id));
        Assert.Empty(await _db.CapExLineItems.Where(l => l.CapExProjectId == project.Id).ToListAsync());
    }

    // === Line Items ===

    [Fact]
    public async Task AddLineItem_RecalculatesProjectSpend()
    {
        var project = new CapExProject(_dealId, "Pool", 50_000m);
        _db.CapExProjects.Add(project);
        await _db.SaveChangesAsync();

        await _service.AddLineItemAsync(new CapExLineItem(project.Id, "Excavation", 15_000m, DateTime.UtcNow));
        await _service.AddLineItemAsync(new CapExLineItem(project.Id, "Concrete", 20_000m, DateTime.UtcNow));

        var saved = await _db.CapExProjects.FindAsync(project.Id);
        Assert.Equal(35_000m, saved!.ActualSpend);
    }

    [Fact]
    public async Task DeleteLineItem_RecalculatesProjectSpend()
    {
        var project = new CapExProject(_dealId, "Gym", 40_000m);
        _db.CapExProjects.Add(project);
        await _db.SaveChangesAsync();

        var li1 = new CapExLineItem(project.Id, "Equipment", 25_000m, DateTime.UtcNow);
        var li2 = new CapExLineItem(project.Id, "Flooring", 10_000m, DateTime.UtcNow);
        _db.CapExLineItems.AddRange(li1, li2);
        project.ActualSpend = 35_000m;
        await _db.SaveChangesAsync();

        await _service.DeleteLineItemAsync(li1.Id);

        var saved = await _db.CapExProjects.Include(p => p.LineItems).FirstAsync(p => p.Id == project.Id);
        Assert.Equal(10_000m, saved.ActualSpend);
    }

    // === Totals ===

    [Fact]
    public async Task GetTotalBudget_SumsAllProjects()
    {
        _db.CapExProjects.AddRange(
            new CapExProject(_dealId, "Project A", 100_000m),
            new CapExProject(_dealId, "Project B", 200_000m)
        );
        await _db.SaveChangesAsync();

        var total = await _service.GetTotalBudgetAsync(_dealId);
        Assert.Equal(300_000m, total);
    }

    [Fact]
    public async Task GetTotalSpend_SumsActualSpend()
    {
        var p1 = new CapExProject(_dealId, "P1", 100_000m) { ActualSpend = 50_000m };
        var p2 = new CapExProject(_dealId, "P2", 200_000m) { ActualSpend = 75_000m };
        _db.CapExProjects.AddRange(p1, p2);
        await _db.SaveChangesAsync();

        var total = await _service.GetTotalSpendAsync(_dealId);
        Assert.Equal(125_000m, total);
    }

    // === Domain entity ===

    [Fact]
    public void CapExProject_BudgetVariance_CalculatesCorrectly()
    {
        var project = new CapExProject(_dealId, "Test", 100_000m) { ActualSpend = 110_000m };
        Assert.Equal(10_000m, project.BudgetVariance);
    }

    [Fact]
    public void CapExProject_BudgetUtilization_CalculatesCorrectly()
    {
        var project = new CapExProject(_dealId, "Test", 200_000m) { ActualSpend = 100_000m };
        Assert.Equal(50m, project.BudgetUtilizationPercent);
    }

    [Fact]
    public void CapExProject_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CapExProject(_dealId, "", 100_000m));
    }
}
