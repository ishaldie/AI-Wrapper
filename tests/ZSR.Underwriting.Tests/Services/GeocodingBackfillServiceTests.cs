using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class GeocodingBackfillServiceTests : IAsyncLifetime
{
    private AppDbContext _db = null!;

    public Task InitializeAsync()
    {
        var dbName = $"BackfillTests_{Guid.NewGuid()}";
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

    [Fact]
    public async Task BackfillAsync_Geocodes_Deals_Without_Coordinates()
    {
        var deal = new Deal("Test Deal", "user-1") { Address = "123 Main St" };
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var geocoding = new StubGeocodingService(new GeocodingResult
        {
            Latitude = 40.7, Longitude = -74.0, FormattedAddress = "123 Main St, NY"
        });
        var service = new GeocodingBackfillService(_db, geocoding, NullLogger<GeocodingBackfillService>.Instance);

        var count = await service.BackfillAsync();

        Assert.Equal(1, count);
        var updated = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(40.7, updated!.Latitude);
        Assert.Equal(-74.0, updated.Longitude);
    }

    [Fact]
    public async Task BackfillAsync_Skips_Empty_Addresses()
    {
        var deal = new Deal("Empty Address", "user-1") { Address = "" };
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var geocoding = new StubGeocodingService(new GeocodingResult
        {
            Latitude = 40.7, Longitude = -74.0, FormattedAddress = "NYC"
        });
        var service = new GeocodingBackfillService(_db, geocoding, NullLogger<GeocodingBackfillService>.Instance);

        var count = await service.BackfillAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task BackfillAsync_Skips_Already_Geocoded()
    {
        var deal = new Deal("Already Done", "user-1")
        {
            Address = "123 Main St",
            Latitude = 40.7,
            Longitude = -74.0
        };
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        var geocoding = new StubGeocodingService(new GeocodingResult
        {
            Latitude = 34.0, Longitude = -118.2, FormattedAddress = "LA"
        });
        var service = new GeocodingBackfillService(_db, geocoding, NullLogger<GeocodingBackfillService>.Instance);

        var count = await service.BackfillAsync();

        Assert.Equal(0, count);
        var unchanged = await _db.Deals.FindAsync(deal.Id);
        Assert.Equal(40.7, unchanged!.Latitude); // Not overwritten
    }

    [Fact]
    public async Task BackfillAsync_Continues_On_Individual_Failure()
    {
        var deal1 = new Deal("Deal 1", "user-1") { Address = "Address 1" };
        var deal2 = new Deal("Deal 2", "user-1") { Address = "Address 2" };
        _db.Deals.AddRange(deal1, deal2);
        await _db.SaveChangesAsync();

        var geocoding = new AlternatingGeocodingService();
        var service = new GeocodingBackfillService(_db, geocoding, NullLogger<GeocodingBackfillService>.Instance);

        var count = await service.BackfillAsync();

        // First call throws, second succeeds
        Assert.Equal(1, count);
    }

    private class StubGeocodingService : IGeocodingService
    {
        private readonly GeocodingResult? _result;
        public StubGeocodingService(GeocodingResult? result) => _result = result;
        public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    private class AlternatingGeocodingService : IGeocodingService
    {
        private int _callCount;
        public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken ct = default)
        {
            _callCount++;
            if (_callCount == 1) throw new HttpRequestException("Transient failure");
            return Task.FromResult<GeocodingResult?>(new GeocodingResult
            {
                Latitude = 34.0, Longitude = -118.2, FormattedAddress = "LA"
            });
        }
    }
}
