using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Calculations;

public class FreddieComplianceTests
{
    // === Core tests ===

    [Fact]
    public void Evaluate_Conventional_AllPass()
    {
        var result = FreddieComplianceCalculator.Evaluate(
            FreddieProductType.Conventional,
            actualDscr: 1.35m, actualLtvPercent: 75m, actualAmortYears: 30,
            noi: 500_000m, annualDebtService: 370_000m,
            loanAmount: 3_000_000m, purchasePrice: 4_000_000m);

        Assert.True(result.OverallPass);
        Assert.True(result.DscrTest.Pass);
        Assert.True(result.LtvTest.Pass);
        Assert.True(result.AmortizationTest.Pass);
    }

    [Fact]
    public void Evaluate_Conventional_DscrFail()
    {
        var result = FreddieComplianceCalculator.Evaluate(
            FreddieProductType.Conventional,
            actualDscr: 1.10m, actualLtvPercent: 75m, actualAmortYears: 30,
            noi: 500_000m, annualDebtService: 454_545m,
            loanAmount: 3_000_000m, purchasePrice: 4_000_000m);

        Assert.False(result.OverallPass);
        Assert.False(result.DscrTest.Pass);
    }

    [Fact]
    public void Evaluate_Conventional_LtvFail()
    {
        var result = FreddieComplianceCalculator.Evaluate(
            FreddieProductType.Conventional,
            actualDscr: 1.35m, actualLtvPercent: 85m, actualAmortYears: 30,
            noi: 500_000m, annualDebtService: 370_000m,
            loanAmount: 3_400_000m, purchasePrice: 4_000_000m);

        Assert.False(result.OverallPass);
        Assert.False(result.LtvTest.Pass);
    }

    // === SBL market tier test ===

    [Fact]
    public void SblMarketTier_Top_Passes()
    {
        var test = FreddieComplianceCalculator.TestSblMarketTier(1.25m, 78m, "Top");
        Assert.True(test.Pass);
    }

    [Fact]
    public void SblMarketTier_Small_LtvFails()
    {
        var test = FreddieComplianceCalculator.TestSblMarketTier(1.30m, 78m, "Small");
        Assert.False(test.Pass); // Small tier max LTV is 75%
    }

    [Fact]
    public void Evaluate_SBL_WithTierInputs()
    {
        var inputs = new FreddieComplianceInputs
        {
            ProductType = FreddieProductType.SmallBalanceLoan,
            SblMarketTier = "Top"
        };
        var result = FreddieComplianceCalculator.Evaluate(
            FreddieProductType.SmallBalanceLoan,
            actualDscr: 1.25m, actualLtvPercent: 78m, actualAmortYears: 30,
            noi: 200_000m, annualDebtService: 160_000m,
            loanAmount: 2_000_000m, purchasePrice: 2_564_000m,
            inputs: inputs);

        Assert.NotNull(result.SblMarketTierTest);
        Assert.True(result.SblMarketTierTest.Pass);
    }

    // === Seniors blended DSCR ===

    [Fact]
    public void SeniorsBlendedDscr_Pass()
    {
        var test = FreddieComplianceCalculator.TestSeniorsBlendedDscr(1.50m, 50, 30, 20);
        Assert.True(test.Pass);
    }

    [Fact]
    public void SeniorsBlendedDscr_Fail()
    {
        var test = FreddieComplianceCalculator.TestSeniorsBlendedDscr(1.25m, 0, 0, 100);
        Assert.False(test.Pass); // 100% SN needs 1.50x
    }

    // === SNF NOI cap ===

    [Fact]
    public void SnfNoiCap_Pass_Under20Pct()
    {
        var test = FreddieComplianceCalculator.TestSnfNoiCap(100_000m, 600_000m);
        Assert.True(test.Pass); // 16.7%
    }

    [Fact]
    public void SnfNoiCap_Fail_Over20Pct()
    {
        var test = FreddieComplianceCalculator.TestSnfNoiCap(250_000m, 600_000m);
        Assert.False(test.Pass); // 41.7%
    }

    // === MHC rental homes cap ===

    [Fact]
    public void MhcRentalHomesCap_Pass()
    {
        var test = FreddieComplianceCalculator.TestMhcRentalHomesCap(20m);
        Assert.True(test.Pass);
    }

    [Fact]
    public void MhcRentalHomesCap_Fail()
    {
        var test = FreddieComplianceCalculator.TestMhcRentalHomesCap(30m);
        Assert.False(test.Pass);
    }

    // === Floating rate cap ===

    [Fact]
    public void FloatingRateCap_NotRequired_Under60LTV()
    {
        var test = FreddieComplianceCalculator.TestFloatingRateCap(55m, false);
        Assert.True(test.Pass); // No cap required
    }

    [Fact]
    public void FloatingRateCap_Required_Over60LTV_HasCap()
    {
        var test = FreddieComplianceCalculator.TestFloatingRateCap(75m, true);
        Assert.True(test.Pass);
    }

    [Fact]
    public void FloatingRateCap_Required_Over60LTV_NoCap()
    {
        var test = FreddieComplianceCalculator.TestFloatingRateCap(75m, false);
        Assert.False(test.Pass);
    }

