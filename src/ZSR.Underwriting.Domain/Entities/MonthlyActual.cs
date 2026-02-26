namespace ZSR.Underwriting.Domain.Entities;

public class MonthlyActual
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }

    // Revenue
    public decimal GrossRentalIncome { get; set; }
    public decimal VacancyLoss { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal EffectiveGrossIncome { get; set; }

    // Expenses (itemized)
    public decimal PropertyTaxes { get; set; }
    public decimal Insurance { get; set; }
    public decimal Utilities { get; set; }
    public decimal Repairs { get; set; }
    public decimal Management { get; set; }
    public decimal Payroll { get; set; }
    public decimal Marketing { get; set; }
    public decimal Administrative { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal TotalOperatingExpenses { get; set; }

    // Bottom line
    public decimal NetOperatingIncome { get; set; }
    public decimal DebtService { get; set; }
    public decimal CapitalExpenditures { get; set; }
    public decimal CashFlow { get; set; }

    // Occupancy snapshot
    public int OccupiedUnits { get; set; }
    public int TotalUnits { get; set; }
    public decimal OccupancyPercent { get; set; }

    public DateTime EnteredAt { get; set; }
    public string? Notes { get; set; }

    // EF Core parameterless constructor
    private MonthlyActual() { }

    public MonthlyActual(Guid dealId, int year, int month)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year));
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month));

        Id = Guid.NewGuid();
        DealId = dealId;
        Year = year;
        Month = month;
        EnteredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculates computed fields from line items.
    /// </summary>
    public void Recalculate()
    {
        EffectiveGrossIncome = GrossRentalIncome - VacancyLoss + OtherIncome;
        TotalOperatingExpenses = PropertyTaxes + Insurance + Utilities + Repairs +
            Management + Payroll + Marketing + Administrative + OtherExpenses;
        NetOperatingIncome = EffectiveGrossIncome - TotalOperatingExpenses;
        CashFlow = NetOperatingIncome - DebtService - CapitalExpenditures;
        if (TotalUnits > 0)
            OccupancyPercent = (decimal)OccupiedUnits / TotalUnits * 100;
    }
}
