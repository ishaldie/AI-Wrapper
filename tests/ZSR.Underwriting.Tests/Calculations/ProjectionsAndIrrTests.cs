using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Domain.Interfaces;

namespace ZSR.Underwriting.Tests.Calculations;

public class ProjectionsAndIrrTests
{
    private readonly IUnderwritingCalculator _calc = new UnderwritingCalculator();

    // === Test scenario (continued from Phase 1+2) ===
    // Base NOI = 693,876.35, Growth: 0% Y1-2, 1.5% Y3-5
    // Debt service (IO) = 536,250, Reserves = 25,000
    // Exit cap = 5.5%, Equity = 5,550,000, Debt = 9,750,000

    // --- NOI Projection ---

    [Fact]
    public void ProjectNoi_ZeroGrowthYears_StaysFlat()
    {
        var growthRates = new[] { 0m, 0m, 1.5m, 1.5m, 1.5m };
        var projected = _calc.ProjectNoi(693_876.35m, growthRates);

        Assert.Equal(5, projected.Length);
        Assert.Equal(693_876.35m, projected[0]); // Y1: no growth
        Assert.Equal(693_876.35m, projected[1]); // Y2: no growth
    }

    [Fact]
    public void ProjectNoi_GrowthAppliesCorrectly()
    {
        var growthRates = new[] { 0m, 0m, 1.5m, 1.5m, 1.5m };
        var projected = _calc.ProjectNoi(693_876.35m, growthRates);

        // Compute expected chain values programmatically to avoid manual rounding errors
        var y3 = Math.Round(693_876.35m * 1.015m, 2);
        var y4 = Math.Round(y3 * 1.015m, 2);
        var y5 = Math.Round(y4 * 1.015m, 2);

        Assert.Equal(y3, projected[2]);
        Assert.Equal(y4, projected[3]);
        Assert.Equal(y5, projected[4]);
        Assert.True(projected[4] > projected[3], "Y5 should exceed Y4 with positive growth");
    }

    [Fact]
    public void ProjectNoi_SingleYear_ReturnsBaseNoi()
    {
        var projected = _calc.ProjectNoi(500_000m, new[] { 0m });
        Assert.Single(projected);
        Assert.Equal(500_000m, projected[0]);
    }

    // --- Cash Flow Projection ---

