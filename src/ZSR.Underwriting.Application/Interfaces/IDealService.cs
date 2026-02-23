using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IDealService
{
    Task<Guid> CreateDealAsync(DealInputDto input, string userId);
    Task UpdateDealAsync(Guid id, DealInputDto input, string userId);
    Task<DealInputDto?> GetDealAsync(Guid id, string userId);
    Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync(string userId);
    Task SetStatusAsync(Guid id, string status, string userId);
    Task DeleteDealAsync(Guid id, string userId);
}
