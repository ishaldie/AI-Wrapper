using System.Text.Json;
using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Calculations;

public class FannieComplianceTests
{
    // === Core compliance tests ===

    [Theory]
    [InlineData(1.30, 1.25, true)]
    [InlineData(1.25, 1.25, true)]
    [InlineData(1.24, 1.25, false)]
    [InlineData(0, 1.25, false)]
    public void TestDscr_PassFail(decimal actual, decimal min, bool expectedPass)
    {
        var result = FannieComplianceCalculator.TestDscr(actual, min);
        Assert.Equal(expectedPass, result.Pass);
        Assert.Equal(actual, result.ActualValue);
        Assert.Equal(min, result.RequiredValue);
    }

    [Theory]
    [InlineData(65, 80, true)]
    [InlineData(80, 80, true)]
    [InlineData(81, 80, false)]
    public void TestLtv_PassFail(decimal actual, decimal max, bool expectedPass)
    {
        var result = FannieComplianceCalculator.TestLtv(actual, max);
        Assert.Equal(expectedPass, result.Pass);
    }

    [Theory]
    [InlineData(25, 30, true)]
    [InlineData(30, 30, true)]
    [InlineData(35, 30, false)]
    [InlineData(35, 35, true)]
    public void TestAmortization_PassFail(int actual, int max, bool expectedPass)
    {
        var result = FannieComplianceCalculator.TestAmortization(actual, max);
        Assert.Equal(expectedPass, result.Pass);
    }

    // === Task 2: Seniors blended DSCR ===

    [Fact]
    public void TestSeniorsBlendedDscr_AllIL_1_30x_Passes()
    {
        var result = FannieComplianceCalculator.TestSeniorsBlendedDscr(1.30m, 100, 0, 0);
        Assert.True(result.Pass);
        Assert.Equal(1.30m, result.RequiredValue);
    }

    [Fact]
    public void TestSeniorsBlendedDscr_MixedBeds_Below_Fails()
    {
        // Mix: 50 IL + 30 AL + 20 ALZ → blended = (0.50*1.30 + 0.30*1.40 + 0.20*1.45) = 1.36
        var result = FannieComplianceCalculator.TestSeniorsBlendedDscr(1.35m, 50, 30, 20);
        Assert.False(result.Pass);
        Assert.Equal(1.36m, result.RequiredValue);
    }

    [Fact]
    public void TestSeniorsBlendedDscr_MixedBeds_Above_Passes()
    {
        var result = FannieComplianceCalculator.TestSeniorsBlendedDscr(1.37m, 50, 30, 20);
        Assert.True(result.Pass);
    }

    [Fact]
    public void TestSeniorsBlendedDscr_ZeroBeds_DefaultsToIL()
    {
        var result = FannieComplianceCalculator.TestSeniorsBlendedDscr(1.30m, 0, 0, 0);
        Assert.True(result.Pass);
        Assert.Equal(1.30m, result.RequiredValue);
    }

    // === Task 3: Cooperative dual DSCR ===

    [Fact]
    public void TestCoopDualDscr_BothPass()
    {
        // NOI: $100K actual, $200K market rental, DS: $80K
        // Actual DSCR: 100/80 = 1.25 >= 1.00 ✓
        // Market DSCR: 200/80 = 2.50 >= 1.55 ✓
        var (actual, market) = FannieComplianceCalculator.TestCooperativeDualDscr(100_000m, 200_000m, 80_000m);
        Assert.True(actual.Pass);
        Assert.True(market.Pass);
        Assert.Equal(1.25m, actual.ActualValue);
        Assert.Equal(2.50m, market.ActualValue);
    }

    [Fact]
    public void TestCoopDualDscr_ActualFails_MarketPasses()
    {
        // NOI: $70K actual, $200K market, DS: $80K
        // Actual DSCR: 70/80 = 0.88 < 1.00 ✗
        var (actual, market) = FannieComplianceCalculator.TestCooperativeDualDscr(70_000m, 200_000m, 80_000m);
        Assert.False(actual.Pass);
        Assert.True(market.Pass);
    }

