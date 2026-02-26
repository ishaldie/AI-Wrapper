using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IRentRollService
{
    Task<Guid> AddUnitAsync(RentRollUnit unit);
    Task UpdateUnitAsync(RentRollUnit unit);
    Task DeleteUnitAsync(Guid unitId);
    Task<IReadOnlyList<RentRollUnit>> GetUnitsForDealAsync(Guid dealId);
    Task<RentRollSummaryDto> GetSummaryAsync(Guid dealId);
    Task BulkAddUnitsAsync(IEnumerable<RentRollUnit> units);
}
