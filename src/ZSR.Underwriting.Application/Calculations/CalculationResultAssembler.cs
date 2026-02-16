using System.Text.Json;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Calculations;

public static class CalculationResultAssembler
{
    public static CalculationResult Assemble(CalculationInputs inputs)
    {
        var calc = new UnderwritingCalculator();
        var result = new CalculationResult(inputs.DealId);

        // Phase 1: Revenue & NOI
        var gpr = calc.CalculateGpr(inputs.RentPerUnit, inputs.UnitCount);
        var vacancyLoss = calc.CalculateVacancyLoss(gpr, inputs.OccupancyPercent);
        var netRent = calc.CalculateNetRent(gpr, vacancyLoss);
        var otherIncome = calc.CalculateOtherIncome(netRent);
        var egi = calc.CalculateEgi(netRent, otherIncome);
        var opEx = calc.CalculateOperatingExpenses(egi, null);
        var noi = calc.CalculateNoi(egi, opEx);
        var noiMargin = calc.CalculateNoiMargin(noi, egi);

        result.GrossPotentialRent = gpr;
        result.VacancyLoss = vacancyLoss;
        result.EffectiveGrossIncome = egi;
        result.OtherIncome = otherIncome;
        result.OperatingExpenses = opEx;
        result.NetOperatingIncome = noi;
        result.NoiMargin = noiMargin;

        // Phase 2: Debt & Returns
        var debtAmount = calc.CalculateDebtAmount(inputs.PurchasePrice, inputs.LtvPercent);
        var debtService = calc.CalculateAnnualDebtService(debtAmount, inputs.InterestRatePercent, inputs.IsInterestOnly, inputs.AmortizationYears);
        var acqCosts = calc.CalculateAcquisitionCosts(inputs.PurchasePrice);
        var equityRequired = calc.CalculateEquityRequired(inputs.PurchasePrice, acqCosts, debtAmount);
        var entryCapRate = calc.CalculateEntryCapRate(noi, inputs.PurchasePrice);
        var exitCapRate = calc.CalculateExitCapRate(inputs.MarketCapRatePercent);
        var reserves = calc.CalculateAnnualReserves(inputs.UnitCount);
        var cashOnCash = calc.CalculateCashOnCash(noi, debtService, reserves, equityRequired);
        var dscr = calc.CalculateDscr(noi, debtService);

        result.LoanAmount = debtAmount;
        result.AnnualDebtService = debtService;
        result.DebtServiceCoverageRatio = dscr;
        result.CashOnCashReturn = cashOnCash;
        result.GoingInCapRate = entryCapRate;
        result.ExitCapRate = exitCapRate;
        result.PricePerUnit = inputs.PurchasePrice / inputs.UnitCount;

        // Phase 3: Projections & IRR
        var projectedNoi = calc.ProjectNoi(noi, inputs.AnnualGrowthRatePercents);
        var cashFlows = calc.ProjectCashFlows(projectedNoi, debtService, reserves);
        var terminalNoi = projectedNoi[^1];
        var exitValue = calc.CalculateExitValue(terminalNoi, exitCapRate);
        var saleCosts = calc.CalculateSaleCosts(exitValue);
        var loanBalance = calc.CalculateLoanBalance(debtAmount, inputs.InterestRatePercent, inputs.IsInterestOnly, inputs.AmortizationYears, inputs.HoldPeriodYears);
        var netSaleProceeds = calc.CalculateNetSaleProceeds(exitValue, saleCosts, loanBalance);
        var equityMultiple = calc.CalculateEquityMultiple(cashFlows, netSaleProceeds, equityRequired);
        var irr = calc.CalculateIrr(equityRequired, cashFlows, netSaleProceeds);

        result.InternalRateOfReturn = irr;
        result.EquityMultiple = equityMultiple;
        result.ExitValue = exitValue;
        result.TotalProfit = netSaleProceeds + cashFlows.Sum() - equityRequired;

        // Cash flow projections JSON
        result.CashFlowProjectionsJson = JsonSerializer.Serialize(new { cashFlows, projectedNoi });

        // Sensitivity analysis
        var scenarios = SensitivityCalculator.RunScenarios(
            gpr, inputs.OccupancyPercent, 0.135m, 0.5435m,
            inputs.PurchasePrice, debtService, reserves, equityRequired,
            exitCapRate, terminalNoi);
        result.SensitivityAnalysisJson = JsonSerializer.Serialize(scenarios);

        return result;
    }
}
