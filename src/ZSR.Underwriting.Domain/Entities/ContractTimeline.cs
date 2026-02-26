namespace ZSR.Underwriting.Domain.Entities;

public class ContractTimeline
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public DateTime? LoiDate { get; set; }
    public DateTime? PsaExecutedDate { get; set; }
    public DateTime? InspectionDeadline { get; set; }
    public DateTime? FinancingContingencyDate { get; set; }
    public DateTime? AppraisalDeadline { get; set; }
    public DateTime? TitleDeadline { get; set; }
    public DateTime? ClosingDate { get; set; }
    public DateTime? ActualClosingDate { get; set; }

    public decimal? EarnestMoneyDeposit { get; set; }
    public decimal? AdditionalDeposit { get; set; }
    public string? LenderName { get; set; }
    public string? TitleCompany { get; set; }
    public string? Notes { get; set; }

    // EF Core parameterless constructor
    private ContractTimeline() { }

    public ContractTimeline(Guid dealId)
    {
        Id = Guid.NewGuid();
        DealId = dealId;
    }

    /// <summary>
    /// Returns the next upcoming deadline, or null if all passed/empty.
    /// </summary>
    public (string Name, DateTime Date)? GetNextDeadline()
    {
        var now = DateTime.UtcNow;
        var deadlines = new (string Name, DateTime? Date)[]
        {
            ("Inspection", InspectionDeadline),
            ("Financing Contingency", FinancingContingencyDate),
            ("Appraisal", AppraisalDeadline),
            ("Title", TitleDeadline),
            ("Closing", ClosingDate)
        };

        var upcoming = deadlines
            .Where(d => d.Date.HasValue && d.Date.Value > now)
            .OrderBy(d => d.Date!.Value)
            .Select(d => (d.Name, d.Date!.Value))
            .ToArray();

        return upcoming.Length > 0 ? upcoming[0] : null;
    }
}
