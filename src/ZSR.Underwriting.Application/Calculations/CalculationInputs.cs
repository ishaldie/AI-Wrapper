using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Calculations;

public class CalculationInputs
{
    public Guid DealId { get; set; }
    public decimal RentPerUnit { get; set; }
    public int UnitCount { get; set; }
    public decimal OccupancyPercent { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal LtvPercent { get; set; }
    public decimal InterestRatePercent { get; set; }
    public bool IsInterestOnly { get; set; }
    public int AmortizationYears { get; set; }
    public int HoldPeriodYears { get; set; }
    public decimal MarketCapRatePercent { get; set; }
    public decimal[] AnnualGrowthRatePercents { get; set; } = Array.Empty<decimal>();

    // Fannie Mae compliance (null when ExecutionType != FannieMae)
    public FannieProductType? FannieProductType { get; set; }
    public FannieComplianceInputs? FannieInputs { get; set; }
}
