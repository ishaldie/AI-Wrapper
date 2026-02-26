using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Infrastructure.Configuration;

namespace ZSR.Underwriting.Infrastructure.Services;

public class GoogleGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleMapsOptions _options;
    private readonly ILogger<GoogleGeocodingService> _logger;

    public GoogleGeocodingService(
        HttpClient httpClient,
        IOptions<GoogleMapsOptions> options,
        ILogger<GoogleGeocodingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_options.ApiKey}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString();
            if (status != "OK")
            {
                _logger.LogWarning("Geocoding returned status {Status} for address: {Address}", status, address);
                return null;
            }

            var results = root.GetProperty("results");
            if (results.GetArrayLength() == 0)
                return null;

            var firstResult = results[0];
            var location = firstResult.GetProperty("geometry").GetProperty("location");
            var formattedAddress = firstResult.GetProperty("formatted_address").GetString() ?? address;

            return new GeocodingResult
            {
                Latitude = location.GetProperty("lat").GetDouble(),
                Longitude = location.GetProperty("lng").GetDouble(),
                FormattedAddress = formattedAddress
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Geocoding HTTP error for address: {Address}", address);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Geocoding JSON parse error for address: {Address}", address);
            return null;
        }
    }
}
