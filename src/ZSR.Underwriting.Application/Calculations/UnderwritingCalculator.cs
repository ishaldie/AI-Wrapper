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

    // Phase 3: Multi-Year Projections & IRR

    public decimal[] ProjectNoi(decimal baseNoi, decimal[] annualGrowthRatePercents)
    {
        var projected = new decimal[annualGrowthRatePercents.Length];
        var current = baseNoi;

        for (int i = 0; i < annualGrowthRatePercents.Length; i++)
        {
            current = Math.Round(current * (1m + annualGrowthRatePercents[i] / 100m), 2);
            projected[i] = current;
        }

        return projected;
    }

    public decimal[] ProjectCashFlows(decimal[] projectedNoi, decimal annualDebtService, decimal annualReserves)
    {
        var cashFlows = new decimal[projectedNoi.Length];

        for (int i = 0; i < projectedNoi.Length; i++)
        {
            cashFlows[i] = projectedNoi[i] - annualDebtService - annualReserves;
        }

        return cashFlows;
    }

    public decimal CalculateExitValue(decimal terminalNoi, decimal exitCapRatePercent)
    {
        if (exitCapRatePercent == 0m)
            return 0m;

        return Math.Round(terminalNoi / (exitCapRatePercent / 100m), 2);
    }

    public decimal CalculateSaleCosts(decimal exitValue, decimal saleCostPercent = 0.02m)
    {
        return Math.Round(exitValue * saleCostPercent, 2);
    }

    public decimal CalculateLoanBalance(decimal originalDebtAmount, decimal interestRatePercent, bool isInterestOnly, int amortizationYears, int yearsHeld)
    {
        if (isInterestOnly)
            return originalDebtAmount;

        if (interestRatePercent == 0m)
        {
            // Simple principal reduction for 0% rate
            var paymentsMade = yearsHeld * 12;
            var totalPayments = amortizationYears * 12;
            var monthlyPrincipal = originalDebtAmount / totalPayments;
            return Math.Max(0m, Math.Round(originalDebtAmount - monthlyPrincipal * paymentsMade, 2));
        }

        var monthlyRate = interestRatePercent / 100m / 12m;
        var n = amortizationYears * 12;
        var paymentsMadeCount = yearsHeld * 12;

        var compoundFactor = (decimal)Math.Pow((double)(1m + monthlyRate), n);
        var monthlyPayment = originalDebtAmount * (monthlyRate * compoundFactor) / (compoundFactor - 1m);

        // Remaining balance = P(1+r)^k - PMT × [(1+r)^k - 1] / r
        var compoundHeld = (decimal)Math.Pow((double)(1m + monthlyRate), paymentsMadeCount);
        var balance = originalDebtAmount * compoundHeld - monthlyPayment * (compoundHeld - 1m) / monthlyRate;

        return Math.Round(balance, 2);
    }

    public decimal CalculateNetSaleProceeds(decimal exitValue, decimal saleCosts, decimal outstandingLoanBalance)
    {
        return exitValue - saleCosts - outstandingLoanBalance;
    }

    public decimal CalculateEquityMultiple(decimal[] annualCashFlows, decimal netSaleProceeds, decimal equityInvested)
    {
        if (equityInvested == 0m)
            return 0m;

        var totalDistributions = annualCashFlows.Sum() + netSaleProceeds;
        return Math.Round(totalDistributions / equityInvested, 2);
    }

    public decimal CalculateIrr(decimal initialInvestment, decimal[] annualCashFlows, decimal terminalCashFlow)
    {
        if (initialInvestment == 0m)
            return 0m;

        // Newton-Raphson method to find rate where NPV = 0
        // CF0 = -initialInvestment
        // CF1..CFn-1 = annualCashFlows[0..n-2]
        // CFn = annualCashFlows[n-1] + terminalCashFlow

        var n = annualCashFlows.Length;
        var flows = new double[n + 1];
        flows[0] = -(double)initialInvestment;
        for (int i = 0; i < n; i++)
        {
            flows[i + 1] = (double)annualCashFlows[i];
        }
        flows[n] += (double)terminalCashFlow;

        double rate = 0.10; // Initial guess 10%
        const int maxIterations = 100;
        const double tolerance = 0.0001;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            double npv = 0;
            double dnpv = 0;

            for (int t = 0; t <= n; t++)
            {
                var discountFactor = Math.Pow(1 + rate, t);
                npv += flows[t] / discountFactor;
                if (t > 0)
                    dnpv -= t * flows[t] / Math.Pow(1 + rate, t + 1);
            }

            if (Math.Abs(dnpv) < 1e-12)
                break;

            var newRate = rate - npv / dnpv;
            if (Math.Abs(newRate - rate) < tolerance)
            {
                rate = newRate;
                break;
            }
            rate = newRate;
        }

        return Math.Round((decimal)(rate * 100), 1);
    }
}
