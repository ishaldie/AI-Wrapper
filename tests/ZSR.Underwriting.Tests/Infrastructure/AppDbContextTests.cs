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
    public void Has_RealAiData_DbSet()
    {
        Assert.NotNull(_ctx.RealAiDataSets);
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
}
