using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IDispositionService
{
    Task<DispositionAnalysis?> GetAnalysisAsync(Guid dealId);
    Task<DispositionAnalysis> CreateOrUpdateAsync(Guid dealId, decimal? bov = null, decimal? marketCapRate = null);
    Task<SellScenario> CalculateSellScenarioAsync(Guid dealId, decimal salePrice, decimal sellingCostPercent = 3m);
    Task<HoldScenario> CalculateHoldScenarioAsync(Guid dealId, int additionalYears = 5, decimal noiGrowthRate = 2m);
    Task<RefinanceScenario> CalculateRefinanceScenarioAsync(Guid dealId, decimal newLoanAmount, decimal newRate);
    Task DeleteAnalysisAsync(Guid dealId);
}
