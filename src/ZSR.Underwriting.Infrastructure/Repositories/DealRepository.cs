using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Repositories;

public class DealRepository : IDealRepository
{
    private readonly AppDbContext _ctx;

    public DealRepository(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<Deal?> GetByIdAsync(Guid id)
    {
        return await _ctx.Deals.FindAsync(id);
    }

    public async Task<Deal?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _ctx.Deals
            .Include(d => d.Property)
            .Include(d => d.UnderwritingInput)
            .Include(d => d.CalculationResult)
            .Include(d => d.Report)
            .Include(d => d.RealAiData)
            .Include(d => d.UploadedDocuments)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IReadOnlyList<Deal>> GetAllAsync()
    {
        return await _ctx.Deals
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Deal>> GetByStatusAsync(DealStatus status)
    {
        return await _ctx.Deals
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public void Add(Deal deal)
    {
        _ctx.Deals.Add(deal);
    }

    public void Remove(Deal deal)
    {
        _ctx.Deals.Remove(deal);
    }
}
