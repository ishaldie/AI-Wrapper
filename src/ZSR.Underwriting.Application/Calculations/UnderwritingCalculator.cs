using ZSR.Underwriting.Domain.Interfaces;

namespace ZSR.Underwriting.Application.Calculations;

public class UnderwritingCalculator : IUnderwritingCalculator
{
    public decimal CalculateGpr(decimal rentPerUnit, int unitCount)
    {
        return rentPerUnit * unitCount * 12;
    }

    public decimal CalculateVacancyLoss(decimal gpr, decimal occupancyPercent)
    {
        var vacancyRate = 1m - (occupancyPercent / 100m);
        return gpr * vacancyRate;
    }

    public decimal CalculateNetRent(decimal gpr, decimal vacancyLoss)
    {
        return gpr - vacancyLoss;
    }

    public decimal CalculateOtherIncome(decimal netRent, decimal? actualOtherIncome = null, decimal otherIncomePercent = 0.135m)
    {
        if (actualOtherIncome.HasValue)
            return actualOtherIncome.Value;

        return netRent * otherIncomePercent;
    }

    public decimal CalculateEgi(decimal netRent, decimal otherIncome)
    {
        return netRent + otherIncome;
    }

    public decimal CalculateOperatingExpenses(decimal egi, decimal? actualExpenses, decimal opExRatio = 0.5435m)
    {
        if (actualExpenses.HasValue)
            return actualExpenses.Value;

        return Math.Round(egi * opExRatio, 2);
    }

    public decimal CalculateNoi(decimal egi, decimal operatingExpenses)
    {
        return egi - operatingExpenses;
    }

    public decimal CalculateNoiMargin(decimal noi, decimal egi)
    {
        if (egi == 0m)
            return 0m;

        return Math.Round((noi / egi) * 100m, 1);
    }

    // Phase 2: Debt & Returns

    public decimal CalculateDebtAmount(decimal purchasePrice, decimal ltvPercent)
    {
        return purchasePrice * (ltvPercent / 100m);
    }

    public decimal CalculateAnnualDebtService(decimal debtAmount, decimal interestRatePercent, bool isInterestOnly, int amortizationYears)
    {
        if (debtAmount == 0m)
            return 0m;

        var annualRate = interestRatePercent / 100m;

        if (isInterestOnly || annualRate == 0m)
            return Math.Round(debtAmount * annualRate, 2);

        // Standard amortization formula: P × [r(1+r)^n / ((1+r)^n - 1)]
        var monthlyRate = annualRate / 12m;
        var totalPayments = amortizationYears * 12;

        // Use double for the power calculation, then back to decimal
        var compoundFactor = (decimal)Math.Pow((double)(1m + monthlyRate), totalPayments);
        var monthlyPayment = debtAmount * (monthlyRate * compoundFactor) / (compoundFactor - 1m);

        return Math.Round(monthlyPayment * 12m, 2);
    }

    public decimal CalculateAcquisitionCosts(decimal purchasePrice, decimal acqCostPercent = 0.02m)
    {
        return purchasePrice * acqCostPercent;
    }

    public decimal CalculateEquityRequired(decimal purchasePrice, decimal acquisitionCosts, decimal debtAmount)
    {
        return purchasePrice + acquisitionCosts - debtAmount;
    }

    public decimal CalculateEntryCapRate(decimal noi, decimal purchasePrice)
    {
        if (purchasePrice == 0m)
            return 0m;

        return Math.Round((noi / purchasePrice) * 100m, 1);
    }

    public decimal CalculateExitCapRate(decimal marketCapRatePercent, decimal spreadPercent = 0.5m)
    {
        return marketCapRatePercent + spreadPercent;
    }

    public decimal CalculateAnnualReserves(int unitCount, decimal reservesPerUnit = 250m)
    {
        return unitCount * reservesPerUnit;
    }

    public decimal CalculateCashOnCash(decimal noi, decimal annualDebtService, decimal annualReserves, decimal equityRequired)
    {
        if (equityRequired == 0m)
            return 0m;

        var cashFlow = noi - annualDebtService - annualReserves;
        return Math.Round((cashFlow / equityRequired) * 100m, 1);
    }

    public decimal CalculateDscr(decimal noi, decimal annualDebtService)
    {
        if (annualDebtService == 0m)
            return 0m;

        return Math.Round(noi / annualDebtService, 2);
    }
}
