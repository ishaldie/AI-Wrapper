using System.Text.Json.Serialization;

namespace ZSR.Underwriting.Application.DTOs;

public class DealUpdateDto
{
    [JsonPropertyName("general")]
    public DealUpdateGeneral? General { get; set; }

    [JsonPropertyName("underwriting")]
    public DealUpdateUnderwriting? Underwriting { get; set; }

    [JsonPropertyName("checklist")]
    public List<DealUpdateChecklistEntry>? Checklist { get; set; }
}

public class DealUpdateGeneral
{
    [JsonPropertyName("propertyName")]
    public string? PropertyName { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("unitCount")]
    public int? UnitCount { get; set; }

    [JsonPropertyName("yearBuilt")]
    public int? YearBuilt { get; set; }

    [JsonPropertyName("buildingType")]
    public string? BuildingType { get; set; }

    [JsonPropertyName("squareFootage")]
    public int? SquareFootage { get; set; }

    [JsonPropertyName("acreage")]
    public decimal? Acreage { get; set; }

    [JsonPropertyName("purchasePrice")]
    public decimal? PurchasePrice { get; set; }
}

public class DealUpdateUnderwriting
{
    [JsonPropertyName("grossPotentialRent")]
    public decimal? GrossPotentialRent { get; set; }

    [JsonPropertyName("vacancyLoss")]
    public decimal? VacancyLoss { get; set; }

    [JsonPropertyName("effectiveGrossIncome")]
    public decimal? EffectiveGrossIncome { get; set; }

    [JsonPropertyName("otherIncome")]
    public decimal? OtherIncome { get; set; }

    [JsonPropertyName("operatingExpenses")]
    public decimal? OperatingExpenses { get; set; }

    [JsonPropertyName("netOperatingIncome")]
    public decimal? NetOperatingIncome { get; set; }

    [JsonPropertyName("goingInCapRate")]
    public decimal? GoingInCapRate { get; set; }

    [JsonPropertyName("exitCapRate")]
    public decimal? ExitCapRate { get; set; }

    [JsonPropertyName("pricePerUnit")]
    public decimal? PricePerUnit { get; set; }

    [JsonPropertyName("loanAmount")]
    public decimal? LoanAmount { get; set; }

    [JsonPropertyName("annualDebtService")]
    public decimal? AnnualDebtService { get; set; }

    [JsonPropertyName("debtServiceCoverageRatio")]
    public decimal? DebtServiceCoverageRatio { get; set; }

    [JsonPropertyName("cashOnCashReturn")]
    public decimal? CashOnCashReturn { get; set; }

    [JsonPropertyName("internalRateOfReturn")]
    public decimal? InternalRateOfReturn { get; set; }

    [JsonPropertyName("equityMultiple")]
    public decimal? EquityMultiple { get; set; }

    [JsonPropertyName("capitalStack")]
    public List<DealUpdateCapitalStackEntry>? CapitalStack { get; set; }
}

public class DealUpdateCapitalStackEntry
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }

    [JsonPropertyName("termYears")]
    public int? TermYears { get; set; }
}

public class DealUpdateChecklistEntry
{
    [JsonPropertyName("item")]
    public string Item { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
}
