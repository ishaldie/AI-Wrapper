using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Calculations;

public class VarianceCalculatorTests
{
    private readonly VarianceCalculator _calculator = new();

    private CalculationResult CreateProjections(
        decimal noi = 120000m,
        decimal egi = 200000m,
        decimal opex = 80000m,
        decimal gpr = 210000m,
        decimal vacancyLoss = 10500m,
        decimal otherIncome = 500m,
        decimal coc = 8.5m,
        decimal loanAmount = 600000m,
        decimal purchasePrice = 1000000m)
    {
        var deal = new Deal("Variance Test Deal", "test-user");
        typeof(Deal).GetProperty("PurchasePrice")!.SetValue(deal, purchasePrice);

        return new CalculationResult(deal.Id)
        {
            Deal = deal,
            NetOperatingIncome = noi,
            EffectiveGrossIncome = egi,
            OperatingExpenses = opex,
            GrossPotentialRent = gpr,
            VacancyLoss = vacancyLoss,
            OtherIncome = otherIncome,
            CashOnCashReturn = coc,
            LoanAmount = loanAmount
        };
    }

    private MonthlyActual CreateMonthlyActual(Guid dealId, int year, int month,
        decimal gri = 17500m, decimal vacancy = 875m, decimal otherIncome = 42m,
        decimal taxes = 1667m, decimal insurance = 667m, decimal utilities = 1000m,
        decimal repairs = 667m, decimal management = 1333m,
        decimal payroll = 500m, decimal marketing = 200m, decimal admin = 300m, decimal other = 333m,
        decimal debtService = 3000m)
    {
        var actual = new MonthlyActual(dealId, year, month)
        {
            GrossRentalIncome = gri,
            VacancyLoss = vacancy,
            OtherIncome = otherIncome,
            PropertyTaxes = taxes,
            Insurance = insurance,
            Utilities = utilities,
            Repairs = repairs,
            Management = management,
            Payroll = payroll,
            Marketing = marketing,
            Administrative = admin,
            OtherExpenses = other,
            DebtService = debtService
        };
        actual.Recalculate();
        return actual;
    }

    [Fact]
    public void EmptyActuals_ReturnsEmptyReport()
    {
        var proj = CreateProjections();
        var report = _calculator.CalculateVariance(proj, Array.Empty<MonthlyActual>());

        Assert.Equal(0, report.ProjectedNoi);
        Assert.Equal(0, report.ActualNoi);
        Assert.Empty(report.RevenueItems);
        Assert.Empty(report.ExpenseItems);
    }

