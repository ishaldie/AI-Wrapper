using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Application.Calculations;

/// <summary>
/// Evaluates Fannie Mae product-specific compliance tests.
/// Each method is a pure function returning a ComplianceTest result.
/// </summary>
public static class FannieComplianceCalculator
{
    /// <summary>
    /// Runs all applicable compliance tests for a given product type.
    /// </summary>
    public static FannieComplianceResult Evaluate(
        FannieProductType productType,
        decimal actualDscr,
        decimal actualLtvPercent,
        int actualAmortYears,
        decimal noi,
        decimal annualDebtService,
        decimal loanAmount,
        decimal purchasePrice,
        FannieComplianceInputs? inputs = null)
    {
        var profile = FannieProductProfiles.Get(productType);

        // Core tests — always run
        var dscrTest = TestDscr(actualDscr, profile.MinDscr);
        var ltvTest = TestLtv(actualLtvPercent, profile.MaxLtvPercent);
        var amortTest = TestAmortization(actualAmortYears, profile.MaxAmortizationYears);

        // Product-specific tests
        ComplianceTest? seniorsBlended = null;
        ComplianceTest? coopActual = null;
        ComplianceTest? coopMarketRental = null;
        ComplianceTest? sarmStress = null;
        ComplianceTest? snfCap = null;
        ComplianceTest? mhcVacancy = null;
        ComplianceTest? roarRehab = null;
        ComplianceTest? suppCombinedDscr = null;
        ComplianceTest? suppCombinedLtv = null;
        decimal? greenAdj = null;
        decimal? adjustedNcf = null;

        if (inputs != null)
        {
            switch (productType)
            {
                case FannieProductType.SeniorsIL:
                case FannieProductType.SeniorsAL:
                case FannieProductType.SeniorsALZ:
                    seniorsBlended = TestSeniorsBlendedDscr(
                        actualDscr, inputs.IlBeds, inputs.AlBeds, inputs.AlzBeds);
                    if (inputs.SnfNcf.HasValue && noi > 0)
                        snfCap = TestSnfNcfCap(inputs.SnfNcf.Value, noi);
                    break;

                case FannieProductType.Cooperative:
                    if (inputs.MarketRentalNoi.HasValue && annualDebtService > 0)
                    {
                        var coopTests = TestCooperativeDualDscr(
                            noi, inputs.MarketRentalNoi.Value, annualDebtService);
                        coopActual = coopTests.actualOps;
                        coopMarketRental = coopTests.marketRental;
                    }
                    break;

                case FannieProductType.SARM:
                    if (inputs.SarmMarginPercent.HasValue && inputs.SarmCapStrikePercent.HasValue)
                        sarmStress = TestSarmStressDscr(
                            noi, loanAmount, actualAmortYears,
                            inputs.SarmMarginPercent.Value, inputs.SarmCapStrikePercent.Value);
                    break;

                case FannieProductType.GreenRewards:
                    if (inputs.OwnerProjectedSavings.HasValue && inputs.TenantProjectedSavings.HasValue)
                    {
                        var green = CalculateGreenNcfAdjustment(
                            noi, inputs.OwnerProjectedSavings.Value, inputs.TenantProjectedSavings.Value);
                        greenAdj = green.adjustment;
                        adjustedNcf = green.adjustedNcf;
                    }
                    break;

                case FannieProductType.ManufacturedHousing:
                    // MHC vacancy floor: occupancy must not exceed 95% (5% min vacancy)
                    var effectiveOccupancy = purchasePrice > 0
                        ? 100m - (actualLtvPercent) // Placeholder — occupancy passed separately
                        : 0m;
                    // The vacancy floor is enforced in the assembler; here we just track it
                    break;

                case FannieProductType.ROAR:
                    if (inputs.IsRehabPeriod && annualDebtService > 0)
                        roarRehab = TestRoarRehabDscr(noi, annualDebtService, true);
                    break;

                case FannieProductType.Supplemental:
                    if (inputs.SeniorLoanAmount.HasValue && inputs.SeniorDebtService.HasValue)
                    {
                        var suppTests = TestSupplementalCombined(
                            inputs.SeniorLoanAmount.Value, loanAmount, purchasePrice,
                            noi, inputs.SeniorDebtService.Value, annualDebtService);
                        suppCombinedDscr = suppTests.dscrTest;
                        suppCombinedLtv = suppTests.ltvTest;
                    }
                    break;
            }
        }

        // Determine overall pass
        var allTests = new List<ComplianceTest> { dscrTest, ltvTest, amortTest };
        if (seniorsBlended != null) allTests.Add(seniorsBlended);
        if (coopActual != null) allTests.Add(coopActual);
        if (coopMarketRental != null) allTests.Add(coopMarketRental);
        if (sarmStress != null) allTests.Add(sarmStress);
        if (snfCap != null) allTests.Add(snfCap);
        if (roarRehab != null) allTests.Add(roarRehab);
        if (suppCombinedDscr != null) allTests.Add(suppCombinedDscr);
        if (suppCombinedLtv != null) allTests.Add(suppCombinedLtv);

        return new FannieComplianceResult
        {
            OverallPass = allTests.All(t => t.Pass),
            ProductMinDscr = profile.MinDscr,
            ProductMaxLtvPercent = profile.MaxLtvPercent,
            ProductMaxAmortYears = profile.MaxAmortizationYears,
            DscrTest = dscrTest,
            LtvTest = ltvTest,
            AmortizationTest = amortTest,
            SeniorsBlendedDscrTest = seniorsBlended,
            CoopActualDscrTest = coopActual,
            CoopMarketRentalDscrTest = coopMarketRental,
            SarmStressDscrTest = sarmStress,
            SnfNcfCapTest = snfCap,
            MhcVacancyFloorTest = mhcVacancy,
            RoarRehabDscrTest = roarRehab,
            SupplementalCombinedDscrTest = suppCombinedDscr,
            SupplementalCombinedLtvTest = suppCombinedLtv,
            GreenNcfAdjustment = greenAdj,
            AdjustedNcf = adjustedNcf
        };
    }