    [Fact]
    public void TestCoopDualDscr_ActualPasses_MarketFails()
    {
        // NOI: $100K actual, $120K market, DS: $80K
        // Market DSCR: 120/80 = 1.50 < 1.55 ✗
        var (actual, market) = FannieComplianceCalculator.TestCooperativeDualDscr(100_000m, 120_000m, 80_000m);
        Assert.True(actual.Pass);
        Assert.False(market.Pass);
    }

    [Fact]
    public void TestCoopDualDscr_ZeroDebtService_ReturnsZero()
    {
        var (actual, market) = FannieComplianceCalculator.TestCooperativeDualDscr(100_000m, 200_000m, 0m);
        Assert.False(actual.Pass);
        Assert.False(market.Pass);
        Assert.Equal(0m, actual.ActualValue);
    }

    // === Task 4: SARM stress DSCR ===

    [Fact]
    public void TestSarmStressDscr_PassesAtMaxRate()
    {
        // NOI: $1M, Loan: $10M, 30yr amort
        // Margin: 2%, Cap Strike: 4% → Max rate: 6%
        // At 6%, DS ≈ $719K → DSCR = 1M / 719K ≈ 1.39x >= 1.05x ✓
        var result = FannieComplianceCalculator.TestSarmStressDscr(1_000_000m, 10_000_000m, 30, 2.0m, 4.0m);
        Assert.True(result.Pass);
        Assert.True(result.ActualValue >= 1.05m);
        Assert.Equal(1.05m, result.RequiredValue);
    }

    [Fact]
    public void TestSarmStressDscr_FailsAtHighMaxRate()
    {
        // NOI: $500K, Loan: $10M, 30yr amort
        // Margin: 3%, Cap Strike: 7% → Max rate: 10%
        // At 10%, DS ≈ $1.05M → DSCR = 500K / 1.05M ≈ 0.48x < 1.05x ✗
        var result = FannieComplianceCalculator.TestSarmStressDscr(500_000m, 10_000_000m, 30, 3.0m, 7.0m);
        Assert.False(result.Pass);
        Assert.True(result.ActualValue < 1.05m);
    }

    [Fact]
    public void TestSarmStressDscr_Notes_ShowMaxRate()
    {
        var result = FannieComplianceCalculator.TestSarmStressDscr(1_000_000m, 10_000_000m, 30, 2.5m, 4.5m);
        Assert.Contains("7.00%", result.Notes);
        Assert.Contains("margin 2.5%", result.Notes);
        Assert.Contains("cap 4.5%", result.Notes);
    }

    // === Task 5: Green Rewards NCF adjustment ===

    [Fact]
    public void GreenNcfAdjustment_CalculatesCorrectly()
    {
        // Base NCF: $500K, Owner savings: $40K, Tenant savings: $20K
        // Adjustment: 75% × $40K + 25% × $20K = $30K + $5K = $35K
        var (adj, adjustedNcf) = FannieComplianceCalculator.CalculateGreenNcfAdjustment(500_000m, 40_000m, 20_000m);
        Assert.Equal(35_000m, adj);
        Assert.Equal(535_000m, adjustedNcf);
    }

    [Fact]
    public void GreenNcfAdjustment_ZeroSavings_NoChange()
    {
        var (adj, adjustedNcf) = FannieComplianceCalculator.CalculateGreenNcfAdjustment(500_000m, 0m, 0m);
        Assert.Equal(0m, adj);
        Assert.Equal(500_000m, adjustedNcf);
    }

    [Fact]
    public void GreenNcfAdjustment_OnlyOwnerSavings()
    {
        var (adj, _) = FannieComplianceCalculator.CalculateGreenNcfAdjustment(500_000m, 100_000m, 0m);
        Assert.Equal(75_000m, adj); // 75% × $100K
    }

    [Fact]
    public void GreenNcfAdjustment_OnlyTenantSavings()
    {
        var (adj, _) = FannieComplianceCalculator.CalculateGreenNcfAdjustment(500_000m, 0m, 100_000m);
        Assert.Equal(25_000m, adj); // 25% × $100K
    }

    // === Task 6: MHC vacancy floor ===

