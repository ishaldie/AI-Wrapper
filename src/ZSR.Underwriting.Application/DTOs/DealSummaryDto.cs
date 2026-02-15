namespace ZSR.Underwriting.Application.DTOs;

public class DealSummaryDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int UnitCount { get; set; }
    public decimal PurchasePrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
