using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Application.Calculations;

/// <summary>
/// Evaluates Freddie Mac product-specific compliance tests.
/// Each method is a pure function returning a ComplianceTest result.
/// </summary>
public static class FreddieComplianceCalculator
{
    /// <summary>
    /// Runs all applicable compliance tests for a given Freddie Mac product type.
    /// </summary>
    public static FreddieComplianceResult Evaluate(
        FreddieProductType productType,
        decimal actualDscr,
        decimal actualLtvPercent,
        int actualAmortYears,
        decimal noi,
        decimal annualDebtService,
        decimal loanAmount,
        decimal purchasePrice,
        FreddieComplianceInputs? inputs = null)
    {
        var profile = FreddieProductProfiles.Get(productType);

        // Core tests — always run
        var dscrTest = TestDscr(actualDscr, profile.MinDscr);
        var ltvTest = TestLtv(actualLtvPercent, profile.MaxLtvPercent);
        var amortTest = TestAmortization(actualAmortYears, profile.MaxAmortizationYears);

        // Product-specific tests
        ComplianceTest? sblMarketTier = null;
        ComplianceTest? seniorsBlended = null;
        ComplianceTest? snfNoiCap = null;
        ComplianceTest? mhcRentalHomes = null;
        ComplianceTest? floatingRateCap = null;
        ComplianceTest? valueAddRehab = null;
        ComplianceTest? leaseUpOccupancy = null;
        ComplianceTest? leaseUpLeased = null;
        ComplianceTest? suppCombinedDscr = null;
        ComplianceTest? suppCombinedLtv = null;

        if (inputs != null)
        {
            switch (productType)
            {
                case FreddieProductType.SmallBalanceLoan:
                    if (!string.IsNullOrEmpty(inputs.SblMarketTier))
                        sblMarketTier = TestSblMarketTier(actualDscr, actualLtvPercent, inputs.SblMarketTier);
                    break;

                case FreddieProductType.SeniorsIL:
                case FreddieProductType.SeniorsAL:
                case FreddieProductType.SeniorsSN:
                    seniorsBlended = TestSeniorsBlendedDscr(
                        actualDscr, inputs.IlBeds, inputs.AlBeds, inputs.SnBeds);
                    if (inputs.SnfNoi.HasValue && noi > 0)
                        snfNoiCap = TestSnfNoiCap(inputs.SnfNoi.Value, noi);
                    break;

                case FreddieProductType.ManufacturedHousing:
                    if (inputs.RentalHomesPercent.HasValue)
                        mhcRentalHomes = TestMhcRentalHomesCap(inputs.RentalHomesPercent.Value);
                    break;

                case FreddieProductType.FloatingRate:
                    floatingRateCap = TestFloatingRateCap(actualLtvPercent, inputs.HasRateCap);
                    break;

                case FreddieProductType.ValueAdd:
                    if (inputs.IsRehabPeriod && annualDebtService > 0)
                        valueAddRehab = TestValueAddRehabDscr(noi, annualDebtService, true);
                    break;

                case FreddieProductType.LeaseUp:
                    if (inputs.PhysicalOccupancyPercent.HasValue)
                        leaseUpOccupancy = TestLeaseUpOccupancy(inputs.PhysicalOccupancyPercent.Value);
                    if (inputs.LeasedPercent.HasValue)
                        leaseUpLeased = TestLeaseUpLeased(inputs.LeasedPercent.Value);
                    break;

                case FreddieProductType.Supplemental:
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
        if (sblMarketTier != null) allTests.Add(sblMarketTier);
        if (seniorsBlended != null) allTests.Add(seniorsBlended);
        if (snfNoiCap != null) allTests.Add(snfNoiCap);
        if (mhcRentalHomes != null) allTests.Add(mhcRentalHomes);
        if (floatingRateCap != null) allTests.Add(floatingRateCap);
        if (valueAddRehab != null) allTests.Add(valueAddRehab);
        if (leaseUpOccupancy != null) allTests.Add(leaseUpOccupancy);
        if (leaseUpLeased != null) allTests.Add(leaseUpLeased);
        if (suppCombinedDscr != null) allTests.Add(suppCombinedDscr);
        if (suppCombinedLtv != null) allTests.Add(suppCombinedLtv);

        return new FreddieComplianceResult
        {
            OverallPass = allTests.All(t => t.Pass),
            ProductMinDscr = profile.MinDscr,
            ProductMaxLtvPercent = profile.MaxLtvPercent,
            ProductMaxAmortYears = profile.MaxAmortizationYears,
            DscrTest = dscrTest,
            LtvTest = ltvTest,
            AmortizationTest = amortTest,
            SblMarketTierTest = sblMarketTier,
            SeniorsBlendedDscrTest = seniorsBlended,
            SnfNoiCapTest = snfNoiCap,
            MhcRentalHomesCapTest = mhcRentalHomes,
            FloatingRateCapTest = floatingRateCap,
            ValueAddRehabDscrTest = valueAddRehab,
            LeaseUpOccupancyTest = leaseUpOccupancy,
            LeaseUpLeasedTest = leaseUpLeased,
            SupplementalCombinedDscrTest = suppCombinedDscr,
            SupplementalCombinedLtvTest = suppCombinedLtv
        };
    }

    // === Individual test methods (public for unit testing) ===

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
    /// SBL market tier test — validates DSCR and LTV against tiered thresholds.
    /// Top markets: 80% LTV / 1.20x; Standard: 80% / 1.25x; Small: 75% / 1.25x.
    /// </summary>
    public static ComplianceTest TestSblMarketTier(decimal actualDscr, decimal actualLtv, string marketTier)
    {
        var (maxLtv, minDscr) = marketTier?.ToLowerInvariant() switch
        {
            "top" => (80m, 1.20m),
            "standard" => (80m, 1.25m),
            "small" => (75m, 1.25m),
            _ => (80m, 1.25m)
        };

        var passLtv = actualLtv <= maxLtv;
        var passDscr = actualDscr >= minDscr;
        var pass = passLtv && passDscr;

        return new ComplianceTest
        {
            Name = $"SBL Market Tier ({marketTier})",
            Pass = pass,
            ActualValue = actualDscr,
            RequiredValue = minDscr,
            Notes = $"Tier {marketTier}: max LTV {maxLtv}% (actual {actualLtv}%), min DSCR {minDscr}x"
        };
    }

    /// <summary>
    /// Freddie Mac seniors blended DSCR test using IL=1.30, AL=1.45, SN=1.50.
    /// </summary>
    public static ComplianceTest TestSeniorsBlendedDscr(
        decimal actualDscr, int ilBeds, int alBeds, int snBeds)
    {
        var blendedMin = FreddieProductProfiles.CalculateSeniorsBlendedMinDscr(ilBeds, alBeds, snBeds);
        return new ComplianceTest
        {
            Name = "Seniors Blended DSCR",
            Pass = actualDscr >= blendedMin,
            ActualValue = actualDscr,
            RequiredValue = blendedMin,
            Notes = $"Blended from {ilBeds} IL + {alBeds} AL + {snBeds} SN beds"
        };
    }

    /// <summary>
    /// SNF NOI cap test — flags if SNF NOI exceeds 20% of total property NOI.
    /// </summary>
    public static ComplianceTest TestSnfNoiCap(decimal snfNoi, decimal totalPropertyNoi)
    {
        var snfPct = totalPropertyNoi > 0 ? Math.Round(snfNoi / totalPropertyNoi * 100m, 1) : 0m;
        return new ComplianceTest
        {
            Name = "SNF NOI Cap (≤20%)",
            Pass = snfPct <= 20m,
            ActualValue = snfPct,
            RequiredValue = 20m,
            Notes = snfPct <= 20m ? null : $"SNF NOI is {snfPct}% of total — exceeds 20% cap"
        };
    }

    /// <summary>
    /// MHC rental homes cap — max 25% of homes can be rental (vs pad-only).
    /// </summary>
    public static ComplianceTest TestMhcRentalHomesCap(decimal rentalHomesPercent)
    {
        return new ComplianceTest
        {
            Name = "MHC Rental Homes Cap (≤25%)",
            Pass = rentalHomesPercent <= 25m,
            ActualValue = rentalHomesPercent,
            RequiredValue = 25m,
            Notes = rentalHomesPercent <= 25m ? null : $"Rental homes {rentalHomesPercent}% exceeds 25% cap"
        };
    }

    /// <summary>
    /// Floating rate cap test — rate cap required when LTV exceeds 60%.
    /// </summary>
    public static ComplianceTest TestFloatingRateCap(decimal actualLtv, bool hasRateCap)
    {
        var capRequired = actualLtv > 60m;
        var pass = !capRequired || hasRateCap;

        return new ComplianceTest
        {
            Name = "Floating Rate Cap",
            Pass = pass,
            ActualValue = hasRateCap ? 1m : 0m,
            RequiredValue = capRequired ? 1m : 0m,
            Notes = capRequired
                ? (hasRateCap ? "Rate cap in place (required at LTV > 60%)" : "Rate cap REQUIRED at LTV > 60% — not in place")
                : "Rate cap not required (LTV ≤ 60%)"
        };
    }

    /// <summary>
    /// Value-Add rehab-period DSCR — during rehab, minimum is 1.10x IO / 1.15x amortizing.
    /// </summary>
    public static ComplianceTest TestValueAddRehabDscr(
        decimal rehabNoi, decimal annualDebtService, bool isInterestOnly)
    {
        var rehabDscr = annualDebtService > 0 ? Math.Round(rehabNoi / annualDebtService, 2) : 0m;
        var minDscr = isInterestOnly ? 1.10m : 1.15m;

        return new ComplianceTest
        {
            Name = isInterestOnly ? "Value-Add Rehab DSCR (IO)" : "Value-Add Rehab DSCR (Amortizing)",
            Pass = rehabDscr >= minDscr,
            ActualValue = rehabDscr,
            RequiredValue = minDscr,
            Notes = $"Rehab period: {(isInterestOnly ? "interest-only" : "amortizing")} basis"
        };
    }

    /// <summary>
    /// Lease-Up occupancy test — minimum 65% physical occupancy at closing.
    /// </summary>
    public static ComplianceTest TestLeaseUpOccupancy(decimal physicalOccupancyPercent)
    {
        return new ComplianceTest
        {
            Name = "Lease-Up Physical Occupancy",
            Pass = physicalOccupancyPercent >= 65m,
            ActualValue = physicalOccupancyPercent,
            RequiredValue = 65m,
            Notes = physicalOccupancyPercent >= 65m ? null : $"Physical occupancy {physicalOccupancyPercent}% below 65% minimum"
        };
    }

    /// <summary>
    /// Lease-Up leased test — minimum 75% leased at closing.
    /// </summary>
    public static ComplianceTest TestLeaseUpLeased(decimal leasedPercent)
    {
        return new ComplianceTest
        {
            Name = "Lease-Up Leased Percentage",
            Pass = leasedPercent >= 75m,
            ActualValue = leasedPercent,
            RequiredValue = 75m,
            Notes = leasedPercent >= 75m ? null : $"Leased {leasedPercent}% below 75% minimum"
        };
    }

    /// <summary>
    /// Supplemental combined test — tests combined DSCR (1.25x) and LTV (80%)
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

        var dscrTest = new ComplianceTest
        {
            Name = "Supplemental Combined DSCR",
            Pass = combinedDscr >= 1.25m,
            ActualValue = combinedDscr,
            RequiredValue = 1.25m,
            Notes = $"Senior DS ${seniorDebtService:N0} + Supp DS ${supplementalDebtService:N0}"
        };

        var ltvTest = new ComplianceTest
        {
            Name = "Supplemental Combined LTV",
            Pass = combinedLtv <= 80m,
            ActualValue = combinedLtv,
            RequiredValue = 80m,
            Notes = $"Senior ${seniorLoanAmount:N0} + Supp ${supplementalLoanAmount:N0} = ${combinedLoan:N0}"
        };

        return (dscrTest, ltvTest);
    }

    /// <summary>
    /// MHC vacancy floor enforcement for Freddie MHC — cap occupancy at 95%.
    /// </summary>
    public static decimal EnforceMhcVacancyFloor(decimal occupancyPercent)
    {
        return Math.Min(occupancyPercent, 95m);
    }
}
