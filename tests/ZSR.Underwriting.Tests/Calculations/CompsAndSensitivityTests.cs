using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Calculations;

public class CompsAndSensitivityTests
{
    private readonly IUnderwritingCalculator _calc = new UnderwritingCalculator();

    // ==================== Sales Comp Adjustments ====================

    [Fact]
    public void AdjustCompPricePerUnit_NoAdjustments_ReturnsOriginal()
    {
        var result = _calc.AdjustCompPricePerUnit(95_000m, 0m, 0m, 0m, 0m, 0m);
        Assert.Equal(95_000m, result);
    }

    [Fact]
    public void AdjustCompPricePerUnit_PositiveTimeAdjustment_IncreasesPrice()
    {
        // Time adjustment +3% (older sale, prices have risen)
        var result = _calc.AdjustCompPricePerUnit(95_000m, 3m, 0m, 0m, 0m, 0m);
        Assert.Equal(97_850m, result);
    }

    [Fact]
    public void AdjustCompPricePerUnit_NegativeSizeAdjustment_DecreasesPrice()
    {
        // Size adjustment -2% (comp is smaller, worth more per unit, so adjust down)
        var result = _calc.AdjustCompPricePerUnit(95_000m, 0m, -2m, 0m, 0m, 0m);
        Assert.Equal(93_100m, result);
    }

    [Fact]
    public void AdjustCompPricePerUnit_MultipleAdjustments_AppliesAll()
    {
        // Time +3%, Size -2%, Age -5%, Location +2%, Amenities +1% = net -1%
        var result = _calc.AdjustCompPricePerUnit(100_000m, 3m, -2m, -5m, 2m, 1m);
        Assert.Equal(99_000m, result);
    }

    [Fact]
    public void AdjustCompPricePerUnit_LargeNegativeAdjustment_HandlesCorrectly()
    {
        var result = _calc.AdjustCompPricePerUnit(100_000m, -10m, -10m, -10m, -10m, -10m);
        Assert.Equal(50_000m, result);
    }

    // ==================== Sensitivity Scenarios ====================

    [Fact]
    public void CalculateSensitivityNoi_IncomeDown5Percent_ReducesNoi()
    {
        var baseNoi = 693_876.35m;
        var result = SensitivityCalculator.CalculateIncomeStressNoi(
            gpr: 1_440_000m, occupancyPercent: 93m, otherIncomePercent: 0.135m,
            opExRatio: 0.5435m, incomeReductionPercent: 5m);

        Assert.True(result < baseNoi);
        Assert.True(result > 0m);
    }

    [Fact]
    public void CalculateSensitivityNoi_OccupancyDown10Percent_ReducesNoi()
    {
        var baseNoi = 693_876.35m;
        var result = SensitivityCalculator.CalculateOccupancyStressNoi(
            gpr: 1_440_000m, baseOccupancyPercent: 93m, occupancyDropPercent: 10m,
            otherIncomePercent: 0.135m, opExRatio: 0.5435m);

        Assert.True(result < baseNoi);
        Assert.True(result > 0m);
    }

    [Fact]
    public void CalculateSensitivityNoi_CapRateUp100Bps_DoesNotAffectNoi()
    {
        // Cap rate change affects exit value, not NOI
        // But we verify the exit value impact
        var baseExitCap = 5.5m;
        var stressedExitCap = baseExitCap + 1.0m; // +100bps
        var terminalNoi = 700_000m;

        var baseExitValue = _calc.CalculateExitValue(terminalNoi, baseExitCap);
        var stressedExitValue = _calc.CalculateExitValue(terminalNoi, stressedExitCap);

        Assert.True(stressedExitValue < baseExitValue);
    }

    [Fact]
    public void CalculateSensitivityScenarios_Returns4Scenarios()
    {
        var scenarios = SensitivityCalculator.RunScenarios(
            gpr: 1_440_000m, occupancyPercent: 93m, otherIncomePercent: 0.135m,
            opExRatio: 0.5435m, purchasePrice: 15_000_000m,
            debtService: 536_250m, reserves: 25_000m, equityRequired: 5_550_000m,
            exitCapPercent: 5.5m, terminalNoi: 700_000m);

        Assert.Equal(4, scenarios.Count);
        Assert.Equal("Base Case", scenarios[0].Name);
        Assert.Equal("Income -5%", scenarios[1].Name);
        Assert.Equal("Occupancy -10%", scenarios[2].Name);
        Assert.Equal("Cap Rate +100bps", scenarios[3].Name);
    }

    [Fact]
    public void CalculateSensitivityScenarios_BaseCaseMatchesDirectCalculation()
    {
        decimal gpr = 1_440_000m;
        decimal occupancy = 93m;
        var vacancyLoss = _calc.CalculateVacancyLoss(gpr, occupancy);
        var netRent = _calc.CalculateNetRent(gpr, vacancyLoss);
        var otherIncome = _calc.CalculateOtherIncome(netRent);
        var egi = _calc.CalculateEgi(netRent, otherIncome);
        var opEx = _calc.CalculateOperatingExpenses(egi, null);
        var baseNoi = _calc.CalculateNoi(egi, opEx);

        var scenarios = SensitivityCalculator.RunScenarios(
            gpr, occupancy, 0.135m, 0.5435m, 15_000_000m,
            536_250m, 25_000m, 5_550_000m, 5.5m, 700_000m);

        Assert.Equal(baseNoi, scenarios[0].Noi);
    }

