using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class RentRollService : IRentRollService
{
    private readonly AppDbContext _db;

    public RentRollService(AppDbContext db) => _db = db;

    public async Task<Guid> AddUnitAsync(RentRollUnit unit)
    {
        _db.RentRollUnits.Add(unit);
        await _db.SaveChangesAsync();
        return unit.Id;
    }

    public async Task UpdateUnitAsync(RentRollUnit unit)
    {
        unit.UpdatedAt = DateTime.UtcNow;
        _db.RentRollUnits.Update(unit);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteUnitAsync(Guid unitId)
    {
        var unit = await _db.RentRollUnits.FindAsync(unitId);
        if (unit is not null)
        {
            _db.RentRollUnits.Remove(unit);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<RentRollUnit>> GetUnitsForDealAsync(Guid dealId)
    {
        return await _db.RentRollUnits
            .Where(u => u.DealId == dealId)
            .OrderBy(u => u.UnitNumber)
            .ToListAsync();
    }

    public async Task<RentRollSummaryDto> GetSummaryAsync(Guid dealId)
    {
        var units = await _db.RentRollUnits.Where(u => u.DealId == dealId).ToListAsync();

        if (units.Count == 0)
            return new RentRollSummaryDto();

        var occupied = units.Count(u => u.Status == UnitStatus.Occupied);
        var total = units.Count;

        return new RentRollSummaryDto
        {
            TotalUnits = total,
            OccupiedUnits = occupied,
            VacantUnits = total - occupied,
            OccupancyPercent = total > 0 ? (decimal)occupied / total * 100 : 0,
            AverageMarketRent = units.Average(u => u.MarketRent),
            AverageActualRent = units.Where(u => u.ActualRent.HasValue).Select(u => u.ActualRent!.Value).DefaultIfEmpty(0).Average(),
            TotalGrossPotentialRent = units.Sum(u => u.MarketRent),
            TotalActualRent = units.Where(u => u.ActualRent.HasValue).Sum(u => u.ActualRent!.Value)
        };
    }

    public async Task BulkAddUnitsAsync(IEnumerable<RentRollUnit> units)
    {
        _db.RentRollUnits.AddRange(units);
        await _db.SaveChangesAsync();
    }
}