    [Fact]
    public void TwelveMonthsActuals_NoAnnualization()
    {
        var dealId = Guid.NewGuid();
        var proj = CreateProjections(noi: 120000m, egi: 200000m, opex: 80000m);

        // 12 months × 10,000 NOI = 120,000 annualized (factor = 1)
        var actuals = Enumerable.Range(1, 12)
            .Select(m => CreateMonthlyActual(dealId, 2025, m, gri: 17500m, vacancy: 875m, otherIncome: 42m,
                taxes: 1667m, insurance: 667m, utilities: 1000m, repairs: 667m, management: 1333m,
                payroll: 500m, marketing: 200m, admin: 300m, other: 333m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        // With 12 months the annualization factor is 1, so actual = sum of all months
        Assert.Equal(120000m, report.ProjectedNoi);
        Assert.Equal(actuals.Sum(a => a.NetOperatingIncome), report.ActualNoi);
    }

    [Fact]
    public void SixMonthsActuals_AnnualizesBy2x()
    {
        var dealId = Guid.NewGuid();
        var proj = CreateProjections(noi: 120000m);

        var actuals = Enumerable.Range(1, 6)
            .Select(m => CreateMonthlyActual(dealId, 2025, m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        // 6 months → factor = 2
        var expectedNoi = actuals.Sum(a => a.NetOperatingIncome) * 2;
        Assert.Equal(expectedNoi, report.ActualNoi);
    }

    [Fact]
    public void ThreeMonthsActuals_AnnualizesBy4x()
    {
        var dealId = Guid.NewGuid();
        var proj = CreateProjections(noi: 120000m);

        var actuals = Enumerable.Range(1, 3)
            .Select(m => CreateMonthlyActual(dealId, 2025, m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        var expectedNoi = actuals.Sum(a => a.NetOperatingIncome) * 4;
        Assert.Equal(expectedNoi, report.ActualNoi);
    }

    [Fact]
    public void NoiVariance_CalculatedCorrectly()
    {
        var dealId = Guid.NewGuid();
        var proj = CreateProjections(noi: 100000m);

        // 12 months with NOI that sums to 110,000
        var actuals = Enumerable.Range(1, 12)
            .Select(m =>
            {
                var a = new MonthlyActual(dealId, 2025, m)
                {
                    GrossRentalIncome = 15000m,
                    VacancyLoss = 750m,
                    OtherIncome = 0m,
                    PropertyTaxes = 1000m,
                    Insurance = 500m,
                    Utilities = 600m,
                    Repairs = 400m,
                    Management = 750m,
                    Payroll = 0m, Marketing = 0m, Administrative = 0m, OtherExpenses = 0m
                };
                a.Recalculate();
                return a;
            })
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        Assert.Equal(100000m, report.ProjectedNoi);
        var totalActualNoi = actuals.Sum(a => a.NetOperatingIncome);
        Assert.Equal(totalActualNoi, report.ActualNoi);
        Assert.Equal(totalActualNoi - 100000m, report.NoiVariance);
    }

    [Fact]
    public void NoiVariancePercent_CalculatedCorrectly()
    {
        var dealId = Guid.NewGuid();
        var proj = CreateProjections(noi: 100000m);

        // Actuals: 12 months with identical NOI, summing to 90,000 annually
        var actuals = Enumerable.Range(1, 12)
            .Select(m =>
            {
                var a = new MonthlyActual(dealId, 2025, m)
                {
                    GrossRentalIncome = 12000m,
                    VacancyLoss = 500m,
                    OtherIncome = 0m,
                    PropertyTaxes = 1000m, Insurance = 400m, Utilities = 500m,
                    Repairs = 300m, Management = 600m,
                    Payroll = 200m, Marketing = 0m, Administrative = 0m, OtherExpenses = 0m
                };
                a.Recalculate();
                return a;
            })
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        var actualNoi = actuals.Sum(a => a.NetOperatingIncome);
        var expectedPct = (actualNoi - 100000m) / 100000m * 100;
        Assert.Equal(expectedPct, report.NoiVariancePercent);
    }

    [Fact]
    public void RevenueItems_ThreeLineItems()
    {
        var dealId = Guid.NewGuid();
        var proj = CreateProjections(gpr: 210000m, vacancyLoss: 10500m, otherIncome: 500m);

        var actuals = Enumerable.Range(1, 12)
            .Select(m => CreateMonthlyActual(dealId, 2025, m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        Assert.Equal(3, report.RevenueItems.Length);
        Assert.Equal("Gross Rental Income", report.RevenueItems[0].Label);
        Assert.Equal("Vacancy Loss", report.RevenueItems[1].Label);
        Assert.Equal("Other Income", report.RevenueItems[2].Label);
    }

    [Fact]
    public void ExpenseItems_SixCategories()
    {
        var dealId = Guid.NewGuid();
        var proj = CreateProjections(opex: 80000m);

        var actuals = Enumerable.Range(1, 12)
            .Select(m => CreateMonthlyActual(dealId, 2025, m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        Assert.Equal(6, report.ExpenseItems.Length);
        Assert.Equal("Property Taxes", report.ExpenseItems[0].Label);
        Assert.Equal("Insurance", report.ExpenseItems[1].Label);
        Assert.Equal("Utilities", report.ExpenseItems[2].Label);
        Assert.Equal("Repairs & Maintenance", report.ExpenseItems[3].Label);
        Assert.Equal("Management", report.ExpenseItems[4].Label);
        Assert.Equal("Other Expenses", report.ExpenseItems[5].Label);
    }

    [Fact]
    public void SeverityThresholds_OnTrack_Under5Percent()
    {
        var dealId = Guid.NewGuid();
        // Projected GPR = 210,000. Actual GPR: 12 months × 17,500 = 210,000. Exact match → 0% variance → OnTrack
        var proj = CreateProjections(gpr: 210000m);

        var actuals = Enumerable.Range(1, 12)
            .Select(m => CreateMonthlyActual(dealId, 2025, m, gri: 17500m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        Assert.Equal(VarianceSeverity.OnTrack, report.RevenueItems[0].Severity);
    }

    [Fact]
    public void SeverityThresholds_Warning_Between5And15Percent()
    {
        var dealId = Guid.NewGuid();
        // Projected GPR = 100,000. Actual: 12 × 9,000 = 108,000 → 8% variance → Warning
        var proj = CreateProjections(gpr: 100000m);

        var actuals = Enumerable.Range(1, 12)
            .Select(m => CreateMonthlyActual(dealId, 2025, m, gri: 9000m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        Assert.Equal(VarianceSeverity.Warning, report.RevenueItems[0].Severity);
    }

    [Fact]
    public void SeverityThresholds_Critical_Over15Percent()
    {
        var dealId = Guid.NewGuid();
        // Projected GPR = 100,000. Actual: 12 × 7,000 = 84,000 → 16% variance → Critical
        var proj = CreateProjections(gpr: 100000m);

        var actuals = Enumerable.Range(1, 12)
            .Select(m => CreateMonthlyActual(dealId, 2025, m, gri: 7000m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        Assert.Equal(VarianceSeverity.Critical, report.RevenueItems[0].Severity);
    }

    [Fact]
    public void CashOnCash_CalculatedFromEquityAndCashFlow()
    {
        var dealId = Guid.NewGuid();
        // Purchase price 1M, loan 600K → equity 400K
        var proj = CreateProjections(purchasePrice: 1000000m, loanAmount: 600000m, coc: 8.5m);

        var actuals = Enumerable.Range(1, 12)
            .Select(m => CreateMonthlyActual(dealId, 2025, m, debtService: 3000m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        Assert.Equal(8.5m, report.ProjectedCashOnCash);
        // Actual CoC = annualized cash flow / equity * 100
        var annualCashFlow = actuals.Sum(a => a.CashFlow);
        var equity = 1000000m - 600000m;
        var expectedCoC = annualCashFlow / equity * 100;
        Assert.Equal(expectedCoC, report.ActualCashOnCash);
    }

    [Fact]
    public void ZeroProjectedNoi_NoiVariancePercentIsZero()
    {
        var dealId = Guid.NewGuid();
        var proj = CreateProjections(noi: 0m);

        var actuals = new List<MonthlyActual>
        {
            CreateMonthlyActual(dealId, 2025, 1)
        };

        var report = _calculator.CalculateVariance(proj, actuals);

        Assert.Equal(0m, report.NoiVariancePercent);
    }

    [Fact]
    public void RevenueAndExpenseTotals_MatchReport()
    {
        var dealId = Guid.NewGuid();
        var proj = CreateProjections(egi: 200000m, opex: 80000m);

        var actuals = Enumerable.Range(1, 12)
            .Select(m => CreateMonthlyActual(dealId, 2025, m))
            .ToList();

        var report = _calculator.CalculateVariance(proj, actuals);

        Assert.Equal(200000m, report.ProjectedRevenue);
        Assert.Equal(80000m, report.ProjectedExpenses);
        Assert.Equal(actuals.Sum(a => a.EffectiveGrossIncome), report.ActualRevenue);
        Assert.Equal(actuals.Sum(a => a.TotalOperatingExpenses), report.ActualExpenses);
    }
}
