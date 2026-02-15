namespace ZSR.Underwriting.Domain.ValueObjects;

public class SalesComp
{
    public string? Address { get; set; }
    public decimal? PricePerUnit { get; set; }
    public DateTime? SaleDate { get; set; }
    public int? Units { get; set; }
    public string? Condition { get; set; }
}
