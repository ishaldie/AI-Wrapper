using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IPortfolioService
{
    Task<Guid> CreateAsync(string name, string userId, string? strategy = null, int? vintageYear = null);
    Task UpdateAsync(Guid id, string name, string userId, string? description = null, string? strategy = null, int? vintageYear = null);
    Task DeleteAsync(Guid id, string userId);
    Task<IReadOnlyList<PortfolioSummaryDto>> GetAllAsync(string userId);
    Task<PortfolioSummaryDto?> GetByIdAsync(Guid id, string userId);
    Task AssignDealAsync(Guid portfolioId, Guid dealId, string userId);
    Task RemoveDealAsync(Guid dealId, string userId);
}
