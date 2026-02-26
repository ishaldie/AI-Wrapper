namespace ZSR.Underwriting.Application.DTOs;

public class AnnualSummaryDto
{
    public int Year { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalNoi { get; set; }
    public decimal TotalCashFlow { get; set; }
    public decimal AverageOccupancy { get; set; }
    public int MonthsReported { get; set; }
}
