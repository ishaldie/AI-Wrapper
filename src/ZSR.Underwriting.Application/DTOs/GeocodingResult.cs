namespace ZSR.Underwriting.Application.DTOs;

public class GeocodingResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
}
