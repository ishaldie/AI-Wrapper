using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Formatting;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ReportAssembler : IReportAssembler
{
    private readonly AppDbContext _db;
    private readonly IMarketDataService? _marketDataService;
    private readonly ISalesCompExtractor? _salesCompExtractor;
    private readonly IPublicDataService? _publicDataService;
    private readonly IReportProseGenerator? _proseGenerator;
    private readonly IHudApiClient? _hudApiClient;
    private readonly UnderwritingCalculator _calc = new();
    private readonly AffordabilityCalculator _affordabilityCalc = new();

    public ReportAssembler(
        AppDbContext db,
        IMarketDataService? marketDataService = null,
        ISalesCompExtractor? salesCompExtractor = null,
        IPublicDataService? publicDataService = null,
        IReportProseGenerator? proseGenerator = null,
        IHudApiClient? hudApiClient = null)
    {
        _db = db;
        _marketDataService = marketDataService;
        _salesCompExtractor = salesCompExtractor;
        _publicDataService = publicDataService;
        _proseGenerator = proseGenerator;
        _hudApiClient = hudApiClient;
    }

    public async Task<UnderwritingReportDto> AssembleReportAsync(
        Guid dealId, CancellationToken cancellationToken = default)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == dealId, cancellationToken)
            ?? throw new KeyNotFoundException($"Deal {dealId} not found.");

        var effectiveLtv = ProtocolDefaults.GetEffectiveLtv(deal.LoanLtv);
        var effectiveHold = ProtocolDefaults.GetEffectiveHoldPeriod(deal.HoldPeriodYears);
        var effectiveOccupancy = ProtocolDefaults.GetEffectiveOccupancy(deal.TargetOccupancy);
        var effectiveAmort = ProtocolDefaults.GetEffectiveAmortization(deal.AmortizationYears);
        var effectiveTerm = ProtocolDefaults.GetEffectiveLoanTerm(deal.LoanTermYears);

        var loanAmount = deal.PurchasePrice * effectiveLtv / 100m;
        var equityRequired = deal.PurchasePrice - loanAmount;
        var pricePerUnit = deal.UnitCount > 0 ? deal.PurchasePrice / deal.UnitCount : 0m;

        // Revenue calculations using available data
        var gpr = (deal.RentRollSummary ?? 0m) * deal.UnitCount * 12m;
        var vacancyLoss = gpr * (1m - effectiveOccupancy / 100m);
        var netRent = gpr - vacancyLoss;
        var otherIncome = netRent * 0.135m;
        var egi = netRent + otherIncome;
        var opEx = deal.T12Summary ?? (egi * 0.5435m);
        var noi = egi - opEx;
        var noiMargin = egi > 0 ? noi / egi * 100m : 0m;
        var capRate = deal.PurchasePrice > 0 ? noi / deal.PurchasePrice * 100m : 0m;

        // Fetch market data if service available
        MarketContextDto? marketContext = null;
        if (_marketDataService != null)
        {
            var (city, state) = ParseCityState(deal.Address);
            if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(state))
            {
                marketContext = await _marketDataService.GetMarketContextForDealAsync(deal.Id, city, state);
            }
        }

        // Fetch public data (Census, BLS, FRED) if service available
        PublicDataDto? publicData = null;
        if (_publicDataService != null)
        {
            var (city, state) = ParseCityState(deal.Address);
            var zip = ParseZipCode(deal.Address);
            if (!string.IsNullOrEmpty(zip) || !string.IsNullOrEmpty(state))
            {
                publicData = await _publicDataService.GetAllPublicDataAsync(
                    zip, state, city, cancellationToken);
            }
        }

        // Fetch HUD income limits for affordability analysis
        AffordabilityResultDto? affordability = null;
        if (_hudApiClient != null && deal.RentRollSummary.HasValue && deal.RentRollSummary > 0)
        {
            var (city, state) = ParseCityState(deal.Address);
            if (!string.IsNullOrEmpty(state))
            {
                var incomeLimits = await _hudApiClient.GetIncomeLimitsAsync(state, city, cancellationToken);
                if (incomeLimits != null)
                {
                    affordability = _affordabilityCalc.CalculateAffordability(
                        deal.RentRollSummary.Value, incomeLimits);
                }
            }
        }

        // Debt service calculation — prefer user rate, fall back to market rate
        var loanRate = MarketDataEnricher.GetEffectiveLoanRate(deal.LoanRate, marketContext ?? new MarketContextDto()) ?? 0m;
        var debtService = _calc.CalculateAnnualDebtService(loanAmount, loanRate, deal.IsInterestOnly, effectiveAmort);
        var reserves = _calc.CalculateAnnualReserves(deal.UnitCount);
        var acqCosts = _calc.CalculateAcquisitionCosts(deal.PurchasePrice);
        var totalEquity = _calc.CalculateEquityRequired(deal.PurchasePrice, acqCosts, loanAmount);

        // Generate AI prose if generator is available
        GeneratedProse? prose = null;
        if (_proseGenerator != null)
        {
            prose = await GenerateProseAsync(deal, noi, egi, opEx, capRate, debtService,
                loanAmount, totalEquity, effectiveHold, effectiveAmort, loanRate,
                marketContext, publicData, cancellationToken);
        }

        return new UnderwritingReportDto
        {
            DealId = deal.Id,
            PropertyName = deal.PropertyName,
            Address = deal.Address,
            GeneratedAt = DateTime.UtcNow,
            CoreMetrics = BuildCoreMetrics(deal, loanAmount, effectiveLtv, pricePerUnit, noi, egi, opEx, capRate),
            ExecutiveSummary = BuildExecutiveSummary(prose),
            Assumptions = BuildAssumptions(deal, effectiveLtv, effectiveHold, effectiveOccupancy, effectiveAmort, effectiveTerm),
            PropertyComps = await BuildPropertyCompsAsync(deal, marketContext, pricePerUnit, cancellationToken),
            TenantMarket = BuildTenantMarket(deal, effectiveOccupancy, marketContext, publicData?.TenantDemographics, affordability),
            Operations = BuildOperations(deal, gpr, vacancyLoss, netRent, otherIncome, egi, opEx, noi, noiMargin),
            FinancialAnalysis = BuildFinancialAnalysis(deal, loanAmount, equityRequired, noi, egi, opEx,
                debtService, reserves, totalEquity, capRate, loanRate, effectiveHold, effectiveAmort),
            ValueCreation = BuildValueCreation(deal, prose),
            RiskAssessment = BuildRiskAssessment(prose),
            InvestmentDecision = BuildInvestmentDecision(prose),
            PublicData = publicData
        };
    }

    private static CoreMetricsSection BuildCoreMetrics(
        Deal deal, decimal loanAmount, decimal ltv, decimal pricePerUnit,
        decimal noi, decimal egi, decimal opEx, decimal capRate)
    {
        var opExRatio = egi > 0 ? opEx / egi * 100m : 0m;

        return new CoreMetricsSection
        {
            PurchasePrice = deal.PurchasePrice,
            UnitCount = deal.UnitCount,
            PricePerUnit = pricePerUnit,
            CapRate = capRate,
            Noi = noi,
            Egi = egi,
            OpExRatio = opExRatio,
            LoanAmount = loanAmount,
            LtvPercent = ltv,
            Metrics =
            [
                new() { Label = "Purchase Price", Value = ProtocolFormatter.Currency(deal.PurchasePrice), Source = DataSource.UserInput },
                new() { Label = "Unit Count", Value = ProtocolFormatter.Integer(deal.UnitCount), Source = DataSource.UserInput },
                new() { Label = "Price/Unit", Value = ProtocolFormatter.Currency(pricePerUnit), Source = DataSource.Calculated },
                new() { Label = "Cap Rate", Value = ProtocolFormatter.Percent(capRate), Source = DataSource.Calculated },
                new() { Label = "NOI", Value = ProtocolFormatter.Currency(noi), Source = DataSource.Calculated },
                new() { Label = "Loan Amount", Value = ProtocolFormatter.Currency(loanAmount), Source = DataSource.Calculated },
                new() { Label = "LTV", Value = ProtocolFormatter.Percent(ltv), Source = deal.LoanLtv.HasValue ? DataSource.UserInput : DataSource.ProtocolDefault },
            ]
        };
    }

    private static ExecutiveSummarySection BuildExecutiveSummary(GeneratedProse? prose = null)
    {
        if (prose != null)
        {
            var decisionLabel = prose.Decision switch
            {
                InvestmentDecisionType.Go => "GO",
                InvestmentDecisionType.NoGo => "NO GO",
                _ => "CONDITIONAL GO"
            };

            return new ExecutiveSummarySection
            {
                Decision = prose.Decision,
                DecisionLabel = decisionLabel,
                Narrative = prose.ExecutiveSummaryNarrative,
                KeyHighlights = prose.KeyHighlights,
                KeyRisks = prose.KeyRisks
            };
        }

        return new ExecutiveSummarySection
        {
            Decision = InvestmentDecisionType.ConditionalGo,
            DecisionLabel = "CONDITIONAL GO",
            Narrative = "[AI-generated executive summary pending]",
            KeyHighlights = ["Report assembly complete", "Financial metrics calculated"],
            KeyRisks = ["AI narrative not yet generated"]
        };
    }

    private static AssumptionsSection BuildAssumptions(
        Deal deal, decimal ltv, int hold, decimal occupancy, int amort, int term)
    {
        return new AssumptionsSection
        {
            Assumptions =
            [
                new() { Parameter = "Purchase Price", Value = ProtocolFormatter.Currency(deal.PurchasePrice), Source = DataSource.UserInput },
                new() { Parameter = "Loan LTV", Value = ProtocolFormatter.Percent(ltv), Source = deal.LoanLtv.HasValue ? DataSource.UserInput : DataSource.ProtocolDefault },
                new() { Parameter = "Loan Rate", Value = deal.LoanRate.HasValue ? ProtocolFormatter.PercentExact(deal.LoanRate.Value) : "TBD", Source = deal.LoanRate.HasValue ? DataSource.UserInput : DataSource.ProtocolDefault },
                new() { Parameter = "Interest Only", Value = deal.IsInterestOnly ? "Yes" : "No", Source = DataSource.UserInput },
                new() { Parameter = "Amortization", Value = ProtocolFormatter.Years(amort), Source = deal.AmortizationYears.HasValue ? DataSource.UserInput : DataSource.ProtocolDefault },
                new() { Parameter = "Loan Term", Value = ProtocolFormatter.Years(term), Source = deal.LoanTermYears.HasValue ? DataSource.UserInput : DataSource.ProtocolDefault },
                new() { Parameter = "Hold Period", Value = ProtocolFormatter.Years(hold), Source = deal.HoldPeriodYears.HasValue ? DataSource.UserInput : DataSource.ProtocolDefault },
                new() { Parameter = "Target Occupancy", Value = ProtocolFormatter.Percent(occupancy), Source = deal.TargetOccupancy.HasValue ? DataSource.UserInput : DataSource.ProtocolDefault },
            ]
        };
    }

    private async Task<PropertyCompsSection> BuildPropertyCompsAsync(
        Deal deal, MarketContextDto? marketContext, decimal pricePerUnit, CancellationToken cancellationToken)
    {
        // Start with enriched narrative from market data
        var section = marketContext != null && marketContext.ComparableTransactions.Count > 0
            ? MarketDataEnricher.EnrichPropertyComps(marketContext)
            : new PropertyCompsSection
            {
                Narrative = "[AI-generated comparables analysis pending]",
                Comps = [],
                Adjustments = []
            };

        // Extract structured comps via Claude if available
        if (_salesCompExtractor != null && marketContext != null && marketContext.ComparableTransactions.Count > 0)
        {
            var result = await _salesCompExtractor.ExtractCompsAsync(
                marketContext, deal.Address, pricePerUnit, deal.UnitCount, cancellationToken);
            if (result.Comps.Count > 0)
            {
                section = new PropertyCompsSection
                {
                    Narrative = section.Narrative,
                    Comps = result.Comps,
                    Adjustments = result.Adjustments
                };
            }
        }

        return section;
    }

    private static TenantMarketSection BuildTenantMarket(
        Deal deal, decimal effectiveOccupancy, MarketContextDto? marketContext,
        TenantDemographicsDto? demographics = null, AffordabilityResultDto? affordability = null)
    {
        if (marketContext != null || demographics != null)
        {
            var enriched = MarketDataEnricher.EnrichTenantMarket(
                marketContext ?? new MarketContextDto(), deal.RentRollSummary ?? 0m, effectiveOccupancy, demographics);
            if (!enriched.Narrative.Contains("unavailable", StringComparison.OrdinalIgnoreCase))
            {
                // Attach affordability data to the enriched section
                return new TenantMarketSection
                {
                    Narrative = enriched.Narrative,
                    Benchmarks = AppendAffordabilityBenchmarks(enriched.Benchmarks, affordability),
                    MarketRentPerUnit = enriched.MarketRentPerUnit,
                    SubjectRentPerUnit = enriched.SubjectRentPerUnit,
                    MarketOccupancy = enriched.MarketOccupancy,
                    SubjectOccupancy = enriched.SubjectOccupancy,
                    Affordability = affordability
                };
            }
        }

        return new TenantMarketSection
        {
            Narrative = "[AI-generated market intelligence pending]",
            SubjectRentPerUnit = deal.RentRollSummary ?? 0m,
            SubjectOccupancy = effectiveOccupancy,
            Benchmarks = AppendAffordabilityBenchmarks([], affordability),
            Affordability = affordability
        };
    }

    private static List<BenchmarkRow> AppendAffordabilityBenchmarks(
        List<BenchmarkRow> existing, AffordabilityResultDto? affordability)
    {
        if (affordability == null)
            return existing;

        var benchmarks = new List<BenchmarkRow>(existing);
        benchmarks.Add(new BenchmarkRow
        {
            Metric = "HUD Affordability",
            Subject = $"{affordability.AffordableAtAmiPercent}% AMI",
            Market = $"Median Family Income: ${affordability.MedianFamilyIncome:N0}",
            Variance = affordability.AffordabilityTier
        });

        return benchmarks;
    }

    internal static (string city, string state) ParseCityState(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return (string.Empty, string.Empty);

        // Expected format: "123 Main St, Dallas, TX" or "Dallas, TX"
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
            return (parts[^2], parts[^1]);
        if (parts.Length == 2)
            return (parts[0], parts[1]);

        return (string.Empty, string.Empty);
    }

    internal static string ParseZipCode(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        // Look for a 5-digit zip code pattern at the end of the address
        var match = System.Text.RegularExpressions.Regex.Match(address, @"\b(\d{5})(?:-\d{4})?\s*$");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private async Task<GeneratedProse?> GenerateProseAsync(
        Deal deal, decimal noi, decimal egi, decimal opEx, decimal capRate,
        decimal debtService, decimal loanAmount, decimal totalEquity,
        int holdPeriod, int amortYears, decimal loanRate,
        MarketContextDto? marketContext, PublicDataDto? publicData,
        CancellationToken cancellationToken)
    {
        try
        {
            // Build CalculationResult with key metrics for prose context
            var calcResult = new CalculationResult(deal.Id)
            {
                NetOperatingIncome = noi,
                EffectiveGrossIncome = egi,
                OperatingExpenses = opEx,
                GoingInCapRate = capRate,
                AnnualDebtService = debtService,
                LoanAmount = loanAmount,
                DebtServiceCoverageRatio = debtService > 0 ? noi / debtService : null,
                PricePerUnit = deal.UnitCount > 0 ? deal.PurchasePrice / deal.UnitCount : 0,
            };

            // Calculate IRR for decision logic
            var growthRates = Enumerable.Repeat(3m, holdPeriod).ToArray();
            var projectedNoi = _calc.ProjectNoi(noi, growthRates);
            var reserves = _calc.CalculateAnnualReserves(deal.UnitCount);
            var projectedCashFlows = _calc.ProjectCashFlows(projectedNoi, debtService, reserves);
            var exitCapRate = _calc.CalculateExitCapRate(capRate);
            var terminalNoi = projectedNoi.Length > 0 ? projectedNoi[^1] : noi;
            var exitValue = _calc.CalculateExitValue(terminalNoi, exitCapRate);
            var saleCosts = _calc.CalculateSaleCosts(exitValue);
            var loanBalance = _calc.CalculateLoanBalance(loanAmount, loanRate, deal.IsInterestOnly, amortYears, holdPeriod);
            var netProceeds = _calc.CalculateNetSaleProceeds(exitValue, saleCosts, loanBalance);
            calcResult.InternalRateOfReturn = _calc.CalculateIrr(totalEquity, projectedCashFlows, netProceeds);
            calcResult.EquityMultiple = _calc.CalculateEquityMultiple(projectedCashFlows, netProceeds, totalEquity);

            var context = new ProseGenerationContext
            {
                Deal = deal,
                Calculations = calcResult,
                MarketContext = marketContext,
                PublicData = publicData
            };

            return await _proseGenerator!.GenerateAllProseAsync(context, cancellationToken);
        }
        catch
        {
            // Prose generation failure is non-fatal — fall back to placeholders
            return null;
        }
    }

    private static OperationsSection BuildOperations(
        Deal deal, decimal gpr, decimal vacancyLoss, decimal netRent,
        decimal otherIncome, decimal egi, decimal opEx, decimal noi, decimal noiMargin)
    {
        var units = deal.UnitCount;
        return new OperationsSection
        {
            Commentary = deal.T12Summary.HasValue
                ? "Based on trailing 12-month actuals provided."
                : "Estimated using protocol default ratios.",
            RevenueItems =
            [
                new() { LineItem = "Gross Potential Rent", Annual = gpr, PerUnit = units > 0 ? gpr / units : 0, PercentOfEgi = egi > 0 ? gpr / egi * 100m : 0m },
                new() { LineItem = "Vacancy Loss", Annual = -vacancyLoss, PerUnit = units > 0 ? -vacancyLoss / units : 0, PercentOfEgi = egi > 0 ? -vacancyLoss / egi * 100m : 0m },
                new() { LineItem = "Net Rental Income", Annual = netRent, PerUnit = units > 0 ? netRent / units : 0, PercentOfEgi = egi > 0 ? netRent / egi * 100m : 0m },
                new() { LineItem = "Other Income", Annual = otherIncome, PerUnit = units > 0 ? otherIncome / units : 0, PercentOfEgi = egi > 0 ? otherIncome / egi * 100m : 0m },
            ],
            ExpenseItems =
            [
                new() { LineItem = "Operating Expenses", Annual = opEx, PerUnit = units > 0 ? opEx / units : 0, PercentOfEgi = egi > 0 ? opEx / egi * 100m : 0m },
            ],
            TotalRevenue = egi,
            TotalExpenses = opEx,
            Noi = noi,
            NoiMargin = noiMargin
        };
    }

    private FinancialAnalysisSection BuildFinancialAnalysis(
        Deal deal, decimal loanAmount, decimal equityRequired, decimal noi, decimal egi, decimal opEx,
        decimal debtService, decimal reserves, decimal totalEquity, decimal capRate, decimal loanRate,
        int holdPeriod, int amortYears)
    {
        // Default 3% annual NOI growth for projection
        var growthRates = Enumerable.Repeat(3m, holdPeriod).ToArray();
        var projectedNoi = _calc.ProjectNoi(noi, growthRates);
        var projectedCashFlows = _calc.ProjectCashFlows(projectedNoi, debtService, reserves);

        // Build year-by-year cash flow rows
        // EGI and OpEx grow at the same rate as NOI for consistency
        var opExRatio = egi > 0 ? opEx / egi : 0m;
        var fiveYearCf = new List<CashFlowYear>();
        for (int i = 0; i < holdPeriod; i++)
        {
            var yearNoi = projectedNoi[i];
            var yearEgi = opExRatio > 0 ? yearNoi / (1m - opExRatio) : 0m;
            var yearOpEx = yearEgi - yearNoi;
            var yearCoc = totalEquity > 0
                ? Math.Round(projectedCashFlows[i] / totalEquity * 100m, 1)
                : 0m;

            fiveYearCf.Add(new CashFlowYear
            {
                Year = i + 1,
                Egi = Math.Round(yearEgi, 2),
                OpEx = Math.Round(yearOpEx, 2),
                Noi = yearNoi,
                DebtService = debtService,
                CashFlow = projectedCashFlows[i],
                CashOnCash = yearCoc
            });
        }

        // Exit analysis
        var exitCapRate = _calc.CalculateExitCapRate(capRate);
        var terminalNoi = projectedNoi.Length > 0 ? projectedNoi[^1] : noi;
        var exitValue = _calc.CalculateExitValue(terminalNoi, exitCapRate);
        var saleCosts = _calc.CalculateSaleCosts(exitValue);
        var loanBalance = _calc.CalculateLoanBalance(loanAmount, loanRate, deal.IsInterestOnly, amortYears, holdPeriod);
        var netProceeds = _calc.CalculateNetSaleProceeds(exitValue, saleCosts, loanBalance);

        // Returns analysis
        var equityMultiple = _calc.CalculateEquityMultiple(projectedCashFlows, netProceeds, totalEquity);
        var irr = _calc.CalculateIrr(totalEquity, projectedCashFlows, netProceeds);
        var avgCoc = fiveYearCf.Count > 0 ? Math.Round(fiveYearCf.Average(y => y.CashOnCash), 1) : 0m;
        var totalProfit = projectedCashFlows.Sum() + netProceeds - totalEquity;

        return new FinancialAnalysisSection
        {
            SourcesAndUses = new SourcesAndUses
            {
                PurchasePrice = deal.PurchasePrice,
                LoanAmount = loanAmount,
                EquityRequired = equityRequired,
                CapexReserve = deal.CapexBudget ?? 0m,
                TotalUses = deal.PurchasePrice + (deal.CapexBudget ?? 0m),
                TotalSources = loanAmount + equityRequired + (deal.CapexBudget ?? 0m),
            },
            FiveYearCashFlow = fiveYearCf,
            Returns = new ReturnsAnalysis
            {
                Irr = irr,
                EquityMultiple = equityMultiple,
                AverageCashOnCash = avgCoc,
                TotalProfit = Math.Round(totalProfit, 2)
            },
            Exit = new ExitAnalysis
            {
                ExitCapRate = exitCapRate,
                ExitNoi = terminalNoi,
                ExitValue = exitValue,
                LoanBalance = loanBalance,
                NetProceeds = netProceeds
            }
        };
    }

    private static ValueCreationSection BuildValueCreation(Deal deal, GeneratedProse? prose = null)
    {
        var strategies = new List<ValueAddItem>();
        if (!string.IsNullOrWhiteSpace(deal.ValueAddPlans))
        {
            strategies.Add(new ValueAddItem
            {
                Strategy = deal.ValueAddPlans,
                EstimatedCost = deal.CapexBudget ?? 0m
            });
        }

        return new ValueCreationSection
        {
            Narrative = prose?.ValueCreationNarrative ?? "[AI-generated value creation strategy pending]",
            Strategies = strategies,
            TotalValueAdd = deal.CapexBudget ?? 0m
        };
    }

    private static RiskAssessmentSection BuildRiskAssessment(GeneratedProse? prose = null)
    {
        if (prose != null)
        {
            return new RiskAssessmentSection
            {
                Narrative = prose.RiskAssessmentNarrative,
                Risks = prose.Risks
            };
        }

        return new RiskAssessmentSection
        {
            Narrative = "[AI-generated risk narrative pending]",
            Risks = []
        };
    }

    private static InvestmentDecisionSection BuildInvestmentDecision(GeneratedProse? prose = null)
    {
        if (prose != null)
        {
            var decisionLabel = prose.Decision switch
            {
                InvestmentDecisionType.Go => "GO",
                InvestmentDecisionType.NoGo => "NO GO",
                _ => "CONDITIONAL GO"
            };

            return new InvestmentDecisionSection
            {
                Decision = prose.Decision,
                DecisionLabel = decisionLabel,
                InvestmentThesis = prose.InvestmentThesis,
                Conditions = prose.Conditions,
                NextSteps = prose.NextSteps
            };
        }

        return new InvestmentDecisionSection
        {
            Decision = InvestmentDecisionType.ConditionalGo,
            DecisionLabel = "CONDITIONAL GO",
            InvestmentThesis = "[AI-generated investment thesis pending]",
            Conditions = ["Complete due diligence", "Verify rent roll"],
            NextSteps = ["Order appraisal", "Complete environmental assessment"]
        };
    }
}