    // === Value-Add rehab DSCR ===

    [Fact]
    public void ValueAddRehabDscr_IO_Pass()
    {
        var test = FreddieComplianceCalculator.TestValueAddRehabDscr(220_000m, 200_000m, true);
        Assert.True(test.Pass); // 1.10x meets IO minimum
    }

    [Fact]
    public void ValueAddRehabDscr_IO_Fail()
    {
        var test = FreddieComplianceCalculator.TestValueAddRehabDscr(200_000m, 200_000m, true);
        Assert.False(test.Pass); // 1.00x < 1.10x IO minimum
    }

    [Fact]
    public void ValueAddRehabDscr_Amortizing_Pass()
    {
        var test = FreddieComplianceCalculator.TestValueAddRehabDscr(230_000m, 200_000m, false);
        Assert.True(test.Pass); // 1.15x meets amortizing minimum
    }

    // === Lease-Up tests ===

    [Fact]
    public void LeaseUpOccupancy_Pass()
    {
        var test = FreddieComplianceCalculator.TestLeaseUpOccupancy(70m);
        Assert.True(test.Pass);
    }

    [Fact]
    public void LeaseUpOccupancy_Fail()
    {
        var test = FreddieComplianceCalculator.TestLeaseUpOccupancy(60m);
        Assert.False(test.Pass);
    }

    [Fact]
    public void LeaseUpLeased_Pass()
    {
        var test = FreddieComplianceCalculator.TestLeaseUpLeased(80m);
        Assert.True(test.Pass);
    }

    [Fact]
    public void LeaseUpLeased_Fail()
    {
        var test = FreddieComplianceCalculator.TestLeaseUpLeased(70m);
        Assert.False(test.Pass);
    }

    // === Supplemental combined ===

    [Fact]
    public void SupplementalCombined_Pass()
    {
        var (dscrTest, ltvTest) = FreddieComplianceCalculator.TestSupplementalCombined(
            seniorLoanAmount: 3_000_000m, supplementalLoanAmount: 500_000m,
            purchasePrice: 5_000_000m, noi: 500_000m,
            seniorDebtService: 300_000m, supplementalDebtService: 50_000m);

        Assert.True(dscrTest.Pass); // 1.43x >= 1.25x
        Assert.True(ltvTest.Pass); // 70% <= 80%
    }

    [Fact]
    public void SupplementalCombined_LtvFail()
    {
        var (dscrTest, ltvTest) = FreddieComplianceCalculator.TestSupplementalCombined(
            seniorLoanAmount: 3_500_000m, supplementalLoanAmount: 1_000_000m,
            purchasePrice: 5_000_000m, noi: 500_000m,
            seniorDebtService: 300_000m, supplementalDebtService: 100_000m);

        Assert.False(ltvTest.Pass); // 90% > 80%
    }

    // === MHC vacancy floor ===

    [Fact]
    public void MhcVacancyFloor_Caps_At_95()
    {
        Assert.Equal(95m, FreddieComplianceCalculator.EnforceMhcVacancyFloor(98m));
    }

    [Fact]
    public void MhcVacancyFloor_NoChange_Under_95()
    {
        Assert.Equal(90m, FreddieComplianceCalculator.EnforceMhcVacancyFloor(90m));
    }

    // === Assembler integration ===

    [Fact]
    public void Assembler_Produces_FreddieComplianceJson()
    {
        var inputs = new CalculationInputs
        {
            DealId = Guid.NewGuid(),
            RentPerUnit = 1500m,
            UnitCount = 100,
            OccupancyPercent = 95m,
            PurchasePrice = 10_000_000m,
            LtvPercent = 75m,
            InterestRatePercent = 5.5m,
            AmortizationYears = 30,
            HoldPeriodYears = 5,
            MarketCapRatePercent = 5.5m,
            AnnualGrowthRatePercents = new[] { 3m, 3m, 3m, 3m, 3m },
            FreddieProductType = FreddieProductType.Conventional
        };

        var result = CalculationResultAssembler.Assemble(inputs);
        Assert.NotNull(result.FreddieComplianceJson);
        Assert.Null(result.FannieComplianceJson);
    }

    [Fact]
    public void Assembler_Fannie_Unchanged_When_Freddie_Set()
    {
        var inputs = new CalculationInputs
        {
            DealId = Guid.NewGuid(),
            RentPerUnit = 1500m,
            UnitCount = 100,
            OccupancyPercent = 95m,
            PurchasePrice = 10_000_000m,
            LtvPercent = 75m,
            InterestRatePercent = 5.5m,
            AmortizationYears = 30,
            HoldPeriodYears = 5,
            MarketCapRatePercent = 5.5m,
            AnnualGrowthRatePercents = new[] { 3m, 3m, 3m, 3m, 3m },
            FannieProductType = FannieProductType.Conventional
        };

        var result = CalculationResultAssembler.Assemble(inputs);
        Assert.NotNull(result.FannieComplianceJson);
        Assert.Null(result.FreddieComplianceJson);
    }
}
