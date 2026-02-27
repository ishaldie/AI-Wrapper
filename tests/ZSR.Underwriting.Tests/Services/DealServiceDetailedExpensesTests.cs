using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class DealServiceDetailedExpensesTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly DealService _service;
    private readonly string _userId = "detail-expense-test-user";

    public DealServiceDetailedExpensesTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DealServiceDetailedExpenses_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _service = new DealService(_db, NullLogger<DealService>.Instance);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    // === DetailedExpenses round-trip ===

    [Fact]
    public async Task CreateAndGet_WithDetailedExpenses_RoundTripsCorrectly()
    {
        var input = new DealInputDto
        {
            PropertyName = "Test Deal",
            Address = "123 Main St",
            UnitCount = 100,
            PurchasePrice = 10_000_000m,
            DetailedExpenses = new DetailedExpenses
            {
                RealEstateTaxes = 150_000m,
                Insurance = 50_000m,
                Utilities = 30_000m,
                RepairsAndMaintenance = 60_000m,
                Payroll = 100_000m,
                Marketing = 10_000m,
                GeneralAndAdmin = 25_000m,
                ManagementFee = 35_000m,
                ReplacementReserves = 25_000m,
                OtherExpenses = 5_000m,
            },
        };

        var dealId = await _service.CreateDealAsync(input, _userId);
        var retrieved = await _service.GetDealAsync(dealId, _userId);

        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.DetailedExpenses);
        Assert.Equal(150_000m, retrieved.DetailedExpenses.RealEstateTaxes);
        Assert.Equal(50_000m, retrieved.DetailedExpenses.Insurance);
        Assert.Equal(30_000m, retrieved.DetailedExpenses.Utilities);
        Assert.Equal(60_000m, retrieved.DetailedExpenses.RepairsAndMaintenance);
        Assert.Equal(100_000m, retrieved.DetailedExpenses.Payroll);
        Assert.Equal(10_000m, retrieved.DetailedExpenses.Marketing);
        Assert.Equal(25_000m, retrieved.DetailedExpenses.GeneralAndAdmin);
        Assert.Equal(35_000m, retrieved.DetailedExpenses.ManagementFee);
        Assert.Equal(25_000m, retrieved.DetailedExpenses.ReplacementReserves);
        Assert.Equal(5_000m, retrieved.DetailedExpenses.OtherExpenses);
    }

    [Fact]
    public async Task CreateAndGet_WithManagementFeePct_RoundTrips()
    {
        var input = new DealInputDto
        {
            PropertyName = "Pct Deal",
            Address = "456 Oak Ave",
            UnitCount = 50,
            PurchasePrice = 5_000_000m,
            DetailedExpenses = new DetailedExpenses
            {
                RealEstateTaxes = 80_000m,
                ManagementFeePct = 4.5m,
            },
        };

        var dealId = await _service.CreateDealAsync(input, _userId);
        var retrieved = await _service.GetDealAsync(dealId, _userId);

        Assert.NotNull(retrieved?.DetailedExpenses);
        Assert.Equal(80_000m, retrieved.DetailedExpenses.RealEstateTaxes);
        Assert.Equal(4.5m, retrieved.DetailedExpenses.ManagementFeePct);
    }

    [Fact]
    public async Task CreateAndGet_WithNullDetailedExpenses_ReturnsNull()
    {
        var input = new DealInputDto
        {
            PropertyName = "No Expenses",
            Address = "789 Elm Blvd",
            UnitCount = 200,
            PurchasePrice = 20_000_000m,
            DetailedExpenses = null,
        };

        var dealId = await _service.CreateDealAsync(input, _userId);
        var retrieved = await _service.GetDealAsync(dealId, _userId);

        Assert.NotNull(retrieved);
        Assert.Null(retrieved.DetailedExpenses);
    }

    [Fact]
    public async Task Update_AddsDetailedExpenses_ToPreviouslyEmptyDeal()
    {
        var input = new DealInputDto
        {
            PropertyName = "Update Deal",
            Address = "111 Pine St",
            UnitCount = 75,
            PurchasePrice = 8_000_000m,
        };

        var dealId = await _service.CreateDealAsync(input, _userId);

        // Update with detailed expenses
        input.DetailedExpenses = new DetailedExpenses
        {
            Insurance = 40_000m,
            Payroll = 90_000m,
        };
        await _service.UpdateDealAsync(dealId, input, _userId);

        var retrieved = await _service.GetDealAsync(dealId, _userId);
        Assert.NotNull(retrieved?.DetailedExpenses);
        Assert.Equal(40_000m, retrieved.DetailedExpenses.Insurance);
        Assert.Equal(90_000m, retrieved.DetailedExpenses.Payroll);
    }

    [Fact]
    public async Task Update_RemovesDetailedExpenses_WhenSetToNull()
    {
        var input = new DealInputDto
        {
            PropertyName = "Remove Expenses",
            Address = "222 Birch Ln",
            UnitCount = 60,
            PurchasePrice = 6_000_000m,
            DetailedExpenses = new DetailedExpenses { Insurance = 30_000m },
        };

        var dealId = await _service.CreateDealAsync(input, _userId);

        // Update with null to remove
        input.DetailedExpenses = null;
        await _service.UpdateDealAsync(dealId, input, _userId);

        var retrieved = await _service.GetDealAsync(dealId, _userId);
        Assert.Null(retrieved?.DetailedExpenses);
    }

    // === New property types round-trip ===

    [Theory]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    [InlineData(PropertyType.BoardAndCare)]
    [InlineData(PropertyType.IndependentLiving)]
    [InlineData(PropertyType.SeniorApartment)]
    public async Task CreateAndGet_NewPropertyTypes_RoundTrip(PropertyType type)
    {
        var input = new DealInputDto
        {
            PropertyName = $"Test {type}",
            Address = "100 Test Way",
            UnitCount = 50,
            PurchasePrice = 5_000_000m,
            PropertyType = type,
        };

        var dealId = await _service.CreateDealAsync(input, _userId);
        var retrieved = await _service.GetDealAsync(dealId, _userId);

        Assert.NotNull(retrieved);
        Assert.Equal(type, retrieved.PropertyType);
    }

    // === DealSummaryDto includes new types ===

    [Fact]
    public async Task GetAllDeals_IncludesNewPropertyTypes()
    {
        var types = new[] { PropertyType.Bridge, PropertyType.Hospitality, PropertyType.Commercial };
        foreach (var type in types)
        {
            await _service.CreateDealAsync(new DealInputDto
            {
                PropertyName = $"Summary {type}",
                Address = "100 Test Way",
                UnitCount = 50,
                PurchasePrice = 5_000_000m,
                PropertyType = type,
            }, _userId);
        }

        var all = await _service.GetAllDealsAsync(_userId);

        Assert.Equal(3, all.Count);
        Assert.Contains(all, d => d.PropertyType == PropertyType.Bridge);
        Assert.Contains(all, d => d.PropertyType == PropertyType.Hospitality);
        Assert.Contains(all, d => d.PropertyType == PropertyType.Commercial);
    }

    // === BulkImport passes new property types through to DealInputDto ===

    [Fact]
    public async Task BulkImport_NewPropertyTypes_PassedToCreateDeal()
    {
        // Use fake DealService to capture what BulkImportService passes
        var fakeDealService = new BulkImportServiceTests.FakeDealService();
        var fakePortfolioService = new BulkImportServiceTests.FakePortfolioService();
        var importService = new BulkImportService(
            fakeDealService, fakePortfolioService,
            NullLogger<BulkImportService>.Instance);

        var rows = new List<BulkImportRowDto>
        {
            new() { RowNumber = 2, PropertyName = "Bridge Deal", Address = "Addr A", PropertyType = "Bridge" },
            new() { RowNumber = 3, PropertyName = "Hotel Deal", Address = "Addr B", PropertyType = "Hospitality" },
            new() { RowNumber = 4, PropertyName = "Office Deal", Address = "Addr C", PropertyType = "Commercial" },
            new() { RowNumber = 5, PropertyName = "LIHTC Deal", Address = "Addr D", PropertyType = "LIHTC" },
        };

        var result = await importService.ImportAsync(rows, "Mixed Types Portfolio", _userId);

        Assert.Equal(4, result.SuccessCount);
        Assert.Equal(PropertyType.Bridge, fakeDealService.CreatedDeals[0].PropertyType);
        Assert.Equal(PropertyType.Hospitality, fakeDealService.CreatedDeals[1].PropertyType);
        Assert.Equal(PropertyType.Commercial, fakeDealService.CreatedDeals[2].PropertyType);
        Assert.Equal(PropertyType.LIHTC, fakeDealService.CreatedDeals[3].PropertyType);
    }
}
