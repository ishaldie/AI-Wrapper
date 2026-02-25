namespace ZSR.Underwriting.Application.DTOs;

public class TenantDemographicsDto
{
    public decimal MedianHouseholdIncome { get; set; }
    public decimal MedianGrossRent { get; set; }
    public decimal AverageHouseholdSize { get; set; }
    public decimal RentBurdenPercent { get; set; }
    public int RenterOccupiedUnits { get; set; }
    public string ZipCode { get; set; } = string.Empty;
    public string Source { get; set; } = "U.S. Census Bureau ACS";
}
