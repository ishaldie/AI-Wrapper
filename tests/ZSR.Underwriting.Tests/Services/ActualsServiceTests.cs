using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class ActualsServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly ActualsService _service;
    private readonly Guid _dealId;

    public ActualsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ActualsTests_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _service = new ActualsService(_db);

        _dealId = Guid.NewGuid();
        var deal = new Deal("Actuals Test", "test-user");
        typeof(Deal).GetProperty("Id")!.SetValue(deal, _dealId);
        _db.Deals.Add(deal);
        _db.SaveChanges();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private MonthlyActual CreateActual(int year, int month, decimal gri = 50000m, decimal expenses = 20000m)
    {
        var actual = new MonthlyActual(_dealId, year, month)
        {
            GrossRentalIncome = gri,
            VacancyLoss = gri * 0.05m,
            OtherIncome = 1000m,
            PropertyTaxes = expenses * 0.25m,
            Insurance = expenses * 0.1m,
            Utilities = expenses * 0.15m,
            Repairs = expenses * 0.1m,
            Management = expenses * 0.2m,
            Payroll = expenses * 0.1m,
            Marketing = expenses * 0.05m,
            Administrative = expenses * 0.03m,
            OtherExpenses = expenses * 0.02m,
            DebtService = 15000m,
            OccupiedUnits = 45,
            TotalUnits = 50
        };
        return actual;
    }

    [Fact]
    public async Task Save_NewActual_PersistsWithRecalculation()
    {
        var actual = CreateActual(2026, 1);
        var id = await _service.SaveAsync(actual);

        var saved = await _db.MonthlyActuals.FindAsync(id);
        Assert.NotNull(saved);
        Assert.Equal(2026, saved.Year);
        Assert.Equal(1, saved.Month);
        Assert.True(saved.EffectiveGrossIncome > 0);
        Assert.True(saved.NetOperatingIncome > 0);
        Assert.Equal(90m, saved.OccupancyPercent);
    }

    [Fact]
    public async Task Save_ExistingActual_UpdatesInPlace()
    {
        var actual = CreateActual(2026, 2);
        var id1 = await _service.SaveAsync(actual);

        var updated = CreateActual(2026, 2, gri: 55000m);
        var id2 = await _service.SaveAsync(updated);

        Assert.Equal(id1, id2); // Same record updated
        var saved = await _db.MonthlyActuals.FindAsync(id1);
        Assert.Equal(55000m, saved!.GrossRentalIncome);
    }

    [Fact]
    public async Task GetAsync_ReturnsCorrectMonth()
    {
        await _service.SaveAsync(CreateActual(2026, 3));
        await _service.SaveAsync(CreateActual(2026, 4));

        var result = await _service.GetAsync(_dealId, 2026, 3);

        Assert.NotNull(result);
        Assert.Equal(3, result.Month);
    }

    [Fact]
    public async Task GetAsync_NonExistent_ReturnsNull()
    {
        var result = await _service.GetAsync(_dealId, 2099, 12);
        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_RemovesActual()
    {
        var actual = CreateActual(2026, 5);
        var id = await _service.SaveAsync(actual);

        await _service.DeleteAsync(id);

        Assert.Null(await _db.MonthlyActuals.FindAsync(id));
    }

    [Fact]
    public async Task GetForYear_ReturnsOrderedByMonth()
    {
        await _service.SaveAsync(CreateActual(2026, 6));
        await _service.SaveAsync(CreateActual(2026, 1));
        await _service.SaveAsync(CreateActual(2026, 3));
        await _service.SaveAsync(CreateActual(2025, 12)); // Different year

        var result = await _service.GetForYearAsync(_dealId, 2026);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Month);
        Assert.Equal(3, result[1].Month);
        Assert.Equal(6, result[2].Month);
    }

    [Fact]
    public async Task GetAnnualSummary_AggregatesCorrectly()
    {
        for (int m = 1; m <= 6; m++)
            await _service.SaveAsync(CreateActual(2026, m));

        var summary = await _service.GetAnnualSummaryAsync(_dealId, 2026);

        Assert.Equal(2026, summary.Year);
        Assert.Equal(6, summary.MonthsReported);
        Assert.True(summary.TotalRevenue > 0);
        Assert.True(summary.TotalExpenses > 0);
        Assert.True(summary.TotalNoi > 0);
        Assert.Equal(90m, summary.AverageOccupancy);
    }

    [Fact]
    public async Task GetAnnualSummary_EmptyYear_ReturnsDefaults()
    {
        var summary = await _service.GetAnnualSummaryAsync(_dealId, 2030);

        Assert.Equal(0, summary.MonthsReported);
        Assert.Equal(0m, summary.TotalRevenue);
    }

    [Fact]
    public void MonthlyActual_Recalculate_ComputesCorrectly()
    {
        var actual = new MonthlyActual(_dealId, 2026, 1)
        {
            GrossRentalIncome = 100_000m,
            VacancyLoss = 5_000m,
            OtherIncome = 2_000m,
            PropertyTaxes = 8_000m,
            Insurance = 3_000m,
            Utilities = 4_000m,
            Repairs = 2_000m,
            Management = 5_000m,
            Payroll = 6_000m,
            Marketing = 1_000m,
            Administrative = 500m,
            OtherExpenses = 500m,
            DebtService = 25_000m,
            CapitalExpenditures = 5_000m,
            OccupiedUnits = 90,
            TotalUnits = 100
        };

        actual.Recalculate();

        Assert.Equal(97_000m, actual.EffectiveGrossIncome);   // 100k - 5k + 2k
        Assert.Equal(30_000m, actual.TotalOperatingExpenses);  // Sum of all expenses
        Assert.Equal(67_000m, actual.NetOperatingIncome);      // 97k - 30k
        Assert.Equal(37_000m, actual.CashFlow);                // 67k - 25k - 5k
        Assert.Equal(90m, actual.OccupancyPercent);            // 90/100 * 100
    }

    [Fact]
    public void MonthlyActual_InvalidMonth_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonthlyActual(_dealId, 2026, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonthlyActual(_dealId, 2026, 13));
    }
}
