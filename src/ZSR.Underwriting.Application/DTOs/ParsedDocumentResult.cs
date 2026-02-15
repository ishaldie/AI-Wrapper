using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.DTOs;

/// <summary>
/// Result of parsing an uploaded document. Contains extracted fields
/// based on the document type. Null fields were not found in the document.
/// </summary>
public class ParsedDocumentResult
{
    public Guid DocumentId { get; set; }
    public DocumentType DocumentType { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // Rent Roll fields
    public int? UnitCount { get; set; }
    public decimal? AverageRent { get; set; }
    public decimal? TotalMonthlyRent { get; set; }
    public decimal? OccupancyRate { get; set; }
    public List<RentRollUnit> Units { get; set; } = new();

    // T12 P&L fields
    public decimal? GrossRevenue { get; set; }
    public decimal? EffectiveGrossIncome { get; set; }
    public decimal? TotalExpenses { get; set; }
    public decimal? NetOperatingIncome { get; set; }
    public List<T12LineItem> RevenueItems { get; set; } = new();
    public List<T12LineItem> ExpenseItems { get; set; } = new();

    // Loan Term Sheet fields
    public decimal? LoanAmount { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? LtvRatio { get; set; }
    public int? LoanTermYears { get; set; }
    public int? AmortizationYears { get; set; }
    public bool? IsInterestOnly { get; set; }
    public int? IoTermMonths { get; set; }
    public string? PrepaymentTerms { get; set; }
}

public class RentRollUnit
{
    public string UnitNumber { get; set; } = string.Empty;
    public string? UnitType { get; set; }
    public decimal MonthlyRent { get; set; }
    public bool IsOccupied { get; set; }
    public DateTime? LeaseExpiration { get; set; }
    public int? SquareFeet { get; set; }
}

public class T12LineItem
{
    public string Category { get; set; } = string.Empty;
    public decimal AnnualAmount { get; set; }
    public decimal? MonthlyAmount { get; set; }
}
