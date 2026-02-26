using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IContractService
{
    Task<ContractTimeline> GetOrCreateTimelineAsync(Guid dealId);
    Task UpdateTimelineAsync(ContractTimeline timeline);
    Task<IReadOnlyList<ClosingCostItem>> GetClosingCostsAsync(Guid dealId);
    Task<Guid> AddClosingCostAsync(ClosingCostItem item);
    Task UpdateClosingCostAsync(ClosingCostItem item);
    Task DeleteClosingCostAsync(Guid itemId);
    Task<decimal> GetTotalEstimatedClosingCostsAsync(Guid dealId);
    Task<decimal> GetTotalActualClosingCostsAsync(Guid dealId);
}