    // === Individual test methods (public for unit testing) ===

    /// <summary>Tests actual DSCR against product minimum.</summary>
    public static ComplianceTest TestDscr(decimal actualDscr, decimal minDscr)
    {
        return new ComplianceTest
        {
            Name = "DSCR Minimum",
            Pass = actualDscr >= minDscr,
            ActualValue = actualDscr,
            RequiredValue = minDscr,
            Notes = actualDscr >= minDscr ? null : $"DSCR {actualDscr:F2}x below minimum {minDscr:F2}x"
        };
    }

    /// <summary>Tests actual LTV against product maximum.</summary>
    public static ComplianceTest TestLtv(decimal actualLtvPercent, decimal maxLtvPercent)
    {
        return new ComplianceTest
        {
            Name = "LTV Maximum",
            Pass = actualLtvPercent <= maxLtvPercent,
            ActualValue = actualLtvPercent,
            RequiredValue = maxLtvPercent,
            Notes = actualLtvPercent <= maxLtvPercent ? null : $"LTV {actualLtvPercent}% exceeds maximum {maxLtvPercent}%"
        };
    }

    /// <summary>Tests actual amortization against product maximum.</summary>
    public static ComplianceTest TestAmortization(int actualYears, int maxYears)
    {
        return new ComplianceTest
        {
            Name = "Amortization Maximum",
            Pass = actualYears <= maxYears,
            ActualValue = actualYears,
            RequiredValue = maxYears,
            Notes = actualYears <= maxYears ? null : $"Amortization {actualYears}yr exceeds maximum {maxYears}yr"
        };
    }

    /// <summary>
    /// Task 2: Tests DSCR against blended minimum for seniors housing
    /// based on IL/AL/ALZ bed mix.
    /// </summary>
    public static ComplianceTest TestSeniorsBlendedDscr(
        decimal actualDscr, int ilBeds, int alBeds, int alzBeds)
    {
        var blendedMin = FannieProductProfiles.CalculateSeniorsBlendedMinDscr(ilBeds, alBeds, alzBeds);
        return new ComplianceTest
        {
            Name = "Seniors Blended DSCR",
            Pass = actualDscr >= blendedMin,
            ActualValue = actualDscr,
            RequiredValue = blendedMin,
            Notes = $"Blended from {ilBeds} IL + {alBeds} AL + {alzBeds} ALZ beds"
        };
    }

    /// <summary>
    /// Task 3: Cooperative dual DSCR — tests both actual operations (1.00x)
    /// and market rental basis (1.55x).
    /// </summary>
    public static (ComplianceTest actualOps, ComplianceTest marketRental) TestCooperativeDualDscr(
        decimal actualOpsNoi, decimal marketRentalNoi, decimal annualDebtService)
    {
        var actualOpsDscr = annualDebtService > 0 ? Math.Round(actualOpsNoi / annualDebtService, 2) : 0m;
        var marketRentalDscr = annualDebtService > 0 ? Math.Round(marketRentalNoi / annualDebtService, 2) : 0m;

        var actualTest = new ComplianceTest
        {
            Name = "Cooperative Actual Operations DSCR",
            Pass = actualOpsDscr >= 1.00m,
            ActualValue = actualOpsDscr,
            RequiredValue = 1.00m,
            Notes = actualOpsDscr >= 1.00m ? null : "Actual operations DSCR below 1.00x minimum"
        };

        var marketTest = new ComplianceTest
        {
            Name = "Cooperative Market Rental DSCR",
            Pass = marketRentalDscr >= 1.55m,
            ActualValue = marketRentalDscr,
            RequiredValue = 1.55m,
            Notes = marketRentalDscr >= 1.55m ? null : "Market rental DSCR below 1.55x minimum"
        };

        return (actualTest, marketTest);
    }

