namespace ZSR.Underwriting.Application.DTOs;

public class DealMapPinDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int UnitCount { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal? CapRate { get; set; }
    public decimal? Irr { get; set; }
}
