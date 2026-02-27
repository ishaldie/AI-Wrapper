namespace ZSR.Underwriting.Application.DTOs;

public class BulkImportRowDto
{
    public int RowNumber { get; set; }

    // Mapped fields
    public string PropertyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int? UnitCount { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? RentRollSummary { get; set; }
    public decimal? T12Summary { get; set; }
    public decimal? LoanLtv { get; set; }
    public decimal? LoanRate { get; set; }
    public decimal? CapexBudget { get; set; }

    // Senior housing fields
    public string? PropertyType { get; set; }
    public int? LicensedBeds { get; set; }
    public decimal? AverageDailyRate { get; set; }
    public decimal? PrivatePayPct { get; set; }

    // Validation state
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();

    private static readonly HashSet<string> SeniorTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AssistedLiving", "SkilledNursing", "MemoryCare", "CCRC",
        "BoardAndCare", "IndependentLiving", "SeniorApartment"
    };

    public bool IsSeniorType => !string.IsNullOrWhiteSpace(PropertyType) &&
        SeniorTypeNames.Contains(PropertyType);
}
