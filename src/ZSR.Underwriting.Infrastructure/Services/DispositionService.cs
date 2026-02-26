using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class DispositionService : IDispositionService
{
    private readonly AppDbContext _db;
    private readonly IActualsService _actualsService;

    public DispositionService(AppDbContext db, IActualsService actualsService)
    {
        _db = db;
        _actualsService = actualsService;
    }

    public async Task<DispositionAnalysis?> GetAnalysisAsync(Guid dealId)
    {
        return await _db.DispositionAnalyses
            .FirstOrDefaultAsync(d => d.DealId == dealId);
    }

    public async Task<DispositionAnalysis> CreateOrUpdateAsync(Guid dealId, decimal? bov = null, decimal? marketCapRate = null)
    {
        var existing = await _db.DispositionAnalyses.FirstOrDefaultAsync(d => d.DealId == dealId);
        var actuals = await _actualsService.GetTrailingTwelveAsync(dealId);
        var t12Noi = actuals.Sum(a => a.NetOperatingIncome);

        // Annualize if fewer than 12 months
        if (actuals.Count > 0 && actuals.Count < 12)
            t12Noi = t12Noi * 12m / actuals.Count;

        if (existing is not null)
        {
            existing.BrokerOpinionOfValue = bov ?? existing.BrokerOpinionOfValue;
            existing.CurrentMarketCapRate = marketCapRate ?? existing.CurrentMarketCapRate;
            existing.TrailingTwelveMonthNoi = t12Noi;
            existing.RecalculateImpliedValue();
            existing.AnalyzedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return existing;
        }

        var analysis = new DispositionAnalysis(dealId)
        {
            BrokerOpinionOfValue = bov,
            CurrentMarketCapRate = marketCapRate,
            TrailingTwelveMonthNoi = t12Noi
        };
        analysis.RecalculateImpliedValue();

        _db.DispositionAnalyses.Add(analysis);
        await _db.SaveChangesAsync();
        return analysis;
    }

    public async Task<SellScenario> CalculateSellScenarioAsync(Guid dealId, decimal salePrice, decimal sellingCostPercent = 3m)
    {
        var deal = await _db.Deals
            .Include(d => d.CalculationResult)
            .FirstOrDefaultAsync(d => d.Id == dealId);

        if (deal is null) throw new InvalidOperationException("Deal not found");

        var loanBalance = deal.CalculationResult?.LoanAmount ?? 0; // Simplified — actual would amortize
        var equity = deal.ActualPurchasePrice ?? deal.PurchasePrice;
        var sellingCosts = salePrice * (sellingCostPercent / 100m);
        var netProceeds = salePrice - sellingCosts - loanBalance;
        var totalInvested = equity - loanBalance; // Equity invested at acquisition

        var holdMonths = deal.ClosedDate.HasValue
            ? (int)((DateTime.UtcNow - deal.ClosedDate.Value).TotalDays / 30.44)
            : 12;

        // Gather cumulative cash flow from actuals
        var actuals = await _actualsService.GetTrailingTwelveAsync(dealId);
        var cumulativeCashFlow = actuals.Sum(a => a.CashFlow);
        if (actuals.Count > 0 && actuals.Count < 12)
            cumulativeCashFlow = cumulativeCashFlow * holdMonths / (actuals.Count * (holdMonths / 12m));

        var totalProfit = netProceeds - totalInvested + cumulativeCashFlow;
        var equityMultiple = totalInvested > 0 ? (netProceeds + cumulativeCashFlow) / totalInvested : 0;

        return new SellScenario
        {
            EstimatedSalePrice = salePrice,
            SellingCosts = sellingCosts,
            NetProceeds = netProceeds,
            RemainingLoanBalance = loanBalance,
            EquityReturned = netProceeds,
            TotalProfit = totalProfit,
            EquityMultiple = equityMultiple,
            HoldPeriodMonths = holdMonths,
            RealizedIrr = 0 // Simplified — full IRR requires cash flow series
        };
    }

    public async Task<HoldScenario> CalculateHoldScenarioAsync(Guid dealId, int additionalYears = 5, decimal noiGrowthRate = 2m)
    {
        var deal = await _db.Deals
            .Include(d => d.CalculationResult)
            .FirstOrDefaultAsync(d => d.Id == dealId);

        if (deal is null) throw new InvalidOperationException("Deal not found");

        var actuals = await _actualsService.GetTrailingTwelveAsync(dealId);
        var currentNoi = actuals.Count > 0
            ? actuals.Sum(a => a.NetOperatingIncome) * (actuals.Count < 12 ? 12m / actuals.Count : 1m)
            : deal.CalculationResult?.NetOperatingIncome ?? 0;

        var growthMultiplier = (decimal)Math.Pow((double)(1 + noiGrowthRate / 100m), additionalYears);
        var futureNoi = currentNoi * growthMultiplier;

        var exitCapRate = deal.CalculationResult?.GoingInCapRate ?? 6m;
        var exitValue = exitCapRate > 0 ? futureNoi / (exitCapRate / 100m) : 0;

        var equity = (deal.ActualPurchasePrice ?? deal.PurchasePrice) - (deal.CalculationResult?.LoanAmount ?? 0);
        var annualCashFlow = currentNoi - (deal.CalculationResult?.AnnualDebtService ?? 0);
        var projectedCoC = equity > 0 ? annualCashFlow / equity * 100 : 0;

        return new HoldScenario
        {
            AdditionalYears = additionalYears,
            ProjectedExitValue = exitValue,
            ProjectedAnnualNoi = futureNoi,
            ProjectedCashOnCash = projectedCoC,
            ProjectedIrr = 0, // Simplified
            ProjectedEquityMultiple = equity > 0 ? (exitValue + annualCashFlow * additionalYears) / equity : 0
        };
    }

    public async Task<RefinanceScenario> CalculateRefinanceScenarioAsync(Guid dealId, decimal newLoanAmount, decimal newRate)
    {
        var deal = await _db.Deals
            .Include(d => d.CalculationResult)
            .FirstOrDefaultAsync(d => d.Id == dealId);

        if (deal is null) throw new InvalidOperationException("Deal not found");

        var currentBalance = deal.CalculationResult?.LoanAmount ?? 0;
        var cashOut = newLoanAmount - currentBalance;
        var newAnnualDebtService = newLoanAmount * newRate / 100m; // Interest-only approximation

        var actuals = await _actualsService.GetTrailingTwelveAsync(dealId);
        var currentNoi = actuals.Count > 0
            ? actuals.Sum(a => a.NetOperatingIncome) * (actuals.Count < 12 ? 12m / actuals.Count : 1m)
            : deal.CalculationResult?.NetOperatingIncome ?? 0;

        var propertyValue = deal.ActualPurchasePrice ?? deal.PurchasePrice;
        var remainingEquity = propertyValue - newLoanAmount;
        var goForwardCashFlow = currentNoi - newAnnualDebtService;
        var goForwardCoC = remainingEquity > 0 ? goForwardCashFlow / remainingEquity * 100 : 0;

        return new RefinanceScenario
        {
            NewLoanAmount = newLoanAmount,
            CurrentLoanBalance = currentBalance,
            CashOutAmount = cashOut > 0 ? cashOut : 0,
            NewInterestRate = newRate,
            NewAnnualDebtService = newAnnualDebtService,
            GoForwardCashOnCash = goForwardCoC,
            RemainingEquity = remainingEquity
        };
    }

    public async Task DeleteAnalysisAsync(Guid dealId)
    {
        var analysis = await _db.DispositionAnalyses.FirstOrDefaultAsync(d => d.DealId == dealId);
        if (analysis is not null)
        {
            _db.DispositionAnalyses.Remove(analysis);
            await _db.SaveChangesAsync();
        }
    }
}
