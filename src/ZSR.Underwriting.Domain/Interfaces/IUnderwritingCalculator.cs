namespace ZSR.Underwriting.Domain.Interfaces;

/// <summary>
/// Core CRE underwriting calculations covering income analysis, debt sizing, multi-year projections, and comp adjustments.
/// All percentage parameters use whole numbers (e.g., 95 for 95%, 5.5 for 5.5%) unless noted otherwise.
/// </summary>
public interface IUnderwritingCalculator
{
    // ── Phase 1: Income Analysis ──

    /// <summary>Gross Potential Rent = rentPerUnit × unitCount × 12.</summary>
    decimal CalculateGpr(decimal rentPerUnit, int unitCount);

    /// <summary>Dollar vacancy loss = GPR × (1 − occupancyPercent / 100).</summary>
    decimal CalculateVacancyLoss(decimal gpr, decimal occupancyPercent);

    /// <summary>GPR minus vacancy loss.</summary>
    decimal CalculateNetRent(decimal gpr, decimal vacancyLoss);

    /// <summary>Returns <paramref name="actualOtherIncome"/> if provided; otherwise netRent × <paramref name="otherIncomePercent"/>.</summary>
    decimal CalculateOtherIncome(decimal netRent, decimal? actualOtherIncome = null, decimal otherIncomePercent = 0.135m);

    /// <summary>Effective Gross Income = netRent + otherIncome.</summary>
    decimal CalculateEgi(decimal netRent, decimal otherIncome);

    /// <summary>Returns <paramref name="actualExpenses"/> if provided; otherwise EGI × <paramref name="opExRatio"/>.</summary>
    decimal CalculateOperatingExpenses(decimal egi, decimal? actualExpenses, decimal opExRatio = 0.5435m);

    /// <summary>Net Operating Income = EGI − operating expenses.</summary>
    decimal CalculateNoi(decimal egi, decimal operatingExpenses);

    /// <summary>NOI margin as a percentage of EGI (returns 0 when EGI is zero).</summary>
    decimal CalculateNoiMargin(decimal noi, decimal egi);

    // ── Phase 2: Debt & Returns ──

    /// <summary>Loan amount = purchasePrice × ltvPercent / 100.</summary>
    decimal CalculateDebtAmount(decimal purchasePrice, decimal ltvPercent);

    /// <summary>Annual debt service using standard amortization (or interest-only when applicable).</summary>
    decimal CalculateAnnualDebtService(decimal debtAmount, decimal interestRatePercent, bool isInterestOnly, int amortizationYears);

    /// <summary>Closing / acquisition costs = purchasePrice × <paramref name="acqCostPercent"/>.</summary>
    decimal CalculateAcquisitionCosts(decimal purchasePrice, decimal acqCostPercent = 0.02m);

    /// <summary>Equity required = purchasePrice + acquisitionCosts − debtAmount.</summary>
    decimal CalculateEquityRequired(decimal purchasePrice, decimal acquisitionCosts, decimal debtAmount);

    /// <summary>Entry (going-in) cap rate = NOI / purchasePrice × 100.</summary>
    decimal CalculateEntryCapRate(decimal noi, decimal purchasePrice);

    /// <summary>Exit cap rate = market cap rate + spread (both in percentage points).</summary>
    decimal CalculateExitCapRate(decimal marketCapRatePercent, decimal spreadPercent = 0.5m);

    /// <summary>Annual replacement reserves = unitCount × per-unit reserve amount.</summary>
    decimal CalculateAnnualReserves(int unitCount, decimal reservesPerUnit = 250m);

    /// <summary>Cash-on-cash return = (NOI − debt service − reserves) / equity × 100.</summary>
    decimal CalculateCashOnCash(decimal noi, decimal annualDebtService, decimal annualReserves, decimal equityRequired);

    /// <summary>Debt Service Coverage Ratio = NOI / annual debt service.</summary>
    decimal CalculateDscr(decimal noi, decimal annualDebtService);

    // ── Phase 3: Multi-Year Projections & IRR ──

    /// <summary>Projects NOI forward by applying per-year growth rates (compound).</summary>
    decimal[] ProjectNoi(decimal baseNoi, decimal[] annualGrowthRatePercents);

    /// <summary>Annual levered cash flows = projected NOI − debt service − reserves.</summary>
    decimal[] ProjectCashFlows(decimal[] projectedNoi, decimal annualDebtService, decimal annualReserves);

    /// <summary>Reversion value = terminal NOI / (exit cap rate / 100).</summary>
    decimal CalculateExitValue(decimal terminalNoi, decimal exitCapRatePercent);

    /// <summary>Disposition costs = exitValue × <paramref name="saleCostPercent"/>.</summary>
    decimal CalculateSaleCosts(decimal exitValue, decimal saleCostPercent = 0.02m);

    /// <summary>Outstanding loan balance after <paramref name="yearsHeld"/> years of amortization (or full balance if IO).</summary>
    decimal CalculateLoanBalance(decimal originalDebtAmount, decimal interestRatePercent, bool isInterestOnly, int amortizationYears, int yearsHeld);

    /// <summary>Net sale proceeds = exit value − sale costs − outstanding loan balance.</summary>
    decimal CalculateNetSaleProceeds(decimal exitValue, decimal saleCosts, decimal outstandingLoanBalance);

    /// <summary>Equity multiple = (sum of cash flows + net sale proceeds) / equity invested.</summary>
    decimal CalculateEquityMultiple(decimal[] annualCashFlows, decimal netSaleProceeds, decimal equityInvested);

    /// <summary>Leveraged IRR via Newton-Raphson, returned as a percentage (e.g., 15.2 for 15.2%).</summary>
    decimal CalculateIrr(decimal initialInvestment, decimal[] annualCashFlows, decimal terminalCashFlow);

    // ── Phase 4: Sales Comp Adjustments ──

    /// <summary>Applies cumulative percentage adjustments (time, size, age, location, amenities) to a raw comp price per unit.</summary>
    decimal AdjustCompPricePerUnit(decimal rawPricePerUnit, decimal timeAdjPercent, decimal sizeAdjPercent, decimal ageAdjPercent, decimal locationAdjPercent, decimal amenitiesAdjPercent);
}
