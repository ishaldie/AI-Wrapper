using ZSR.Underwriting.Application.Calculations;

namespace ZSR.Underwriting.Tests.Calculations;

public class DscrLoanSizingTests
{
    private readonly UnderwritingCalculator _calc = new();

    // === Mortgage Constant ===

    [Fact]
    public void MortgageConstant_InterestOnly_EqualsAnnualRate()
    {
        var constant = _calc.CalculateMortgageConstant(5m, 30, isInterestOnly: true);
        Assert.Equal(0.05m, constant);
    }

    [Fact]
    public void MortgageConstant_30Year_At5Percent()
    {
        var constant = _calc.CalculateMortgageConstant(5m, 30, isInterestOnly: false);
        // 30yr amortizing at 5%: monthly = 0.004167, n=360
        // Annual constant ≈ 6.44%
        Assert.InRange(constant, 0.0640m, 0.0650m);
    }

    [Fact]
    public void MortgageConstant_25Year_At6Percent()
    {
        var constant = _calc.CalculateMortgageConstant(6m, 25, isInterestOnly: false);
        // 25yr at 6%: annual constant ≈ 7.73%
        Assert.InRange(constant, 0.0770m, 0.0780m);
    }

    [Fact]
    public void MortgageConstant_ZeroRate_Amortizing_ReturnsSimplePrincipal()
    {
        var constant = _calc.CalculateMortgageConstant(0m, 30, isInterestOnly: false);
        // 0% rate: 1/30 = 0.03333
        Assert.InRange(constant, 0.0330m, 0.0340m);
    }

    [Fact]
    public void MortgageConstant_ZeroRate_IO_ReturnsZero()
    {
        var constant = _calc.CalculateMortgageConstant(0m, 30, isInterestOnly: true);
        Assert.Equal(0m, constant);
    }

    [Fact]
    public void MortgageConstant_ConsistentWithDebtService()
    {
        // Mortgage constant × loan = annual debt service
        decimal loanAmount = 10_000_000m;
        var constant = _calc.CalculateMortgageConstant(5.5m, 30, isInterestOnly: false);
        var debtServiceFromConstant = loanAmount * constant;
        var debtServiceDirect = _calc.CalculateAnnualDebtService(loanAmount, 5.5m, false, 30);

        // Should match within rounding tolerance
        Assert.InRange(debtServiceFromConstant, debtServiceDirect - 100m, debtServiceDirect + 100m);
    }

    // === Max Loan by DSCR ===

    [Fact]
    public void MaxLoanByDscr_InterestOnly_BasicCalc()
    {
        // NOI $400k, DSCR 1.25, IO rate 5% → constant = 0.05
        // Max = 400000 / (1.25 × 0.05) = 400000 / 0.0625 = 6,400,000
        var maxLoan = _calc.CalculateMaxLoanByDscr(400_000m, 1.25m, 0.05m);
        Assert.Equal(6_400_000m, maxLoan);
    }

    [Fact]
    public void MaxLoanByDscr_HigherDscr_ReducesLoan()
    {
        var loan125 = _calc.CalculateMaxLoanByDscr(500_000m, 1.25m, 0.065m);
        var loan145 = _calc.CalculateMaxLoanByDscr(500_000m, 1.45m, 0.065m);
        Assert.True(loan145 < loan125);
    }

    [Fact]
    public void MaxLoanByDscr_ZeroDscr_ReturnsZero()
    {
        var maxLoan = _calc.CalculateMaxLoanByDscr(500_000m, 0m, 0.065m);
        Assert.Equal(0m, maxLoan);
    }

    [Fact]
    public void MaxLoanByDscr_ZeroConstant_ReturnsZero()
    {
        var maxLoan = _calc.CalculateMaxLoanByDscr(500_000m, 1.25m, 0m);
        Assert.Equal(0m, maxLoan);
    }

    // === Constrained Loan ===

