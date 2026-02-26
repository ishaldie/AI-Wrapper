using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class GeocodingBackfillService : IGeocodingBackfillService
{
    private readonly AppDbContext _db;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<GeocodingBackfillService> _logger;

    public GeocodingBackfillService(
        AppDbContext db,
        IGeocodingService geocodingService,
        ILogger<GeocodingBackfillService> logger)
    {
        _db = db;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<int> BackfillAsync(CancellationToken cancellationToken = default)
    {
        var deals = await _db.Deals
            .Where(d => d.Latitude == null && d.Longitude == null && d.Address != null && d.Address != "")
            .ToListAsync(cancellationToken);

        var geocoded = 0;

        foreach (var deal in deals)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var result = await _geocodingService.GeocodeAsync(deal.Address, cancellationToken);
                if (result is not null)
                {
                    deal.Latitude = result.Latitude;
                    deal.Longitude = result.Longitude;
                    geocoded++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backfill geocoding failed for deal {DealId} ({Address})", deal.Id, deal.Address);
            }

            // Rate limit: 50ms between requests to stay under Google Maps free tier
            await Task.Delay(50, cancellationToken);
        }

        if (geocoded > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Geocoding backfill complete: {Geocoded}/{Total} deals geocoded", geocoded, deals.Count);
        return geocoded;
    }
}
