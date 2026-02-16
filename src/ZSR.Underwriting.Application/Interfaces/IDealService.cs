using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IDealService
{
    Task<Guid> CreateDealAsync(DealInputDto input);
    Task UpdateDealAsync(Guid id, DealInputDto input);
    Task<DealInputDto?> GetDealAsync(Guid id);
    Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync();
    Task SetStatusAsync(Guid id, string status);
    Task DeleteDealAsync(Guid id);
}
