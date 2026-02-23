using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Tests.Infrastructure;

public class AppDbContextTests : IDisposable
{
    private readonly AppDbContext _ctx;

    public AppDbContextTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
    }

    public void Dispose() => _ctx.Dispose();

    // --- DbSet existence ---

    [Fact]
    public void Has_Deals_DbSet()
    {
        Assert.NotNull(_ctx.Deals);
    }

    [Fact]
    public void Has_Properties_DbSet()
    {
        Assert.NotNull(_ctx.Properties);
    }

    [Fact]
    public void Has_UnderwritingInputs_DbSet()
    {
        Assert.NotNull(_ctx.UnderwritingInputs);
    }

    [Fact]
    public void Has_CalculationResults_DbSet()
    {
        Assert.NotNull(_ctx.CalculationResults);
    }

    [Fact]
    public void Has_UnderwritingReports_DbSet()
    {
        Assert.NotNull(_ctx.UnderwritingReports);
    }

    [Fact]
    public void Has_UploadedDocuments_DbSet()
    {
        Assert.NotNull(_ctx.UploadedDocuments);
    }

    // --- Relationships ---

    [Fact]
    public async Task Deal_With_Property_Cascades()
    {
        var deal = new Deal("Test Deal");
        var property = new Property("123 Main St", 50) { DealId = deal.Id };
        deal.Property = property;

        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Deals
            .Include(d => d.Property)
            .FirstAsync(d => d.Id == deal.Id);

        Assert.NotNull(loaded.Property);
        Assert.Equal("123 Main St", loaded.Property.Address);
    }

    [Fact]
    public async Task Deal_With_UnderwritingInput_Cascades()
    {
        var deal = new Deal("Test Deal");
        var input = new UnderwritingInput(5_000_000m) { DealId = deal.Id };
        deal.UnderwritingInput = input;

        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Deals
            .Include(d => d.UnderwritingInput)
            .FirstAsync(d => d.Id == deal.Id);

        Assert.NotNull(loaded.UnderwritingInput);
        Assert.Equal(5_000_000m, loaded.UnderwritingInput.PurchasePrice);
    }

    [Fact]
    public async Task Deal_With_CalculationResult_Cascades()
    {
        var deal = new Deal("Test Deal");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var result = new CalculationResult(deal.Id);
        result.NetOperatingIncome = 500_000m;
        _ctx.CalculationResults.Add(result);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Deals
            .Include(d => d.CalculationResult)
            .FirstAsync(d => d.Id == deal.Id);

        Assert.NotNull(loaded.CalculationResult);
        Assert.Equal(500_000m, loaded.CalculationResult.NetOperatingIncome);
    }

    [Fact]
    public async Task Deal_With_Report_Cascades()
    {
        var deal = new Deal("Test Deal");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var report = new UnderwritingReport(deal.Id);
        report.ExecutiveSummary = "Great deal.";
        _ctx.UnderwritingReports.Add(report);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Deals
            .Include(d => d.Report)
            .FirstAsync(d => d.Id == deal.Id);

        Assert.NotNull(loaded.Report);
        Assert.Equal("Great deal.", loaded.Report.ExecutiveSummary);
    }

    [Fact]
    public async Task Deal_With_UploadedDocuments_Collection()
    {
        var deal = new Deal("Test Deal");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var doc = new UploadedDocument(deal.Id, "file.pdf", "/path/file.pdf", DocumentType.RentRoll, 1024);
        _ctx.UploadedDocuments.Add(doc);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Deals
            .Include(d => d.UploadedDocuments)
            .FirstAsync(d => d.Id == deal.Id);

        Assert.Single(loaded.UploadedDocuments);
    }

    // --- Multi-tenant: Deal.UserId ---

    [Fact]
    public async Task Deal_Persists_UserId()
    {
        var deal = new Deal("Tenant Test", "user-abc");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Deals.FindAsync(deal.Id);
        Assert.NotNull(loaded);
        Assert.Equal("user-abc", loaded.UserId);
    }

    [Fact]
    public async Task Can_Query_Deals_By_UserId()
    {
        var dealA = new Deal("User A Deal", "user-a");
        var dealB = new Deal("User B Deal", "user-b");
        _ctx.Deals.AddRange(dealA, dealB);
        await _ctx.SaveChangesAsync();

        var userADeals = await _ctx.Deals
            .Where(d => d.UserId == "user-a")
            .ToListAsync();

        Assert.Single(userADeals);
        Assert.Equal("User A Deal", userADeals[0].Name);
    }

    // --- Cascade delete ---

    [Fact]
    public async Task Deleting_Deal_Cascades_To_Children()
    {
        var deal = new Deal("Test Deal");
        var property = new Property("123 Main St", 50) { DealId = deal.Id };
        deal.Property = property;
        var input = new UnderwritingInput(5_000_000m) { DealId = deal.Id };
        deal.UnderwritingInput = input;

        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        _ctx.Deals.Remove(deal);
        await _ctx.SaveChangesAsync();

        Assert.Empty(await _ctx.Properties.ToListAsync());
        Assert.Empty(await _ctx.UnderwritingInputs.ToListAsync());
    }

    // --- Index verification ---

    [Fact]
    public async Task Can_Query_Deals_By_Status()
    {
        _ctx.Deals.Add(new Deal("Deal A"));
        var dealB = new Deal("Deal B");
        dealB.UpdateStatus(DealStatus.InProgress);
        _ctx.Deals.Add(dealB);
        await _ctx.SaveChangesAsync();

        var inProgress = await _ctx.Deals
            .Where(d => d.Status == DealStatus.InProgress)
            .ToListAsync();

        Assert.Single(inProgress);
        Assert.Equal("Deal B", inProgress[0].Name);
    }

    // --- CRUD operations ---

    [Fact]
    public async Task Can_Create_And_Read_Deal()
    {
        var deal = new Deal("CRUD Test Deal");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Deals.FindAsync(deal.Id);
        Assert.NotNull(loaded);
        Assert.Equal("CRUD Test Deal", loaded.Name);
        Assert.Equal(DealStatus.Draft, loaded.Status);
    }

    [Fact]
    public async Task Can_Update_Deal_Status()
    {
        var deal = new Deal("Update Test");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        deal.UpdateStatus(DealStatus.Complete);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Deals.FindAsync(deal.Id);
        Assert.NotNull(loaded);
        Assert.Equal(DealStatus.Complete, loaded.Status);
    }

    [Fact]
    public async Task Can_Delete_Deal()
    {
        var deal = new Deal("Delete Test");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        _ctx.Deals.Remove(deal);
        await _ctx.SaveChangesAsync();

        Assert.Null(await _ctx.Deals.FindAsync(deal.Id));
    }

    [Fact]
    public async Task Can_Create_And_Read_Property()
    {
        var deal = new Deal("Prop CRUD");
        var prop = new Property("456 Oak Ave", 100) { DealId = deal.Id, YearBuilt = 2005 };
        deal.Property = prop;
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Properties.FindAsync(prop.Id);
        Assert.NotNull(loaded);
        Assert.Equal("456 Oak Ave", loaded.Address);
        Assert.Equal(100, loaded.UnitCount);
        Assert.Equal(2005, loaded.YearBuilt);
    }

    [Fact]
    public async Task Can_Update_Property_Optional_Fields()
    {
        var deal = new Deal("Prop Update");
        var prop = new Property("789 Elm St", 25) { DealId = deal.Id };
        deal.Property = prop;
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        prop.BuildingType = "Garden-style";
        prop.SquareFootage = 50_000;
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Properties.FindAsync(prop.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Garden-style", loaded.BuildingType);
        Assert.Equal(50_000, loaded.SquareFootage);
    }

    [Fact]
    public async Task Can_Update_CalculationResult_Metrics()
    {
        var deal = new Deal("Calc Update");
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var calc = new CalculationResult(deal.Id) { NetOperatingIncome = 300_000m };
        _ctx.CalculationResults.Add(calc);
        await _ctx.SaveChangesAsync();

        calc.CashOnCashReturn = 0.085m;
        calc.InternalRateOfReturn = 0.15m;
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.CalculationResults.FindAsync(calc.Id);
        Assert.NotNull(loaded);
        Assert.Equal(300_000m, loaded.NetOperatingIncome);
        Assert.Equal(0.085m, loaded.CashOnCashReturn);
        Assert.Equal(0.15m, loaded.InternalRateOfReturn);
    }

    [Fact]
    public async Task Can_Load_Full_Deal_Aggregate()
    {
        var deal = new Deal("Full Aggregate");
        deal.Property = new Property("100 Main St", 50) { DealId = deal.Id };
        deal.UnderwritingInput = new UnderwritingInput(10_000_000m) { DealId = deal.Id };
        _ctx.Deals.Add(deal);
        await _ctx.SaveChangesAsync();

        var calc = new CalculationResult(deal.Id) { NetOperatingIncome = 750_000m };
        _ctx.CalculationResults.Add(calc);
        var report = new UnderwritingReport(deal.Id) { ExecutiveSummary = "Strong deal." };
        _ctx.UnderwritingReports.Add(report);
        var doc = new UploadedDocument(deal.Id, "rent-roll.pdf", "/uploads/rent-roll.pdf", DocumentType.RentRoll, 2048);
        _ctx.UploadedDocuments.Add(doc);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Deals
            .Include(d => d.Property)
            .Include(d => d.UnderwritingInput)
            .Include(d => d.CalculationResult)
            .Include(d => d.Report)
            .Include(d => d.UploadedDocuments)
            .FirstAsync(d => d.Id == deal.Id);

        Assert.NotNull(loaded.Property);
        Assert.NotNull(loaded.UnderwritingInput);
        Assert.NotNull(loaded.CalculationResult);
        Assert.NotNull(loaded.Report);
        Assert.Single(loaded.UploadedDocuments);
        Assert.Equal(10_000_000m, loaded.UnderwritingInput.PurchasePrice);
    }
}
