using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Interfaces;

public interface IDealRepository
{
    Task<Deal?> GetByIdAsync(Guid id, string userId);
    Task<Deal?> GetByIdWithDetailsAsync(Guid id, string userId);
    Task<IReadOnlyList<Deal>> GetAllAsync(string userId);
    Task<IReadOnlyList<Deal>> GetByStatusAsync(DealStatus status, string userId);
    void Add(Deal deal);
    void Remove(Deal deal);
}