    [Theory]
    [InlineData(97, 95)]  // Capped to 95%
    [InlineData(96, 95)]
    [InlineData(95, 95)]  // At boundary
    [InlineData(93, 93)]  // No change needed
    [InlineData(80, 80)]
    public void EnforceMhcVacancyFloor_CapsAt95(decimal input, decimal expected)
    {
        Assert.Equal(expected, FannieComplianceCalculator.EnforceMhcVacancyFloor(input));
    }

    [Fact]
    public void Assembler_MHC_Enforces_VacancyFloor()
    {
        var inputs = CreateBaseInputs();
        inputs.FannieProductType = FannieProductType.ManufacturedHousing;
        inputs.OccupancyPercent = 98m; // Should be capped to 95%

        var result = CalculationResultAssembler.Assemble(inputs);

        // With 95% occupancy, vacancy loss should be 5% of GPR
        var expectedGpr = inputs.RentPerUnit * inputs.UnitCount * 12;
        var expectedVacancy = expectedGpr * 0.05m; // 5% vacancy floor
        Assert.Equal(expectedVacancy, result.VacancyLoss);
    }

    // === Task 7: SNF NCF cap ===

    [Theory]
    [InlineData(150_000, 1_000_000, true)]   // 15% — under cap
    [InlineData(200_000, 1_000_000, true)]   // 20% — at cap
    [InlineData(250_000, 1_000_000, false)]  // 25% — over cap
    public void TestSnfNcfCap_PassFail(decimal snfNcf, decimal totalNcf, bool expectedPass)
    {
        var result = FannieComplianceCalculator.TestSnfNcfCap(snfNcf, totalNcf);
        Assert.Equal(expectedPass, result.Pass);
        Assert.Equal(20m, result.RequiredValue);
    }

    [Fact]
    public void TestSnfNcfCap_ZeroTotal_Returns0Percent()
    {
        var result = FannieComplianceCalculator.TestSnfNcfCap(100_000m, 0m);
        Assert.Equal(0m, result.ActualValue);
        Assert.True(result.Pass);
    }

    // === Task 8: ROAR rehab DSCR ===

    [Fact]
    public void TestRoarRehabDscr_IO_Passes()
    {
        // Rehab NOI: $100K, DS: $90K (IO), DSCR = 1.11 >= 1.00 ✓
        var result = FannieComplianceCalculator.TestRoarRehabDscr(100_000m, 90_000m, true);
        Assert.True(result.Pass);
        Assert.Equal(1.00m, result.RequiredValue);
        Assert.Equal(1.11m, result.ActualValue);
    }

    [Fact]
    public void TestRoarRehabDscr_IO_Fails()
    {
        // Rehab NOI: $80K, DS: $90K (IO), DSCR = 0.89 < 1.00 ✗
        var result = FannieComplianceCalculator.TestRoarRehabDscr(80_000m, 90_000m, true);
        Assert.False(result.Pass);
    }

    [Fact]
    public void TestRoarRehabDscr_Amortizing_Passes()
    {
        // Rehab NOI: $80K, DS: $100K (amort), DSCR = 0.80 >= 0.75 ✓
        var result = FannieComplianceCalculator.TestRoarRehabDscr(80_000m, 100_000m, false);
        Assert.True(result.Pass);
        Assert.Equal(0.75m, result.RequiredValue);
    }

    [Fact]
    public void TestRoarRehabDscr_Amortizing_Fails()
    {
        // Rehab NOI: $70K, DS: $100K (amort), DSCR = 0.70 < 0.75 ✗
        var result = FannieComplianceCalculator.TestRoarRehabDscr(70_000m, 100_000m, false);
        Assert.False(result.Pass);
    }

    [Fact]
    public void TestRoarRehabDscr_ZeroDS_Fails()
    {
        var result = FannieComplianceCalculator.TestRoarRehabDscr(100_000m, 0m, true);
        Assert.False(result.Pass);
        Assert.Equal(0m, result.ActualValue);
    }

    // === Task 9: Supplemental combined test ===

