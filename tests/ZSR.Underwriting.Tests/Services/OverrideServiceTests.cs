using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class OverrideServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly OverrideService _sut;

    public OverrideServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new OverrideService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ApplyOverrides_RentRoll_UpdatesDealFields()
    {
        var deal = await SeedDealAsync();
        var parsed = new ParsedDocumentResult
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.RentRoll,
            Success = true,
            TotalMonthlyRent = 85000m,
            OccupancyRate = 94.5m,
            UnitCount = 100,
        };

        var result = await _sut.ApplyOverridesAsync(deal.Id, parsed);

        Assert.True(result.Success);
        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(85000m * 12, updated!.RentRollSummary);
        Assert.Equal(94.5m, updated.TargetOccupancy);
        Assert.Equal(100, updated.UnitCount);
    }

    [Fact]
    public async Task ApplyOverrides_T12_UpdatesDealFields()
    {
        var deal = await SeedDealAsync();
        var parsed = new ParsedDocumentResult
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.T12PAndL,
            Success = true,
            NetOperatingIncome = 780000m,
        };

        var result = await _sut.ApplyOverridesAsync(deal.Id, parsed);

        Assert.True(result.Success);
        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(780000m, updated!.T12Summary);
    }

    [Fact]
    public async Task ApplyOverrides_LoanTermSheet_UpdatesDealFields()
    {
        var deal = await SeedDealAsync();
        var parsed = new ParsedDocumentResult
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.LoanTermSheet,
            Success = true,
            LtvRatio = 75m,
            InterestRate = 5.25m,
            IsInterestOnly = true,
            AmortizationYears = 30,
            LoanTermYears = 10,
        };

        var result = await _sut.ApplyOverridesAsync(deal.Id, parsed);

        Assert.True(result.Success);
        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(75m, updated!.LoanLtv);
        Assert.Equal(5.25m, updated.LoanRate);
        Assert.True(updated.IsInterestOnly);
        Assert.Equal(30, updated.AmortizationYears);
        Assert.Equal(10, updated.LoanTermYears);
    }

    [Fact]
    public async Task ApplyOverrides_CreatesFieldOverrideRecords()
    {
        var deal = await SeedDealAsync();
        deal.LoanRate = 4.0m;
        await _db.SaveChangesAsync();

        var parsed = new ParsedDocumentResult
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.LoanTermSheet,
            Success = true,
            InterestRate = 5.25m,
        };

        await _sut.ApplyOverridesAsync(deal.Id, parsed);

        var overrides = await _db.FieldOverrides.Where(f => f.DealId == deal.Id).ToListAsync();
        Assert.Contains(overrides, o => o.FieldName == "LoanRate" && o.NewValue == "5.25");
    }

    [Fact]
    public async Task ApplyOverrides_FieldOverrideHasSourceAttribution()
    {
        var deal = await SeedDealAsync();
        var parsed = new ParsedDocumentResult
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.RentRoll,
            Success = true,
            TotalMonthlyRent = 50000m,
        };

        await _sut.ApplyOverridesAsync(deal.Id, parsed);

        var overrides = await _db.FieldOverrides.Where(f => f.DealId == deal.Id).ToListAsync();
        Assert.All(overrides, o => Assert.Equal("User-Provided: RentRoll", o.Source));
    }

    [Fact]
    public async Task ApplyOverrides_FailedParsedResult_ReturnsError()
    {
        var deal = await SeedDealAsync();
        var parsed = new ParsedDocumentResult
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.RentRoll,
            Success = false,
            ErrorMessage = "Parse failed",
        };

        var result = await _sut.ApplyOverridesAsync(deal.Id, parsed);

        Assert.False(result.Success);
        Assert.Contains("Parse failed", result.ErrorMessage);
    }

    [Fact]
    public async Task ApplyOverrides_DealNotFound_ReturnsError()
    {
        var parsed = new ParsedDocumentResult
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.RentRoll,
            Success = true,
            TotalMonthlyRent = 50000m,
        };

        var result = await _sut.ApplyOverridesAsync(Guid.NewGuid(), parsed);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task GetOverridesForDeal_ReturnsList()
    {
        var deal = await SeedDealAsync();
        var parsed = new ParsedDocumentResult
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.LoanTermSheet,
            Success = true,
            LtvRatio = 75m,
            InterestRate = 5.25m,
        };

        await _sut.ApplyOverridesAsync(deal.Id, parsed);
        var overrides = await _sut.GetOverridesForDealAsync(deal.Id);

        Assert.Equal(2, overrides.Count);
    }

    [Fact]
    public async Task ApplyOverrides_SkipsNullFields()
    {
        var deal = await SeedDealAsync();
        deal.LoanRate = 4.0m;
        await _db.SaveChangesAsync();

        var parsed = new ParsedDocumentResult
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = DocumentType.LoanTermSheet,
            Success = true,
            // Only InterestRate set, all others null
            InterestRate = 5.25m,
        };

        await _sut.ApplyOverridesAsync(deal.Id, parsed);

        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(5.25m, updated!.LoanRate);
        // Other fields should remain unchanged
        Assert.Null(updated.LoanLtv);
    }

    private async Task<Deal> SeedDealAsync()
    {
        var deal = new Deal("Test Deal");
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();
        return deal;
    }
}
