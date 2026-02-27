using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class SecuritizationCompServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly SecuritizationCompService _service;
    private readonly string _dbName;

    public SecuritizationCompServiceTests()
    {
        _dbName = $"SecCompTests_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        _db = new AppDbContext(options);
        _service = new SecuritizationCompService(_db, NullLogger<SecuritizationCompService>.Instance);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private Deal CreateDeal(PropertyType propertyType = PropertyType.Multifamily,
        string state = "GA", int unitCount = 100)
    {
        var deal = new Deal("Test Deal", "user1")
        {
            PropertyType = propertyType,
            UnitCount = unitCount,
        };
        // Set state via the flat field on Deal
        deal.GetType().GetProperty("Address")!.SetValue(deal, $"123 Main St, Atlanta, {state}");
        return deal;
    }

    private SecuritizationComp CreateComp(
        SecuritizationDataSource source = SecuritizationDataSource.CMBS,
        PropertyType propertyType = PropertyType.Multifamily,
        string state = "GA",
        int units = 100,
        decimal dscr = 1.30m,
        decimal ltv = 72m,
        decimal capRate = 5.5m,
        decimal occupancy = 93m,
        decimal interestRate = 5.25m,
        decimal loanAmount = 10_000_000m,
        int monthsAgo = 6)
    {
        return new SecuritizationComp(source)
        {
            PropertyType = propertyType,
            State = state,
            City = "Atlanta",
            Units = units,
            LoanAmount = loanAmount,
            InterestRate = interestRate,
            DSCR = dscr,
            LTV = ltv,
            CapRate = capRate,
            Occupancy = occupancy,
            NOI = 800_000m,
            OriginationDate = DateTime.UtcNow.AddMonths(-monthsAgo),
            DealName = $"Deal-{Guid.NewGuid():N}"[..20],
        };
    }

    private async Task SeedComps(params SecuritizationComp[] comps)
    {
        _db.SecuritizationComps.AddRange(comps);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task FindComps_FiltersByPropertyType()
    {
        await SeedComps(
            CreateComp(propertyType: PropertyType.Multifamily, state: "GA"),
            CreateComp(propertyType: PropertyType.SeniorApartment, state: "GA"),
            CreateComp(propertyType: PropertyType.Multifamily, state: "GA")
        );

        var deal = CreateDeal(PropertyType.Multifamily, "GA");
        var result = await _service.FindCompsAsync(deal);

        Assert.Equal(2, result.Comps.Count);
        Assert.All(result.Comps, c => Assert.Equal(PropertyType.Multifamily, c.PropertyType));
    }

    [Fact]
    public async Task FindComps_FiltersByState_WhenEnoughInState()
    {
        // Seed 6 GA comps (above the 5-comp threshold) + 1 TX comp
        await SeedComps(
            CreateComp(state: "GA"),
            CreateComp(state: "GA"),
            CreateComp(state: "GA"),
            CreateComp(state: "GA"),
            CreateComp(state: "GA"),
            CreateComp(state: "GA"),
            CreateComp(state: "TX")
        );

        var deal = CreateDeal(state: "GA");
        var result = await _service.FindCompsAsync(deal);

        Assert.Equal(6, result.Comps.Count);
        Assert.All(result.Comps, c => Assert.Equal("GA", c.State));
    }

    [Fact]
    public async Task FindComps_FallsBackToNationwide_WhenFewStateComps()
    {
        await SeedComps(
            CreateComp(state: "GA"),                              // 1 in-state
            CreateComp(state: "TX"),                              // out-of-state
            CreateComp(state: "FL"),                              // out-of-state
            CreateComp(state: "CA"),                              // out-of-state
            CreateComp(state: "NY")                               // out-of-state
        );

        var deal = CreateDeal(state: "GA");
        var result = await _service.FindCompsAsync(deal, maxResults: 10);

        // Should include in-state comp + nationwide fallback (all same property type)
        Assert.True(result.Comps.Count >= 2, $"Expected at least 2 comps, got {result.Comps.Count}");
        Assert.Contains(result.Comps, c => c.State == "GA");
    }

    [Fact]
    public async Task FindComps_RanksCloserUnitCountHigher()
    {
        await SeedComps(
            CreateComp(state: "GA", units: 200),  // farther from 100
            CreateComp(state: "GA", units: 110),  // closer to 100
            CreateComp(state: "GA", units: 50)    // farther from 100
        );

        var deal = CreateDeal(unitCount: 100);
        var result = await _service.FindCompsAsync(deal);

        // Closest unit count should rank first
        Assert.Equal(110, result.Comps[0].Units);
    }

    [Fact]
    public async Task FindComps_RanksRecentCompsHigher()
    {
        await SeedComps(
            CreateComp(state: "GA", units: 100, monthsAgo: 24),  // older
            CreateComp(state: "GA", units: 100, monthsAgo: 3),   // recent
            CreateComp(state: "GA", units: 100, monthsAgo: 12)   // middle
        );

        var deal = CreateDeal(unitCount: 100);
        var result = await _service.FindCompsAsync(deal);

        // Most recent should rank first (all same units, so recency breaks tie)
        Assert.True(result.Comps[0].OriginationDate > result.Comps[1].OriginationDate);
    }

    [Fact]
    public async Task FindComps_RespectsMaxResults()
    {
        var comps = Enumerable.Range(0, 20)
            .Select(_ => CreateComp(state: "GA"))
            .ToArray();
        await SeedComps(comps);

        var deal = CreateDeal();
        var result = await _service.FindCompsAsync(deal, maxResults: 5);

        Assert.Equal(5, result.Comps.Count);
        Assert.Equal(20, result.TotalCompsFound);
    }

    [Fact]
    public async Task FindComps_NoComps_ReturnsEmptyResult()
    {
        var deal = CreateDeal();
        var result = await _service.FindCompsAsync(deal);

        Assert.Empty(result.Comps);
        Assert.Equal(0, result.TotalCompsFound);
        Assert.Null(result.MedianDSCR);
    }

    [Fact]
    public async Task FindComps_ComputesMedianDSCR()
    {
        await SeedComps(
            CreateComp(state: "GA", dscr: 1.20m),
            CreateComp(state: "GA", dscr: 1.30m),
            CreateComp(state: "GA", dscr: 1.40m),
            CreateComp(state: "GA", dscr: 1.50m),
            CreateComp(state: "GA", dscr: 1.60m)
        );

        var deal = CreateDeal();
        var result = await _service.FindCompsAsync(deal);

        Assert.Equal(1.40m, result.MedianDSCR);
        Assert.Equal(1.20m, result.MinDSCR);
        Assert.Equal(1.60m, result.MaxDSCR);
    }

    [Fact]
    public async Task FindComps_ComputesMedianLTV()
    {
        await SeedComps(
            CreateComp(state: "GA", ltv: 65m),
            CreateComp(state: "GA", ltv: 70m),
            CreateComp(state: "GA", ltv: 75m),
            CreateComp(state: "GA", ltv: 80m)
        );

        var deal = CreateDeal();
        var result = await _service.FindCompsAsync(deal);

        // Median of [65, 70, 75, 80] = (70 + 75) / 2 = 72.5
        Assert.Equal(72.5m, result.MedianLTV);
    }

    [Fact]
    public async Task FindComps_PopulatesUserMetrics()
    {
        await SeedComps(CreateComp(state: "GA"));

        var deal = CreateDeal();
        deal.CalculationResult = new CalculationResult(deal.Id)
        {
            DebtServiceCoverageRatio = 1.35m,
            GoingInCapRate = 5.5m,
            NetOperatingIncome = 900_000m,
        };
        deal.LoanLtv = 72m;
        deal.LoanRate = 5.25m;
        deal.TargetOccupancy = 94m;

        var result = await _service.FindCompsAsync(deal);

        Assert.Equal(1.35m, result.UserDSCR);
        Assert.Equal(72m, result.UserLTV);
        Assert.Equal(5.5m, result.UserCapRate);
        Assert.Equal(94m, result.UserOccupancy);
        Assert.Equal(5.25m, result.UserInterestRate);
    }

    [Fact]
    public async Task FindComps_FiltersToLast10Years()
    {
        await SeedComps(
            CreateComp(state: "GA", monthsAgo: 6),    // recent — included
            CreateComp(state: "GA", monthsAgo: 130)    // older than 10 years — excluded
        );

        var deal = CreateDeal();
        var result = await _service.FindCompsAsync(deal);

        Assert.Single(result.Comps);
    }

    [Fact]
    public async Task FindComps_IncludesAllDataSources()
    {
        await SeedComps(
            CreateComp(source: SecuritizationDataSource.CMBS, state: "GA"),
            CreateComp(source: SecuritizationDataSource.FannieMae, state: "GA"),
            CreateComp(source: SecuritizationDataSource.FreddieMac, state: "GA")
        );

        var deal = CreateDeal();
        var result = await _service.FindCompsAsync(deal);

        Assert.Equal(3, result.Comps.Count);
    }
}