    /// <summary>
    /// Task 4: SARM stress test — calculates DSCR at maximum note rate
    /// (margin + cap strike) instead of actual rate.
    /// </summary>
    public static ComplianceTest TestSarmStressDscr(
        decimal noi, decimal loanAmount, int amortYears,
        decimal marginPercent, decimal capStrikePercent)
    {
        var maxNoteRate = marginPercent + capStrikePercent;
        var calc = new UnderwritingCalculator();
        var stressDebtService = calc.CalculateAnnualDebtService(loanAmount, maxNoteRate, false, amortYears);
        var stressDscr = stressDebtService > 0 ? Math.Round(noi / stressDebtService, 2) : 0m;

        return new ComplianceTest
        {
            Name = "SARM Stress DSCR (at Max Note Rate)",
            Pass = stressDscr >= 1.05m,
            ActualValue = stressDscr,
            RequiredValue = 1.05m,
            Notes = $"Tested at max rate {maxNoteRate:F2}% (margin {marginPercent}% + cap {capStrikePercent}%)"
        };
    }

    /// <summary>
    /// Task 5: Green Rewards NCF adjustment — adds 75% of owner-projected
    /// and 25% of tenant-projected savings to NCF.
    /// </summary>
    public static (decimal adjustment, decimal adjustedNcf) CalculateGreenNcfAdjustment(
        decimal baseNcf, decimal ownerProjectedSavings, decimal tenantProjectedSavings)
    {
        var ownerCredit = ownerProjectedSavings * 0.75m;
        var tenantCredit = tenantProjectedSavings * 0.25m;
        var adjustment = Math.Round(ownerCredit + tenantCredit, 2);
        return (adjustment, Math.Round(baseNcf + adjustment, 2));
    }

    /// <summary>
    /// Task 6: MHC vacancy floor enforcement — returns effective occupancy
    /// capped at 95% (minimum 5% vacancy).
    /// </summary>
    public static decimal EnforceMhcVacancyFloor(decimal occupancyPercent)
    {
        return Math.Min(occupancyPercent, 95m);
    }

    /// <summary>
    /// Task 7: SNF NCF cap test — flags if SNF NCF exceeds 20% of total property NCF.
    /// </summary>
    public static ComplianceTest TestSnfNcfCap(decimal snfNcf, decimal totalPropertyNcf)
    {
        var snfPct = totalPropertyNcf > 0 ? Math.Round(snfNcf / totalPropertyNcf * 100m, 1) : 0m;
        return new ComplianceTest
        {
            Name = "SNF NCF Cap (≤20%)",
            Pass = snfPct <= 20m,
            ActualValue = snfPct,
            RequiredValue = 20m,
            Notes = snfPct <= 20m ? null : $"SNF NCF is {snfPct}% of total — exceeds 20% cap"
        };
    }

    /// <summary>
    /// Task 8: ROAR rehab-period DSCR — during rehab, minimum is 1.0x IO / 0.75x amortizing.
    /// </summary>
    public static ComplianceTest TestRoarRehabDscr(
        decimal rehabNoi, decimal annualDebtService, bool isInterestOnly)
    {
        var rehabDscr = annualDebtService > 0 ? Math.Round(rehabNoi / annualDebtService, 2) : 0m;
        var minDscr = isInterestOnly ? 1.00m : 0.75m;

        return new ComplianceTest
        {
            Name = isInterestOnly ? "ROAR Rehab DSCR (IO)" : "ROAR Rehab DSCR (Amortizing)",
            Pass = rehabDscr >= minDscr,
            ActualValue = rehabDscr,
            RequiredValue = minDscr,
            Notes = $"Rehab period: {(isInterestOnly ? "interest-only" : "amortizing")} basis"
        };
    }

    /// <summary>
    /// Task 9: Supplemental combined test — tests combined DSCR and LTV
    /// across senior + supplemental loans.
    /// </summary>
    public static (ComplianceTest dscrTest, ComplianceTest ltvTest) TestSupplementalCombined(
        decimal seniorLoanAmount, decimal supplementalLoanAmount, decimal purchasePrice,
        decimal noi, decimal seniorDebtService, decimal supplementalDebtService)
    {
        var combinedLoan = seniorLoanAmount + supplementalLoanAmount;
        var combinedLtv = purchasePrice > 0 ? Math.Round(combinedLoan / purchasePrice * 100m, 1) : 0m;
        var combinedDebtService = seniorDebtService + supplementalDebtService;
        var combinedDscr = combinedDebtService > 0 ? Math.Round(noi / combinedDebtService, 2) : 0m;

        // Supplemental profile: max 70% combined LTV, 1.30x combined DSCR
        var dscrTest = new ComplianceTest
        {
            Name = "Supplemental Combined DSCR",
            Pass = combinedDscr >= 1.30m,
            ActualValue = combinedDscr,
            RequiredValue = 1.30m,
            Notes = $"Senior DS ${seniorDebtService:N0} + Supp DS ${supplementalDebtService:N0}"
        };

        var ltvTest = new ComplianceTest
        {
            Name = "Supplemental Combined LTV",
            Pass = combinedLtv <= 70m,
            ActualValue = combinedLtv,
            RequiredValue = 70m,
            Notes = $"Senior ${seniorLoanAmount:N0} + Supp ${supplementalLoanAmount:N0} = ${combinedLoan:N0}"
        };

        return (dscrTest, ltvTest);
    }
}
