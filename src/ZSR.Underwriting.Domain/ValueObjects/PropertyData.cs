namespace ZSR.Underwriting.Domain.ValueObjects;

public class PropertyData
{
    public decimal? InPlaceRent { get; set; }
    public decimal? Occupancy { get; set; }
    public int? YearBuilt { get; set; }
    public decimal? Acreage { get; set; }
    public int? SquareFootage { get; set; }
    public string? Amenities { get; set; }
    public string? BuildingType { get; set; }
}