    [Fact]
    public void TestSupplementalCombined_BothPass()
    {
        // Senior: $7M loan, $500K DS; Supp: $2M loan, $150K DS; Purchase: $15M
        // Combined LTV: 9M/15M = 60% <= 70% ✓
        // Combined DSCR: NOI $1M / ($500K + $150K) = 1.54 >= 1.30 ✓
        var (dscr, ltv) = FannieComplianceCalculator.TestSupplementalCombined(
            7_000_000m, 2_000_000m, 15_000_000m, 1_000_000m, 500_000m, 150_000m);
        Assert.True(dscr.Pass);
        Assert.True(ltv.Pass);
        Assert.Equal(60m, ltv.ActualValue);
        Assert.Equal(1.54m, dscr.ActualValue);
    }

    [Fact]
    public void TestSupplementalCombined_LtvFails()
    {
        // Senior: $8M, Supp: $3.5M, Purchase: $15M → Combined LTV: 76.7% > 70% ✗
        var (_, ltv) = FannieComplianceCalculator.TestSupplementalCombined(
            8_000_000m, 3_500_000m, 15_000_000m, 1_000_000m, 500_000m, 200_000m);
        Assert.False(ltv.Pass);
    }

    [Fact]
    public void TestSupplementalCombined_DscrFails()
    {
        // NOI $800K / ($500K + $200K) = 1.14 < 1.30 ✗
        var (dscr, _) = FannieComplianceCalculator.TestSupplementalCombined(
            7_000_000m, 2_000_000m, 15_000_000m, 800_000m, 500_000m, 200_000m);
        Assert.False(dscr.Pass);
    }

    // === Task 10: Assembler stores compliance JSON ===

    [Fact]
    public void Assembler_Without_FannieType_No_Compliance()
    {
        var inputs = CreateBaseInputs();
        inputs.FannieProductType = null;

        var result = CalculationResultAssembler.Assemble(inputs);
        Assert.Null(result.FannieComplianceJson);
    }

    [Fact]
    public void Assembler_With_FannieType_Stores_Compliance()
    {
        var inputs = CreateBaseInputs();
        inputs.FannieProductType = FannieProductType.Conventional;

        var result = CalculationResultAssembler.Assemble(inputs);
        Assert.NotNull(result.FannieComplianceJson);

        var compliance = JsonSerializer.Deserialize<FannieComplianceResult>(result.FannieComplianceJson);
        Assert.NotNull(compliance);
        Assert.Equal(1.25m, compliance!.ProductMinDscr);
        Assert.Equal(80m, compliance.ProductMaxLtvPercent);
        Assert.Equal(30, compliance.ProductMaxAmortYears);
    }

    [Fact]
    public void Assembler_Conventional_65LTV_Passes_LTV()
    {
        var inputs = CreateBaseInputs();
        inputs.FannieProductType = FannieProductType.Conventional;
        inputs.LtvPercent = 65m;

        var result = CalculationResultAssembler.Assemble(inputs);
        var compliance = JsonSerializer.Deserialize<FannieComplianceResult>(result.FannieComplianceJson!);
        Assert.True(compliance!.LtvTest.Pass);
    }

    [Fact]
    public void Assembler_Conventional_85LTV_Fails_LTV()
    {
        var inputs = CreateBaseInputs();
        inputs.FannieProductType = FannieProductType.Conventional;
        inputs.LtvPercent = 85m;

        var result = CalculationResultAssembler.Assemble(inputs);
        var compliance = JsonSerializer.Deserialize<FannieComplianceResult>(result.FannieComplianceJson!);
        Assert.False(compliance!.LtvTest.Pass);
    }

    [Fact]
    public void Assembler_SeniorsAL_With_BlendedDscr()
    {
        var inputs = CreateBaseInputs();
        inputs.FannieProductType = FannieProductType.SeniorsAL;
        inputs.FannieInputs = new FannieComplianceInputs
        {
            ProductType = FannieProductType.SeniorsAL,
            IlBeds = 50,
            AlBeds = 30,
            AlzBeds = 20
        };

        var result = CalculationResultAssembler.Assemble(inputs);
        var compliance = JsonSerializer.Deserialize<FannieComplianceResult>(result.FannieComplianceJson!);
        Assert.NotNull(compliance!.SeniorsBlendedDscrTest);
        Assert.Equal(1.36m, compliance.SeniorsBlendedDscrTest!.RequiredValue);
    }

