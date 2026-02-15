using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Domain.Interfaces;

namespace ZSR.Underwriting.Tests.Calculations;

public class DebtAndReturnsTests
{
    private readonly IUnderwritingCalculator _calc = new UnderwritingCalculator();

    // === Test scenario: $15M purchase, 65% LTV, 5.5% rate, 100 units ===
    // Debt = 15,000,000 × 0.65 = 9,750,000
    // Acq costs = 15,000,000 × 0.02 = 300,000
    // Equity = 15,000,000 + 300,000 - 9,750,000 = 5,550,000
    // IO debt service = 9,750,000 × 0.055 = 536,250
    // Reserves = 250 × 100 = 25,000

    // --- Debt Amount ---

    [Fact]
    public void CalculateDebtAmount_65PercentLtv_ReturnsCorrect()
    {
        var result = _calc.CalculateDebtAmount(15_000_000m, 65m);
        Assert.Equal(9_750_000m, result);
    }

    [Fact]
    public void CalculateDebtAmount_ZeroLtv_ReturnsZero()
    {
        var result = _calc.CalculateDebtAmount(15_000_000m, 0m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateDebtAmount_100PercentLtv_ReturnsFullPrice()
    {
        var result = _calc.CalculateDebtAmount(15_000_000m, 100m);
        Assert.Equal(15_000_000m, result);
    }

    // --- Debt Service (Interest Only) ---

    [Fact]
    public void CalculateAnnualDebtService_InterestOnly_ReturnsCorrect()
    {
        var result = _calc.CalculateAnnualDebtService(9_750_000m, 5.5m, true, 30);
        Assert.Equal(536_250m, result);
    }

    [Fact]
    public void CalculateAnnualDebtService_InterestOnly_ZeroRate_ReturnsZero()
    {
        var result = _calc.CalculateAnnualDebtService(9_750_000m, 0m, true, 30);
        Assert.Equal(0m, result);
    }

    // --- Debt Service (Amortizing) ---

    [Fact]
    public void CalculateAnnualDebtService_Amortizing30Year_ReturnsCorrect()
    {
        // Monthly payment = 9,750,000 × [0.004583(1.004583)^360 / ((1.004583)^360 - 1)]
        // Monthly rate = 5.5 / 100 / 12 = 0.00458333...
        // (1+r)^360 = 7.18957...
        // Monthly = 9,750,000 × (0.004583 × 7.18957) / (7.18957 - 1)
        //         = 9,750,000 × 0.032948 / 6.18957
        //         = 9,750,000 × 0.005323
        //         ≈ 55,373.02 per month → 664,476.24 per year
        var result = _calc.CalculateAnnualDebtService(9_750_000m, 5.5m, false, 30);

        // Allow small rounding tolerance for amortization formula
        Assert.InRange(result, 664_000m, 665_000m);
    }

    [Fact]
    public void CalculateAnnualDebtService_Amortizing15Year_HigherPayment()
    {
        var result15 = _calc.CalculateAnnualDebtService(9_750_000m, 5.5m, false, 15);
        var result30 = _calc.CalculateAnnualDebtService(9_750_000m, 5.5m, false, 30);

        Assert.True(result15 > result30, "15-year amortization should have higher annual payment than 30-year");
    }

    [Fact]
    public void CalculateAnnualDebtService_ZeroDebt_ReturnsZero()
    {
        var result = _calc.CalculateAnnualDebtService(0m, 5.5m, false, 30);
        Assert.Equal(0m, result);
    }

    // --- Acquisition Costs ---

    [Fact]
    public void CalculateAcquisitionCosts_Default2Percent_ReturnsCorrect()
    {
        var result = _calc.CalculateAcquisitionCosts(15_000_000m);
        Assert.Equal(300_000m, result);
    }

    [Fact]
    public void CalculateAcquisitionCosts_CustomPercent_ReturnsCorrect()
    {
        var result = _calc.CalculateAcquisitionCosts(15_000_000m, 0.03m);
        Assert.Equal(450_000m, result);
    }

    // --- Equity Required ---

    [Fact]
    public void CalculateEquityRequired_ReturnsCorrect()
    {
        // 15M + 300K acq costs - 9.75M debt = 5,550,000
        var result = _calc.CalculateEquityRequired(15_000_000m, 300_000m, 9_750_000m);
        Assert.Equal(5_550_000m, result);
    }

    [Fact]
    public void CalculateEquityRequired_NoCosts_NoDept_ReturnsFullPrice()
    {
        var result = _calc.CalculateEquityRequired(15_000_000m, 0m, 0m);
        Assert.Equal(15_000_000m, result);
    }

    // --- Entry Cap Rate ---

    [Fact]
    public void CalculateEntryCapRate_ReturnsNoiOverPrice()
    {
        // NOI 693,876.35 / Price 15,000,000 × 100 = 4.6%
        var result = _calc.CalculateEntryCapRate(693_876.35m, 15_000_000m);
        Assert.Equal(4.6m, result);
    }

    [Fact]
    public void CalculateEntryCapRate_ZeroPrice_ReturnsZero()
    {
        var result = _calc.CalculateEntryCapRate(693_876.35m, 0m);
        Assert.Equal(0m, result);
    }

    // --- Exit Cap Rate ---

    [Fact]
    public void CalculateExitCapRate_AddsSpreadToMarketCap()
    {
        // Market cap 5.0% + 50bps (0.5%) = 5.5%
        var result = _calc.CalculateExitCapRate(5.0m);
        Assert.Equal(5.5m, result);
    }

    [Fact]
    public void CalculateExitCapRate_CustomSpread_ReturnsCorrect()
    {
        var result = _calc.CalculateExitCapRate(5.0m, 0.75m);
        Assert.Equal(5.75m, result);
    }

    // --- Reserves ---

    [Fact]
    public void CalculateAnnualReserves_Default250PerUnit_ReturnsCorrect()
    {
        var result = _calc.CalculateAnnualReserves(100);
        Assert.Equal(25_000m, result);
    }

    [Fact]
    public void CalculateAnnualReserves_CustomPerUnit_ReturnsCorrect()
    {
        var result = _calc.CalculateAnnualReserves(100, 300m);
        Assert.Equal(30_000m, result);
    }

    // --- Cash-on-Cash ---

    [Fact]
    public void CalculateCashOnCash_IOLoan_ReturnsCorrect()
    {
        // (NOI - debt service - reserves) / equity × 100
        // (693,876.35 - 536,250 - 25,000) / 5,550,000 × 100
        // = 132,626.35 / 5,550,000 × 100 = 2.4%
        var result = _calc.CalculateCashOnCash(693_876.35m, 536_250m, 25_000m, 5_550_000m);
        Assert.Equal(2.4m, result);
    }

    [Fact]
    public void CalculateCashOnCash_ZeroEquity_ReturnsZero()
    {
        var result = _calc.CalculateCashOnCash(693_876.35m, 536_250m, 25_000m, 0m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateCashOnCash_NegativeCashFlow_ReturnsNegative()
    {
        // NOI 100K - debt 200K - reserves 25K = -125K / 5M equity = -2.5%
        var result = _calc.CalculateCashOnCash(100_000m, 200_000m, 25_000m, 5_000_000m);
        Assert.Equal(-2.5m, result);
    }

    // --- DSCR ---

    [Fact]
    public void CalculateDscr_IOLoan_ReturnsCorrect()
    {
        // NOI / debt service = 693,876.35 / 536,250 = 1.29x
        var result = _calc.CalculateDscr(693_876.35m, 536_250m);
        Assert.Equal(1.29m, result);
    }

    [Fact]
    public void CalculateDscr_ZeroDebtService_ReturnsZero()
    {
        var result = _calc.CalculateDscr(693_876.35m, 0m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateDscr_BelowOne_IndicatesNegativeCoverage()
    {
        var result = _calc.CalculateDscr(400_000m, 536_250m);
        Assert.True(result < 1.0m, "DSCR below 1.0 means NOI doesn't cover debt service");
    }

    // --- Full Pipeline Integration ---

    [Fact]
    public void FullPipeline_DebtAndReturns_ProducesCorrectMetrics()
    {
        // Use Phase 1 pipeline results
        decimal noi = 693_876.35m;
        decimal purchasePrice = 15_000_000m;
        int unitCount = 100;
        decimal ltvPercent = 65m;
        decimal interestRate = 5.5m;
        decimal marketCapRate = 5.0m;

        var debtAmount = _calc.CalculateDebtAmount(purchasePrice, ltvPercent);
        var debtService = _calc.CalculateAnnualDebtService(debtAmount, interestRate, true, 30);
        var acqCosts = _calc.CalculateAcquisitionCosts(purchasePrice);
        var equity = _calc.CalculateEquityRequired(purchasePrice, acqCosts, debtAmount);
        var entryCap = _calc.CalculateEntryCapRate(noi, purchasePrice);
        var exitCap = _calc.CalculateExitCapRate(marketCapRate);
        var reserves = _calc.CalculateAnnualReserves(unitCount);
        var coc = _calc.CalculateCashOnCash(noi, debtService, reserves, equity);
        var dscr = _calc.CalculateDscr(noi, debtService);

        Assert.Equal(9_750_000m, debtAmount);
        Assert.Equal(536_250m, debtService);
        Assert.Equal(300_000m, acqCosts);
        Assert.Equal(5_550_000m, equity);
        Assert.Equal(4.6m, entryCap);
        Assert.Equal(5.5m, exitCap);
        Assert.Equal(25_000m, reserves);
        Assert.Equal(2.4m, coc);
        Assert.Equal(1.29m, dscr);
    }
}
