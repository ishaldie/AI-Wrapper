using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class Deal
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DealStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties (populated in later tasks)
    public Property? Property { get; set; }
    public UnderwritingInput? UnderwritingInput { get; set; }
    public CalculationResult? CalculationResult { get; set; }
    public UnderwritingReport? Report { get; set; }
    public RealAiData? RealAiData { get; set; }
    public ICollection<UploadedDocument> UploadedDocuments { get; set; } = new List<UploadedDocument>();

    // === Temporary flat fields (will migrate to Property/UnderwritingInput entities) ===
    public string PropertyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int UnitCount { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal? RentRollSummary { get; set; }
    public decimal? T12Summary { get; set; }
    public decimal? LoanLtv { get; set; }
    public decimal? LoanRate { get; set; }
    public bool IsInterestOnly { get; set; }
    public int? AmortizationYears { get; set; }
    public int? LoanTermYears { get; set; }
    public int? HoldPeriodYears { get; set; }
    public decimal? CapexBudget { get; set; }
    public decimal? TargetOccupancy { get; set; }
    public string? ValueAddPlans { get; set; }
    // === End temporary fields ===

    // EF Core parameterless constructor
    private Deal() { Name = string.Empty; }

    public Deal(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Deal name cannot be empty.", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
        Status = DealStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void UpdateStatus(DealStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
