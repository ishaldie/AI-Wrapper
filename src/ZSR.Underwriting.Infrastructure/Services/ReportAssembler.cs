using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Formatting;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ReportAssembler : IReportAssembler
{
    private readonly AppDbContext _db;

    public ReportAssembler(AppDbContext db)
    {
        _db = db;
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

        return new UnderwritingReportDto
        {
            DealId = deal.Id,
            PropertyName = deal.PropertyName,
            Address = deal.Address,
            GeneratedAt = DateTime.UtcNow,
            CoreMetrics = BuildCoreMetrics(deal, loanAmount, effectiveLtv, pricePerUnit, noi, egi, opEx, capRate),
            ExecutiveSummary = BuildExecutiveSummary(),
            Assumptions = BuildAssumptions(deal, effectiveLtv, effectiveHold, effectiveOccupancy, effectiveAmort, effectiveTerm),
            PropertyComps = BuildPropertyComps(),
            TenantMarket = BuildTenantMarket(deal, effectiveOccupancy),
            Operations = BuildOperations(deal, gpr, vacancyLoss, netRent, otherIncome, egi, opEx, noi, noiMargin),
            FinancialAnalysis = BuildFinancialAnalysis(deal, loanAmount, equityRequired),
            ValueCreation = BuildValueCreation(deal),
            RiskAssessment = BuildRiskAssessment(),
            InvestmentDecision = BuildInvestmentDecision()
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

    private static ExecutiveSummarySection BuildExecutiveSummary()
    {
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

    private static PropertyCompsSection BuildPropertyComps()
    {
        return new PropertyCompsSection
        {
            Narrative = "[AI-generated comparables analysis pending]",
            Comps = [],
            Adjustments = []
        };
    }

    private static TenantMarketSection BuildTenantMarket(Deal deal, decimal effectiveOccupancy)
    {
        return new TenantMarketSection
        {
            Narrative = "[AI-generated market intelligence pending]",
            SubjectRentPerUnit = deal.RentRollSummary ?? 0m,
            SubjectOccupancy = effectiveOccupancy,
            Benchmarks = []
        };
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

    private static FinancialAnalysisSection BuildFinancialAnalysis(
        Deal deal, decimal loanAmount, decimal equityRequired)
    {
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
            FiveYearCashFlow = [],
            Returns = new ReturnsAnalysis(),
            Exit = new ExitAnalysis()
        };
    }

    private static ValueCreationSection BuildValueCreation(Deal deal)
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
            Narrative = "[AI-generated value creation strategy pending]",
            Strategies = strategies,
            TotalValueAdd = deal.CapexBudget ?? 0m
        };
    }

    private static RiskAssessmentSection BuildRiskAssessment()
    {
        return new RiskAssessmentSection
        {
            Narrative = "[AI-generated risk narrative pending]",
            Risks = []
        };
    }

    private static InvestmentDecisionSection BuildInvestmentDecision()
    {
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
