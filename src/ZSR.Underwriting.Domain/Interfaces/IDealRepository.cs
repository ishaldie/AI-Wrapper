using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Interfaces;

public interface IDealRepository
{
    Task<Deal?> GetByIdAsync(Guid id);
    Task<Deal?> GetByIdWithDetailsAsync(Guid id);
    Task<IReadOnlyList<Deal>> GetAllAsync();
    Task<IReadOnlyList<Deal>> GetByStatusAsync(DealStatus status);
    void Add(Deal deal);
    void Remove(Deal deal);
}
