namespace ZSR.Underwriting.Application.DTOs;

public class RentRollSummaryDto
{
    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public int VacantUnits { get; set; }
    public decimal OccupancyPercent { get; set; }
    public decimal AverageMarketRent { get; set; }
    public decimal AverageActualRent { get; set; }
    public decimal TotalGrossPotentialRent { get; set; }
    public decimal TotalActualRent { get; set; }
}
