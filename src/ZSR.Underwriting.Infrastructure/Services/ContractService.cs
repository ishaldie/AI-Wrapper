using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ContractService : IContractService
{
    private readonly AppDbContext _db;

    public ContractService(AppDbContext db) => _db = db;

    public async Task<ContractTimeline> GetOrCreateTimelineAsync(Guid dealId)
    {
        var timeline = await _db.ContractTimelines.FirstOrDefaultAsync(t => t.DealId == dealId);
        if (timeline is not null) return timeline;

        timeline = new ContractTimeline(dealId);
        _db.ContractTimelines.Add(timeline);
        await _db.SaveChangesAsync();
        return timeline;
    }

    public async Task UpdateTimelineAsync(ContractTimeline timeline)
    {
        _db.ContractTimelines.Update(timeline);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ClosingCostItem>> GetClosingCostsAsync(Guid dealId)
    {
        return await _db.ClosingCostItems
            .Where(c => c.DealId == dealId)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Description)
            .ToListAsync();
    }

    public async Task<Guid> AddClosingCostAsync(ClosingCostItem item)
    {
        _db.ClosingCostItems.Add(item);
        await _db.SaveChangesAsync();
        return item.Id;
    }

    public async Task UpdateClosingCostAsync(ClosingCostItem item)
    {
        _db.ClosingCostItems.Update(item);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteClosingCostAsync(Guid itemId)
    {
        var item = await _db.ClosingCostItems.FindAsync(itemId);
        if (item is not null)
        {
            _db.ClosingCostItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<decimal> GetTotalEstimatedClosingCostsAsync(Guid dealId)
    {
        return await _db.ClosingCostItems
            .Where(c => c.DealId == dealId)
            .SumAsync(c => c.EstimatedAmount);
    }

    public async Task<decimal> GetTotalActualClosingCostsAsync(Guid dealId)
    {
        return await _db.ClosingCostItems
            .Where(c => c.DealId == dealId && c.ActualAmount != null)
            .SumAsync(c => c.ActualAmount!.Value);
    }
}