    [Fact]
    public void ConstrainedLoan_DscrConstrains_WhenNoiIsLow()
    {
        // Purchase $10M, LTV 80% → LTV-based = $8M
        // NOI $400k, DSCR 1.25, 5% IO → constant 0.05
        // DSCR-based = 400000/(1.25×0.05) = $6.4M
        // $6.4M < $8M → DSCR constrains
        var result = _calc.CalculateConstrainedLoan(
            purchasePrice: 10_000_000m,
            maxLtvPercent: 80m,
            noi: 400_000m,
            minDscr: 1.25m,
            annualRatePercent: 5m,
            amortizationYears: 30,
            isInterestOnly: true);

        Assert.Equal("DSCR", result.ConstrainingTest);
        Assert.Equal(6_400_000m, result.MaxLoan);
        Assert.Equal(8_000_000m, result.LtvBasedLoan);
        Assert.Equal(6_400_000m, result.DscrBasedLoan);
    }

    [Fact]
    public void ConstrainedLoan_LtvConstrains_WhenNoiIsHigh()
    {
        // Purchase $10M, LTV 75% → LTV-based = $7.5M
        // NOI $800k, DSCR 1.25, 5% IO → constant 0.05
        // DSCR-based = 800000/(1.25×0.05) = $12.8M
        // $7.5M < $12.8M → LTV constrains
        var result = _calc.CalculateConstrainedLoan(
            purchasePrice: 10_000_000m,
            maxLtvPercent: 75m,
            noi: 800_000m,
            minDscr: 1.25m,
            annualRatePercent: 5m,
            amortizationYears: 30,
            isInterestOnly: true);

        Assert.Equal("LTV", result.ConstrainingTest);
        Assert.Equal(7_500_000m, result.MaxLoan);
        Assert.Equal(7_500_000m, result.LtvBasedLoan);
        Assert.Equal(12_800_000m, result.DscrBasedLoan);
    }

    [Fact]
    public void ConstrainedLoan_Amortizing_TighterThanIO()
    {
        // Same deal, amortizing vs IO — amortizing has higher constant so DSCR loan is smaller
        var resultIO = _calc.CalculateConstrainedLoan(
            10_000_000m, 80m, 600_000m, 1.25m, 6m, 30, isInterestOnly: true);
        var resultAmort = _calc.CalculateConstrainedLoan(
            10_000_000m, 80m, 600_000m, 1.25m, 6m, 30, isInterestOnly: false);

        Assert.True(resultAmort.DscrBasedLoan < resultIO.DscrBasedLoan);
    }

    [Fact]
    public void ConstrainedLoan_ReturnsPositiveValues()
    {
        var result = _calc.CalculateConstrainedLoan(
            10_000_000m, 80m, 500_000m, 1.25m, 5m, 30, isInterestOnly: false);

        Assert.True(result.MaxLoan > 0);
        Assert.True(result.LtvBasedLoan > 0);
        Assert.True(result.DscrBasedLoan > 0);
    }

    // === Integration: Constrained loan consistent with type-specific defaults ===

    [Fact]
    public void ConstrainedLoan_HealthcareHighDscr_DscrLikelyConstrains()
    {
        // Healthcare DSCR = 1.45, LTV = 85% — high DSCR + moderate cap rate typical
        // NOI $1.2M, 6% IO, price $20M
        var result = _calc.CalculateConstrainedLoan(
            purchasePrice: 20_000_000m,
            maxLtvPercent: 85m,
            noi: 1_200_000m,
            minDscr: 1.45m,
            annualRatePercent: 6m,
            amortizationYears: 30,
            isInterestOnly: true);

        // LTV-based = $17M
        // DSCR-based = 1200000/(1.45×0.06) = 1200000/0.087 = $13.79M → DSCR constrains
        Assert.Equal("DSCR", result.ConstrainingTest);
        Assert.True(result.MaxLoan < 17_000_000m);
    }
}