    [Fact]
    public void Assembler_Cooperative_With_DualDscr()
    {
        var inputs = CreateBaseInputs();
        inputs.FannieProductType = FannieProductType.Cooperative;
        inputs.FannieInputs = new FannieComplianceInputs
        {
            ProductType = FannieProductType.Cooperative,
            MarketRentalNoi = 2_000_000m
        };

        var result = CalculationResultAssembler.Assemble(inputs);
        var compliance = JsonSerializer.Deserialize<FannieComplianceResult>(result.FannieComplianceJson!);
        Assert.NotNull(compliance!.CoopActualDscrTest);
        Assert.NotNull(compliance.CoopMarketRentalDscrTest);
    }

    [Fact]
    public void Assembler_SARM_With_StressTest()
    {
        var inputs = CreateBaseInputs();
        inputs.FannieProductType = FannieProductType.SARM;
        inputs.FannieInputs = new FannieComplianceInputs
        {
            ProductType = FannieProductType.SARM,
            SarmMarginPercent = 2.0m,
            SarmCapStrikePercent = 4.0m
        };

        var result = CalculationResultAssembler.Assemble(inputs);
        var compliance = JsonSerializer.Deserialize<FannieComplianceResult>(result.FannieComplianceJson!);
        Assert.NotNull(compliance!.SarmStressDscrTest);
    }

    [Fact]
    public void Assembler_GreenRewards_With_NcfAdjustment()
    {
        var inputs = CreateBaseInputs();
        inputs.FannieProductType = FannieProductType.GreenRewards;
        inputs.FannieInputs = new FannieComplianceInputs
        {
            ProductType = FannieProductType.GreenRewards,
            OwnerProjectedSavings = 40_000m,
            TenantProjectedSavings = 20_000m
        };

        var result = CalculationResultAssembler.Assemble(inputs);
        var compliance = JsonSerializer.Deserialize<FannieComplianceResult>(result.FannieComplianceJson!);
        Assert.NotNull(compliance!.GreenNcfAdjustment);
        Assert.Equal(35_000m, compliance.GreenNcfAdjustment);
    }

    [Fact]
    public void Evaluate_OverallPass_WhenAllTestsPass()
    {
        var result = FannieComplianceCalculator.Evaluate(
            FannieProductType.Conventional,
            actualDscr: 1.50m,
            actualLtvPercent: 65m,
            actualAmortYears: 30,
            noi: 1_000_000m,
            annualDebtService: 600_000m,
            loanAmount: 10_000_000m,
            purchasePrice: 15_000_000m);

        Assert.True(result.OverallPass);
        Assert.True(result.DscrTest.Pass);
        Assert.True(result.LtvTest.Pass);
        Assert.True(result.AmortizationTest.Pass);
    }

    [Fact]
    public void Evaluate_OverallFail_WhenAnyTestFails()
    {
        var result = FannieComplianceCalculator.Evaluate(
            FannieProductType.Conventional,
            actualDscr: 1.10m, // Below 1.25x minimum
            actualLtvPercent: 65m,
            actualAmortYears: 30,
            noi: 1_000_000m,
            annualDebtService: 900_000m,
            loanAmount: 10_000_000m,
            purchasePrice: 15_000_000m);

        Assert.False(result.OverallPass);
        Assert.False(result.DscrTest.Pass);
    }

    // === Helpers ===

    private static CalculationInputs CreateBaseInputs() => new()
    {
        DealId = Guid.NewGuid(),
        RentPerUnit = 1_200m,
        UnitCount = 100,
        OccupancyPercent = 93m,
        PurchasePrice = 15_000_000m,
        LtvPercent = 65m,
        InterestRatePercent = 5.5m,
        IsInterestOnly = true,
        AmortizationYears = 30,
        HoldPeriodYears = 5,
        MarketCapRatePercent = 5.0m,
        AnnualGrowthRatePercents = new[] { 0m, 0m, 1.5m, 1.5m, 1.5m }
    };
}
