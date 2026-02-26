using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class CapExProject
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal ActualSpend { get; set; }
    public CapExStatus Status { get; set; } = CapExStatus.Planned;
    public DateTime? StartDate { get; set; }
    public DateTime? TargetCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
    public int? UnitsAffected { get; set; }
    public decimal? ExpectedRentIncrease { get; set; }

    public ICollection<CapExLineItem> LineItems { get; set; } = new List<CapExLineItem>();

    // EF Core parameterless constructor
    private CapExProject() { }

    public CapExProject(Guid dealId, string name, decimal budgetAmount)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Project name cannot be empty.", nameof(name));

        Id = Guid.NewGuid();
        DealId = dealId;
        Name = name;
        BudgetAmount = budgetAmount;
    }

    /// <summary>
    /// Recalculates ActualSpend from line items.
    /// </summary>
    public void RecalculateSpend()
    {
        ActualSpend = LineItems.Sum(li => li.Amount);
    }

    public decimal BudgetVariance => ActualSpend - BudgetAmount;
    public decimal BudgetUtilizationPercent => BudgetAmount > 0 ? ActualSpend / BudgetAmount * 100 : 0;
}
