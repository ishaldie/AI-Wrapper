using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.DTOs;

public class DealSummaryDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int UnitCount { get; set; }
    public decimal PurchasePrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public Guid? PortfolioId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal? CapRate { get; set; }
    public decimal? Irr { get; set; }

    public bool IsSeniorHousing => PropertyType != PropertyType.Multifamily;
    public int? LicensedBeds { get; set; }
}
