using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class PortfolioService : IPortfolioService
{
    private readonly AppDbContext _db;

    public PortfolioService(AppDbContext db) => _db = db;

    public async Task<Guid> CreateAsync(string name, string userId, string? strategy = null, int? vintageYear = null)
    {
        var portfolio = new Portfolio(name, userId)
        {
            Strategy = strategy,
            VintageYear = vintageYear
        };
        _db.Portfolios.Add(portfolio);
        await _db.SaveChangesAsync();
        return portfolio.Id;
    }

    public async Task UpdateAsync(Guid id, string name, string userId, string? description = null, string? strategy = null, int? vintageYear = null)
    {
        var portfolio = await _db.Portfolios.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found.");

        portfolio.Name = name;
        portfolio.Description = description;
        portfolio.Strategy = strategy;
        portfolio.VintageYear = vintageYear;
        portfolio.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, string userId)
    {
        var portfolio = await _db.Portfolios.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found.");

        // Unassign deals first
        var deals = await _db.Deals.Where(d => d.PortfolioId == id).ToListAsync();
        foreach (var deal in deals)
            deal.PortfolioId = null;

        _db.Portfolios.Remove(portfolio);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PortfolioSummaryDto>> GetAllAsync(string userId)
    {
        var portfolios = await _db.Portfolios
            .Where(p => p.UserId == userId)
            .Include(p => p.Deals).ThenInclude(d => d.CalculationResult)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

        return portfolios.Select(p => MapToSummary(p)).ToList();
    }

    public async Task<PortfolioSummaryDto?> GetByIdAsync(Guid id, string userId)
    {
        var portfolio = await _db.Portfolios
            .Where(p => p.Id == id && p.UserId == userId)
            .Include(p => p.Deals).ThenInclude(d => d.CalculationResult)
            .FirstOrDefaultAsync();

        return portfolio is null ? null : MapToSummary(portfolio);
    }

    private static PortfolioSummaryDto MapToSummary(Portfolio p)
    {
        var dealsWithCalc = p.Deals.Where(d => d.CalculationResult != null).ToList();
        return new PortfolioSummaryDto
        {
            Id = p.Id,
            Name = p.Name,
            Strategy = p.Strategy,
            VintageYear = p.VintageYear,
            DealCount = p.Deals.Count,
            ActiveAssetCount = p.Deals.Count(d => d.Status == DealStatus.Active || d.Status == DealStatus.Closed),
            TotalUnits = p.Deals.Sum(d => d.UnitCount),
            TotalAum = p.Deals.Sum(d => d.PurchasePrice),
            WeightedAvgCapRate = dealsWithCalc.Count > 0
                ? dealsWithCalc.Average(d => d.CalculationResult!.GoingInCapRate)
                : 0,
            AggregateNoi = dealsWithCalc.Sum(d => d.CalculationResult!.NetOperatingIncome)
        };
    }

    public async Task AssignDealAsync(Guid portfolioId, Guid dealId, string userId)
    {
        var portfolio = await _db.Portfolios.FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId)
            ?? throw new KeyNotFoundException($"Portfolio {portfolioId} not found.");
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == dealId && d.UserId == userId)
            ?? throw new KeyNotFoundException($"Deal {dealId} not found.");

        deal.PortfolioId = portfolioId;
        await _db.SaveChangesAsync();
    }

    public async Task RemoveDealAsync(Guid dealId, string userId)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == dealId && d.UserId == userId)
            ?? throw new KeyNotFoundException($"Deal {dealId} not found.");

        deal.PortfolioId = null;
        await _db.SaveChangesAsync();
    }
}
