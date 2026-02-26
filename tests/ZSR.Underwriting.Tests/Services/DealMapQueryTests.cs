using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class DealMapQueryTests : IAsyncLifetime
{
    private AppDbContext _db = null!;

    public Task InitializeAsync()
    {
        var dbName = $"DealMapQueryTests_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _db = new AppDbContext(options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    private DealService CreateService(IGeocodingService? geocoding = null)
    {
        return new DealService(_db, NullLogger<DealService>.Instance, geocoding);
    }

    // --- Phase 3: Geocoding integration ---

    [Fact]
    public async Task CreateDealAsync_Geocodes_Address()
    {
        var geocoding = new StubGeocodingService(new GeocodingResult
        {
            Latitude = 40.712776,
            Longitude = -74.005974,
            FormattedAddress = "New York, NY, USA"
        });
        var service = CreateService(geocoding);
        var input = new DealInputDto { PropertyName = "NYC Deal", Address = "New York, NY" };

        var id = await service.CreateDealAsync(input, "user-1");

        var deal = await _db.Deals.FindAsync(id);
        Assert.NotNull(deal);
        Assert.Equal(40.712776, deal.Latitude);
        Assert.Equal(-74.005974, deal.Longitude);
    }

    [Fact]
    public async Task UpdateDealAsync_Geocodes_When_Address_Changes()
    {
        var geocoding = new StubGeocodingService(new GeocodingResult
        {
            Latitude = 34.0522,
            Longitude = -118.2437,
            FormattedAddress = "Los Angeles, CA, USA"
        });
        var service = CreateService(geocoding);
        var input = new DealInputDto { PropertyName = "LA Deal", Address = "Los Angeles, CA" };
        var id = await service.CreateDealAsync(input, "user-1");
        geocoding.CallCount = 0;

        // Update with different address
        var updateInput = new DealInputDto { PropertyName = "LA Deal", Address = "San Francisco, CA" };
        await service.UpdateDealAsync(id, updateInput, "user-1");

        Assert.Equal(1, geocoding.CallCount);
    }

    [Fact]
    public async Task UpdateDealAsync_Skips_Geocoding_When_Address_Unchanged()
    {
        var geocoding = new StubGeocodingService(new GeocodingResult
        {
            Latitude = 40.712776,
            Longitude = -74.005974,
            FormattedAddress = "New York, NY, USA"
        });
        var service = CreateService(geocoding);
        var input = new DealInputDto { PropertyName = "NYC Deal", Address = "New York, NY" };
        var id = await service.CreateDealAsync(input, "user-1");
        geocoding.CallCount = 0;

        // Update with same address
        var updateInput = new DealInputDto { PropertyName = "NYC Deal Updated", Address = "New York, NY" };
        await service.UpdateDealAsync(id, updateInput, "user-1");

        Assert.Equal(0, geocoding.CallCount);
    }

    [Fact]
    public async Task CreateDealAsync_Saves_Deal_Even_When_Geocoding_Fails()
    {
        var geocoding = new FailingGeocodingService();
        var service = CreateService(geocoding);
        var input = new DealInputDto { PropertyName = "Test Deal", Address = "123 Main St" };

        var id = await service.CreateDealAsync(input, "user-1");

        var deal = await _db.Deals.FindAsync(id);
        Assert.NotNull(deal);
        Assert.Null(deal.Latitude);
        Assert.Null(deal.Longitude);
    }

    [Fact]
    public async Task CreateDealAsync_Works_Without_Geocoding_Service()
    {
        var service = CreateService(geocoding: null);
        var input = new DealInputDto { PropertyName = "Test Deal", Address = "123 Main St" };

        var id = await service.CreateDealAsync(input, "user-1");

        var deal = await _db.Deals.FindAsync(id);
        Assert.NotNull(deal);
        Assert.Null(deal.Latitude);
    }

    // --- Phase 4: Map query ---

    [Fact]
    public async Task GetDealsForMapAsync_Returns_Only_Geocoded_Deals()
    {
        // Seed deals directly
        var geocoded = new Deal("Geocoded Deal", "user-1") { PropertyName = "Geocoded Deal", Address = "NYC", Latitude = 40.7, Longitude = -74.0 };
        var notGeocoded = new Deal("No Coords", "user-1") { PropertyName = "No Coords", Address = "Unknown" };
        _db.Deals.AddRange(geocoded, notGeocoded);
        await _db.SaveChangesAsync();

        var service = CreateService();
        var pins = await service.GetDealsForMapAsync("user-1");

        Assert.Single(pins);
        Assert.Equal("Geocoded Deal", pins[0].PropertyName);
        Assert.Equal(40.7, pins[0].Latitude);
    }

    [Fact]
    public async Task GetDealsForMapAsync_Filters_By_User()
    {
        var deal1 = new Deal("User1 Deal", "user-1") { PropertyName = "User1 Deal", Address = "NYC", Latitude = 40.7, Longitude = -74.0 };
        var deal2 = new Deal("User2 Deal", "user-2") { PropertyName = "User2 Deal", Address = "LA", Latitude = 34.0, Longitude = -118.2 };
        _db.Deals.AddRange(deal1, deal2);
        await _db.SaveChangesAsync();

        var service = CreateService();
        var pins = await service.GetDealsForMapAsync("user-1");

        Assert.Single(pins);
        Assert.Equal("User1 Deal", pins[0].PropertyName);
    }

    [Fact]
    public async Task GetDealsForMapAsync_Returns_Empty_When_No_Geocoded_Deals()
    {
        var deal = new Deal("No Coords", "user-1") { Address = "Unknown" };
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var service = CreateService();
        var pins = await service.GetDealsForMapAsync("user-1");

        Assert.Empty(pins);
    }

    // --- Stubs ---

    private class StubGeocodingService : IGeocodingService
    {
        private readonly GeocodingResult? _result;
        public int CallCount { get; set; }

        public StubGeocodingService(GeocodingResult? result) => _result = result;

        public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }

    private class FailingGeocodingService : IGeocodingService
    {
        public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
            => throw new HttpRequestException("Geocoding unavailable");
    }
}
