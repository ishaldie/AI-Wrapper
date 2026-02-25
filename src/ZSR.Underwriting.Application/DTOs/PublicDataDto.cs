namespace ZSR.Underwriting.Application.DTOs;

public class PublicDataDto
{
    public CensusData? Census { get; set; }
    public BlsData? Bls { get; set; }
    public FredData? Fred { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}

public class CensusData
{
    public decimal MedianHouseholdIncome { get; set; }
    public int TotalPopulation { get; set; }
    public decimal MedianAge { get; set; }
    public string ZipCode { get; set; } = string.Empty;
    public string Source { get; set; } = "U.S. Census Bureau ACS";
}

public class BlsData
{
    public decimal UnemploymentRate { get; set; }
    public decimal JobGrowthPercent { get; set; }
    public string AreaName { get; set; } = string.Empty;
    public string Source { get; set; } = "U.S. Bureau of Labor Statistics";
}

public class FredData
{
    public decimal? CpiAllItems { get; set; }
    public decimal? TreasuryRate10Y { get; set; }
    public decimal? RentIndex { get; set; }
    public string Source { get; set; } = "Federal Reserve Economic Data (FRED)";
}
