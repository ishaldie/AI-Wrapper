namespace ZSR.Underwriting.Application.DTOs;

public class VarianceReport
{
    public decimal ProjectedNoi { get; set; }
    public decimal ActualNoi { get; set; }
    public decimal NoiVariance { get; set; }
    public decimal NoiVariancePercent { get; set; }

    public decimal ProjectedRevenue { get; set; }
    public decimal ActualRevenue { get; set; }

    public decimal ProjectedExpenses { get; set; }
    public decimal ActualExpenses { get; set; }

    public decimal ProjectedCashOnCash { get; set; }
    public decimal ActualCashOnCash { get; set; }

    public VarianceLineItem[] RevenueItems { get; set; } = [];
    public VarianceLineItem[] ExpenseItems { get; set; } = [];
}

public class VarianceLineItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Projected { get; set; }
    public decimal Actual { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercent { get; set; }
    public VarianceSeverity Severity { get; set; }
}

public enum VarianceSeverity
{
    OnTrack,   // < 5%
    Warning,   // 5-15%
    Critical   // > 15%
}