    [Fact]
    public void ProjectCashFlows_ReturnsNoiMinusDebtAndReserves()
    {
        // Use projected NOI from calculator to get correct rounded values
        var growthRates = new[] { 0m, 0m, 1.5m, 1.5m, 1.5m };
        var nois = _calc.ProjectNoi(693_876.35m, growthRates);
        var cashFlows = _calc.ProjectCashFlows(nois, 536_250m, 25_000m);

        Assert.Equal(5, cashFlows.Length);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(nois[i] - 536_250m - 25_000m, cashFlows[i]);
        }
        Assert.True(cashFlows[0] > 0, "Y1 cash flow should be positive for this scenario");
    }

    [Fact]
    public void ProjectCashFlows_NegativeCashFlow_Allowed()
    {
        var nois = new[] { 100_000m };
        var cashFlows = _calc.ProjectCashFlows(nois, 200_000m, 25_000m);

        Assert.Equal(-125_000m, cashFlows[0]);
    }

    // --- Exit Value ---

    [Fact]
    public void CalculateExitValue_ReturnsTerminalNoiOverCap()
    {
        // Compute terminal NOI from projected chain to avoid manual rounding errors
        var growthRates = new[] { 0m, 0m, 1.5m, 1.5m, 1.5m };
        var projected = _calc.ProjectNoi(693_876.35m, growthRates);
        var terminalNoi = Math.Round(projected[4] * 1.015m, 2);
        var result = _calc.CalculateExitValue(terminalNoi, 5.5m);

        // Exit value = terminalNoi / 0.055
        var expected = Math.Round(terminalNoi / 0.055m, 2);
        Assert.Equal(expected, result);
        Assert.True(result > 10_000_000m, "Exit value should be in 8-figure range for this deal");
    }

    [Fact]
    public void CalculateExitValue_ZeroCap_ReturnsZero()
    {
        var result = _calc.CalculateExitValue(700_000m, 0m);
        Assert.Equal(0m, result);
    }

    // --- Sale Costs ---

    [Fact]
    public void CalculateSaleCosts_Default2Percent_ReturnsCorrect()
    {
        // Use a clean number to avoid chained rounding
        var result = _calc.CalculateSaleCosts(10_000_000m);
        Assert.Equal(200_000m, result);
    }

    [Fact]
    public void CalculateSaleCosts_CustomPercent_ReturnsCorrect()
    {
        var result = _calc.CalculateSaleCosts(10_000_000m, 0.03m);
        Assert.Equal(300_000m, result);
    }

    // --- Loan Balance ---

    [Fact]
    public void CalculateLoanBalance_InterestOnly_ReturnsOriginalAmount()
    {
        var result = _calc.CalculateLoanBalance(9_750_000m, 5.5m, true, 30, 5);
        Assert.Equal(9_750_000m, result);
    }

    [Fact]
    public void CalculateLoanBalance_Amortizing_LessThanOriginal()
    {
        var result = _calc.CalculateLoanBalance(9_750_000m, 5.5m, false, 30, 5);
        Assert.True(result < 9_750_000m, "Amortizing loan balance should decrease over time");
        Assert.True(result > 8_500_000m, "5 years into 30-year amortization shouldn't pay down that much");
    }

    [Fact]
    public void CalculateLoanBalance_FullyAmortized_ReturnsZero()
    {
        var result = _calc.CalculateLoanBalance(100_000m, 5.0m, false, 30, 30);
        Assert.InRange(result, -1m, 1m); // Should be ~0 with rounding
    }

    // --- Net Sale Proceeds ---

    [Fact]
    public void CalculateNetSaleProceeds_IOLoan_ReturnsCorrect()
    {
        // Exit 10M - sale costs 200K - loan 6.5M = 3,300,000
        var result = _calc.CalculateNetSaleProceeds(10_000_000m, 200_000m, 6_500_000m);
        Assert.Equal(3_300_000m, result);
    }

    // --- Equity Multiple ---

    [Fact]
    public void CalculateEquityMultiple_ReturnsCorrect()
    {
        // Simple: 200K/yr × 5 = 1M CFs + 1.5M proceeds = 2.5M total / 2M equity = 1.25x
        var cashFlows = new[] { 200_000m, 200_000m, 200_000m, 200_000m, 200_000m };
        var netProceeds = 1_500_000m;

        var result = _calc.CalculateEquityMultiple(cashFlows, netProceeds, 2_000_000m);
        Assert.Equal(1.25m, result);
    }

    [Fact]
    public void CalculateEquityMultiple_ZeroEquity_ReturnsZero()
    {
        var result = _calc.CalculateEquityMultiple(new[] { 100_000m }, 500_000m, 0m);
        Assert.Equal(0m, result);
    }

    // --- IRR ---

    [Fact]
    public void CalculateIrr_PositiveReturns_Converges()
    {
        // Simple scenario: invest 1M, get 200K/yr for 5 years, sell for 1.2M
        var cashFlows = new[] { 200_000m, 200_000m, 200_000m, 200_000m, 200_000m };
        var terminalValue = 1_200_000m;
        var result = _calc.CalculateIrr(1_000_000m, cashFlows, terminalValue);

        // IRR should be around 22-25% for this scenario
        Assert.InRange(result, 20m, 28m);
    }

    [Fact]
    public void CalculateIrr_BreakEven_ReturnsNearZero()
    {
        // Invest 1M, get back 1M after 1 year (no return)
        var cashFlows = new[] { 0m };
        var terminalValue = 1_000_000m;
        var result = _calc.CalculateIrr(1_000_000m, cashFlows, terminalValue);

        Assert.InRange(result, -1m, 1m);
    }

    [Fact]
    public void CalculateIrr_NegativeReturn_ReturnsNegative()
    {
        // Invest 1M, get back 800K total
        var cashFlows = new[] { 50_000m, 50_000m, 50_000m };
        var terminalValue = 600_000m;
        var result = _calc.CalculateIrr(1_000_000m, cashFlows, terminalValue);

        Assert.True(result < 0m, "IRR should be negative when total returns < investment");
    }

    [Fact]
    public void CalculateIrr_ZeroInvestment_ReturnsZero()
    {
        var result = _calc.CalculateIrr(0m, new[] { 100_000m }, 500_000m);
        Assert.Equal(0m, result);
    }

    // --- Full Pipeline Integration ---

    [Fact]
    public void FullPipeline_ProjectionsAndExit_ProducesCorrectMetrics()
    {
        decimal baseNoi = 693_876.35m;
        decimal debtService = 536_250m; // IO
        decimal reserves = 25_000m;
        decimal exitCapPercent = 5.5m;
        decimal debtAmount = 9_750_000m;
        decimal equity = 5_550_000m;
        var growthRates = new[] { 0m, 0m, 1.5m, 1.5m, 1.5m };

        var projectedNoi = _calc.ProjectNoi(baseNoi, growthRates);
        var cashFlows = _calc.ProjectCashFlows(projectedNoi, debtService, reserves);

        // Terminal NOI (forward Y6 from Y5)
        var terminalNoi = Math.Round(projectedNoi[4] * (1m + 1.5m / 100m), 2);
        var exitValue = _calc.CalculateExitValue(terminalNoi, exitCapPercent);
        var saleCosts = _calc.CalculateSaleCosts(exitValue);
        var loanBalance = _calc.CalculateLoanBalance(debtAmount, 5.5m, true, 30, 5);
        var netProceeds = _calc.CalculateNetSaleProceeds(exitValue, saleCosts, loanBalance);
        var equityMultiple = _calc.CalculateEquityMultiple(cashFlows, netProceeds, equity);

        Assert.Equal(5, projectedNoi.Length);
        Assert.Equal(5, cashFlows.Length);
        Assert.True(exitValue > 10_000_000m, "Exit value should be reasonable for this deal");
        Assert.True(netProceeds > 0m, "Net proceeds should be positive");
        Assert.True(equityMultiple > 0m, "Equity multiple should be positive");
    }
}
