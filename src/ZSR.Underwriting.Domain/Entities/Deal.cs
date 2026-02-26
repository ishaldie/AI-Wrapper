using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class Deal
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string ShortCode { get; private set; } = string.Empty;
    public DealStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Multi-tenant ownership
    public string UserId { get; private set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    // Computed lifecycle phase
    public DealPhase Phase => Status switch
    {
        DealStatus.Draft or DealStatus.InProgress or DealStatus.Screening or DealStatus.Complete => DealPhase.Acquisition,
        DealStatus.UnderContract => DealPhase.Contract,
        DealStatus.Closed or DealStatus.Active => DealPhase.Ownership,
        DealStatus.Disposition or DealStatus.Sold => DealPhase.Exit,
        _ => DealPhase.Acquisition
    };

    // Deal classification (for checklist filtering)
    public ExecutionType ExecutionType { get; set; } = ExecutionType.All;
    public string TransactionType { get; set; } = "All";

    // Portfolio assignment
    public Guid? PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }

    // Closing snapshot
    public DateTime? ClosedDate { get; set; }
    public decimal? ActualPurchasePrice { get; set; }

    // Navigation properties (populated in later tasks)
    public Property? Property { get; set; }
    public UnderwritingInput? UnderwritingInput { get; set; }
    public CalculationResult? CalculationResult { get; set; }
    public UnderwritingReport? Report { get; set; }
    public ICollection<UploadedDocument> UploadedDocuments { get; set; } = new List<UploadedDocument>();
    public ICollection<FieldOverride> FieldOverrides { get; set; } = new List<FieldOverride>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public ICollection<DealInvestor> DealInvestors { get; set; } = new List<DealInvestor>();
    public ICollection<CapitalStackItem> CapitalStackItems { get; set; } = new List<CapitalStackItem>();
    public ICollection<DealChecklistItem> DealChecklistItems { get; set; } = new List<DealChecklistItem>();

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
    public string? QuickAnalysisContent { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    // === End temporary fields ===

    // EF Core parameterless constructor
    private Deal() { Name = string.Empty; }

    public Deal(string name) : this(name, string.Empty, skipUserValidation: true) { }

    public Deal(string name, string userId) : this(name, userId, skipUserValidation: false) { }

    private Deal(string name, string userId, bool skipUserValidation)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Deal name cannot be empty.", nameof(name));
        if (!skipUserValidation && string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        Id = Guid.NewGuid();
        Name = name;
        ShortCode = GenerateShortCode();
        UserId = userId ?? string.Empty;
        Status = DealStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    private static string GenerateShortCode()
    {
        const string chars = "abcdefghjkmnpqrstuvwxyz23456789";
        Span<char> code = stackalloc char[8];
        var bytes = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        for (int i = 0; i < 8; i++)
            code[i] = chars[bytes[i] % chars.Length];
        return new string(code);
    }

    public void UpdateStatus(DealStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