    [Fact]
    public void CalculateSensitivityScenarios_StressedScenariosHaveNegativeDeltas()
    {
        var scenarios = SensitivityCalculator.RunScenarios(
            gpr: 1_440_000m, occupancyPercent: 93m, otherIncomePercent: 0.135m,
            opExRatio: 0.5435m, purchasePrice: 15_000_000m,
            debtService: 536_250m, reserves: 25_000m, equityRequired: 5_550_000m,
            exitCapPercent: 5.5m, terminalNoi: 700_000m);

        // Income stress: lower NOI
        Assert.True(scenarios[1].NoiDelta < 0);
        // Occupancy stress: lower NOI
        Assert.True(scenarios[2].NoiDelta < 0);
        // Cap rate stress: lower exit value (NOI unchanged)
        Assert.Equal(0m, scenarios[3].NoiDelta);
        Assert.True(scenarios[3].ExitValueDelta < 0);
    }

    // ==================== Risk Severity Ratings ====================

    [Fact]
    public void RateRentPremium_Under5Percent_ReturnsLow()
    {
        var result = RiskRatingCalculator.RateRentPremium(subjectRent: 1200m, marketRent: 1180m);
        Assert.Equal(RiskSeverity.Low, result);
    }

    [Fact]
    public void RateRentPremium_5To10Percent_ReturnsModerate()
    {
        var result = RiskRatingCalculator.RateRentPremium(subjectRent: 1300m, marketRent: 1200m);
        Assert.Equal(RiskSeverity.Moderate, result);
    }

    [Fact]
    public void RateRentPremium_Over15Percent_ReturnsCritical()
    {
        var result = RiskRatingCalculator.RateRentPremium(subjectRent: 1400m, marketRent: 1200m);
        Assert.Equal(RiskSeverity.Critical, result);
    }

    [Fact]
    public void RateDscr_Above125_ReturnsLow()
    {
        var result = RiskRatingCalculator.RateDscr(1.30m);
        Assert.Equal(RiskSeverity.Low, result);
    }

    [Fact]
    public void RateDscr_Between1And115_ReturnsHigh()
    {
        var result = RiskRatingCalculator.RateDscr(1.10m);
        Assert.Equal(RiskSeverity.High, result);
    }

    [Fact]
    public void RateDscr_BelowOne_ReturnsCritical()
    {
        var result = RiskRatingCalculator.RateDscr(0.95m);
        Assert.Equal(RiskSeverity.Critical, result);
    }

    [Fact]
    public void RateOccupancyGap_SmallGap_ReturnsLow()
    {
        var result = RiskRatingCalculator.RateOccupancyGap(subjectOccupancy: 93m, marketOccupancy: 95m);
        Assert.Equal(RiskSeverity.Low, result);
    }

    [Fact]
    public void RateOccupancyGap_LargeGap_ReturnsHigh()
    {
        var result = RiskRatingCalculator.RateOccupancyGap(subjectOccupancy: 80m, marketOccupancy: 95m);
        Assert.Equal(RiskSeverity.High, result);
    }

    [Fact]
    public void RateFicoGap_SmallGap_ReturnsLow()
    {
        var result = RiskRatingCalculator.RateFicoGap(subjectFico: 710, metroFico: 720);
        Assert.Equal(RiskSeverity.Low, result);
    }

    [Fact]
    public void RateFicoGap_LargeGap_ReturnsCritical()
    {
        var result = RiskRatingCalculator.RateFicoGap(subjectFico: 620, metroFico: 720);
        Assert.Equal(RiskSeverity.Critical, result);
    }

    // ==================== CalculationResult Assembler ====================

    [Fact]
    public void Assemble_PopulatesRevenueMetrics()
    {
        var result = CreateAssembledResult();

        Assert.NotNull(result.GrossPotentialRent);
        Assert.NotNull(result.EffectiveGrossIncome);
        Assert.NotNull(result.NetOperatingIncome);
        Assert.NotNull(result.NoiMargin);
        Assert.Equal(1_440_000m, result.GrossPotentialRent);
    }

    [Fact]
    public void Assemble_PopulatesDebtMetrics()
    {
        var result = CreateAssembledResult();

        Assert.NotNull(result.LoanAmount);
        Assert.NotNull(result.AnnualDebtService);
        Assert.NotNull(result.DebtServiceCoverageRatio);
        Assert.Equal(9_750_000m, result.LoanAmount);
    }

    [Fact]
    public void Assemble_PopulatesReturnMetrics()
    {
        var result = CreateAssembledResult();

        Assert.NotNull(result.CashOnCashReturn);
        Assert.NotNull(result.InternalRateOfReturn);
        Assert.NotNull(result.EquityMultiple);
        Assert.NotNull(result.GoingInCapRate);
    }

    [Fact]
    public void Assemble_PopulatesCashFlowJson()
    {
        var result = CreateAssembledResult();

        Assert.NotNull(result.CashFlowProjectionsJson);
        Assert.Contains("cashFlows", result.CashFlowProjectionsJson!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Assemble_PopulatesSensitivityJson()
    {
        var result = CreateAssembledResult();

        Assert.NotNull(result.SensitivityAnalysisJson);
        Assert.Contains("Base Case", result.SensitivityAnalysisJson!);
    }

    [Fact]
    public void Assemble_SetsDealIdAndTimestamp()
    {
        var result = CreateAssembledResult();

        Assert.NotEqual(Guid.Empty, result.DealId);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    // --- Helper ---

    private static ZSR.Underwriting.Domain.Entities.CalculationResult CreateAssembledResult()
    {
        var inputs = new CalculationInputs
        {
            DealId = Guid.NewGuid(),
            RentPerUnit = 1200m,
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

        return CalculationResultAssembler.Assemble(inputs);
    }
}
