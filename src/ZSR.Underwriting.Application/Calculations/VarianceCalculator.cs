using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Calculations;

public class VarianceCalculator : IVarianceCalculator
{
    public VarianceReport CalculateVariance(CalculationResult projections, IReadOnlyList<MonthlyActual> actuals)
    {
        if (actuals.Count == 0)
            return new VarianceReport();

        // Annualize actuals (if fewer than 12 months, extrapolate)
        var monthsReported = actuals.Count;
        var annualizationFactor = monthsReported > 0 ? 12m / monthsReported : 1m;

        var actualRevenue = actuals.Sum(a => a.EffectiveGrossIncome) * annualizationFactor;
        var actualExpenses = actuals.Sum(a => a.TotalOperatingExpenses) * annualizationFactor;
        var actualNoi = actuals.Sum(a => a.NetOperatingIncome) * annualizationFactor;
        var actualCashFlow = actuals.Sum(a => a.CashFlow) * annualizationFactor;

        var projectedRevenue = projections.EffectiveGrossIncome ?? 0;
        var projectedExpenses = projections.OperatingExpenses ?? 0;
        var projectedNoi = projections.NetOperatingIncome ?? 0;
        var projectedCoC = projections.CashOnCashReturn ?? 0;

        // Calculate actual CoC (annualized cash flow / equity invested)
        // Approximation using projected equity basis
        var equity = (projections.Deal?.PurchasePrice ?? 0) - (projections.LoanAmount ?? 0);
        var actualCoC = equity > 0 ? actualCashFlow / equity * 100 : 0;

        var report = new VarianceReport
        {
            ProjectedNoi = projectedNoi,
            ActualNoi = actualNoi,
            NoiVariance = actualNoi - projectedNoi,
            NoiVariancePercent = projectedNoi != 0 ? (actualNoi - projectedNoi) / Math.Abs(projectedNoi) * 100 : 0,
            ProjectedRevenue = projectedRevenue,
            ActualRevenue = actualRevenue,
            ProjectedExpenses = projectedExpenses,
            ActualExpenses = actualExpenses,
            ProjectedCashOnCash = projectedCoC,
            ActualCashOnCash = actualCoC,
            RevenueItems = BuildRevenueItems(projections, actuals, annualizationFactor),
            ExpenseItems = BuildExpenseItems(projections, actuals, annualizationFactor)
        };

        return report;
    }

    private static VarianceLineItem[] BuildRevenueItems(CalculationResult proj, IReadOnlyList<MonthlyActual> actuals, decimal factor)
    {
        var items = new List<VarianceLineItem>();

        AddItem(items, "Gross Rental Income", proj.GrossPotentialRent ?? 0, actuals.Sum(a => a.GrossRentalIncome) * factor);
        AddItem(items, "Vacancy Loss", proj.VacancyLoss ?? 0, actuals.Sum(a => a.VacancyLoss) * factor);
        AddItem(items, "Other Income", proj.OtherIncome ?? 0, actuals.Sum(a => a.OtherIncome) * factor);

        return items.ToArray();
    }

    private static VarianceLineItem[] BuildExpenseItems(CalculationResult proj, IReadOnlyList<MonthlyActual> actuals, decimal factor)
    {
        var items = new List<VarianceLineItem>();

        // Total projected expenses spread across categories
        var projExpenses = proj.OperatingExpenses ?? 0;

        AddItem(items, "Property Taxes", projExpenses * 0.25m, actuals.Sum(a => a.PropertyTaxes) * factor);
        AddItem(items, "Insurance", projExpenses * 0.10m, actuals.Sum(a => a.Insurance) * factor);
        AddItem(items, "Utilities", projExpenses * 0.15m, actuals.Sum(a => a.Utilities) * factor);
        AddItem(items, "Repairs & Maintenance", projExpenses * 0.10m, actuals.Sum(a => a.Repairs) * factor);
        AddItem(items, "Management", projExpenses * 0.20m, actuals.Sum(a => a.Management) * factor);
        AddItem(items, "Other Expenses", projExpenses * 0.20m,
            actuals.Sum(a => a.Payroll + a.Marketing + a.Administrative + a.OtherExpenses) * factor);

        return items.ToArray();
    }

    private static void AddItem(List<VarianceLineItem> items, string label, decimal projected, decimal actual)
    {
        var variance = actual - projected;
        var variancePct = projected != 0 ? variance / projected * 100 : 0;

        items.Add(new VarianceLineItem
        {
            Label = label,
            Projected = projected,
            Actual = actual,
            VarianceAmount = variance,
            VariancePercent = variancePct,
            Severity = Math.Abs(variancePct) switch
            {
                < 5 => VarianceSeverity.OnTrack,
                < 15 => VarianceSeverity.Warning,
                _ => VarianceSeverity.Critical
            }
        });
    }
}
