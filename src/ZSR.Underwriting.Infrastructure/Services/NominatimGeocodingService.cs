using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Infrastructure.Services;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        HttpClient httpClient,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "ZSR-Underwriting-Analyst/1.0");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetArrayLength() == 0)
            {
                _logger.LogWarning("Nominatim returned no results for address: {Address}", address);
                return null;
            }

            var first = root[0];
            var lat = double.Parse(first.GetProperty("lat").GetString()!);
            var lng = double.Parse(first.GetProperty("lon").GetString()!);
            var displayName = first.GetProperty("display_name").GetString() ?? address;

            return new GeocodingResult
            {
                Latitude = lat,
                Longitude = lng,
                FormattedAddress = displayName
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Nominatim HTTP error for address: {Address}", address);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Nominatim JSON parse error for address: {Address}", address);
            return null;
        }
    }
}
