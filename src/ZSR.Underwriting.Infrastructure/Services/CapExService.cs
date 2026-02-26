using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class CapExService : ICapExService
{
    private readonly AppDbContext _db;

    public CapExService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CapExProject>> GetProjectsAsync(Guid dealId)
    {
        return await _db.CapExProjects
            .Where(p => p.DealId == dealId)
            .Include(p => p.LineItems)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<CapExProject?> GetProjectAsync(Guid projectId)
    {
        return await _db.CapExProjects
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<Guid> AddProjectAsync(CapExProject project)
    {
        _db.CapExProjects.Add(project);
        await _db.SaveChangesAsync();
        return project.Id;
    }

    public async Task UpdateProjectAsync(CapExProject project)
    {
        _db.CapExProjects.Update(project);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteProjectAsync(Guid projectId)
    {
        var project = await _db.CapExProjects
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is not null)
        {
            _db.CapExLineItems.RemoveRange(project.LineItems);
            _db.CapExProjects.Remove(project);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<Guid> AddLineItemAsync(CapExLineItem lineItem)
    {
        _db.CapExLineItems.Add(lineItem);
        await _db.SaveChangesAsync();

        // Recalculate project spend
        var project = await _db.CapExProjects
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == lineItem.CapExProjectId);
        if (project is not null)
        {
            project.RecalculateSpend();
            await _db.SaveChangesAsync();
        }

        return lineItem.Id;
    }

    public async Task DeleteLineItemAsync(Guid lineItemId)
    {
        var item = await _db.CapExLineItems.FindAsync(lineItemId);
        if (item is not null)
        {
            var projectId = item.CapExProjectId;
            _db.CapExLineItems.Remove(item);
            await _db.SaveChangesAsync();

            var project = await _db.CapExProjects
                .Include(p => p.LineItems)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project is not null)
            {
                project.RecalculateSpend();
                await _db.SaveChangesAsync();
            }
        }
    }

    public async Task<decimal> GetTotalBudgetAsync(Guid dealId)
    {
        return await _db.CapExProjects
            .Where(p => p.DealId == dealId)
            .SumAsync(p => p.BudgetAmount);
    }

    public async Task<decimal> GetTotalSpendAsync(Guid dealId)
    {
        return await _db.CapExProjects
            .Where(p => p.DealId == dealId)
            .SumAsync(p => p.ActualSpend);
    }
}
