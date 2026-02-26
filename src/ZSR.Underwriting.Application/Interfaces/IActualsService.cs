using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IActualsService
{
    Task<MonthlyActual?> GetAsync(Guid dealId, int year, int month);
    Task<Guid> SaveAsync(MonthlyActual actual);
    Task DeleteAsync(Guid id);
    Task<IReadOnlyList<MonthlyActual>> GetForYearAsync(Guid dealId, int year);
    Task<IReadOnlyList<MonthlyActual>> GetTrailingTwelveAsync(Guid dealId);
    Task<AnnualSummaryDto> GetAnnualSummaryAsync(Guid dealId, int year);
}
