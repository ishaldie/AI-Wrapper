namespace ZSR.Underwriting.Domain.Interfaces;

public interface IUnderwritingCalculator
{
    decimal CalculateGpr(decimal rentPerUnit, int unitCount);
    decimal CalculateVacancyLoss(decimal gpr, decimal occupancyPercent);
    decimal CalculateNetRent(decimal gpr, decimal vacancyLoss);
    decimal CalculateOtherIncome(decimal netRent, decimal? actualOtherIncome = null, decimal otherIncomePercent = 0.135m);
    decimal CalculateEgi(decimal netRent, decimal otherIncome);
    decimal CalculateOperatingExpenses(decimal egi, decimal? actualExpenses, decimal opExRatio = 0.5435m);
    decimal CalculateNoi(decimal egi, decimal operatingExpenses);
    decimal CalculateNoiMargin(decimal noi, decimal egi);

    // Phase 2: Debt & Returns
    decimal CalculateDebtAmount(decimal purchasePrice, decimal ltvPercent);
    decimal CalculateAnnualDebtService(decimal debtAmount, decimal interestRatePercent, bool isInterestOnly, int amortizationYears);
    decimal CalculateAcquisitionCosts(decimal purchasePrice, decimal acqCostPercent = 0.02m);
    decimal CalculateEquityRequired(decimal purchasePrice, decimal acquisitionCosts, decimal debtAmount);
    decimal CalculateEntryCapRate(decimal noi, decimal purchasePrice);
    decimal CalculateExitCapRate(decimal marketCapRatePercent, decimal spreadPercent = 0.5m);
    decimal CalculateAnnualReserves(int unitCount, decimal reservesPerUnit = 250m);
    decimal CalculateCashOnCash(decimal noi, decimal annualDebtService, decimal annualReserves, decimal equityRequired);
    decimal CalculateDscr(decimal noi, decimal annualDebtService);

    // Phase 3: Multi-Year Projections & IRR
    decimal[] ProjectNoi(decimal baseNoi, decimal[] annualGrowthRatePercents);
    decimal[] ProjectCashFlows(decimal[] projectedNoi, decimal annualDebtService, decimal annualReserves);
    decimal CalculateExitValue(decimal terminalNoi, decimal exitCapRatePercent);
    decimal CalculateSaleCosts(decimal exitValue, decimal saleCostPercent = 0.02m);
    decimal CalculateLoanBalance(decimal originalDebtAmount, decimal interestRatePercent, bool isInterestOnly, int amortizationYears, int yearsHeld);
    decimal CalculateNetSaleProceeds(decimal exitValue, decimal saleCosts, decimal outstandingLoanBalance);
    decimal CalculateEquityMultiple(decimal[] annualCashFlows, decimal netSaleProceeds, decimal equityInvested);
    decimal CalculateIrr(decimal initialInvestment, decimal[] annualCashFlows, decimal terminalCashFlow);
}
