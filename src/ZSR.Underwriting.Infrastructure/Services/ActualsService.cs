using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ActualsService : IActualsService
{
    private readonly AppDbContext _db;

    public ActualsService(AppDbContext db) => _db = db;

    public async Task<MonthlyActual?> GetAsync(Guid dealId, int year, int month)
    {
        return await _db.MonthlyActuals
            .FirstOrDefaultAsync(a => a.DealId == dealId && a.Year == year && a.Month == month);
    }

    public async Task<Guid> SaveAsync(MonthlyActual actual)
    {
        var existing = await _db.MonthlyActuals
            .FirstOrDefaultAsync(a => a.DealId == actual.DealId && a.Year == actual.Year && a.Month == actual.Month);

        if (existing is not null)
        {
            // Update existing entry
            existing.GrossRentalIncome = actual.GrossRentalIncome;
            existing.VacancyLoss = actual.VacancyLoss;
            existing.OtherIncome = actual.OtherIncome;
            existing.PropertyTaxes = actual.PropertyTaxes;
            existing.Insurance = actual.Insurance;
            existing.Utilities = actual.Utilities;
            existing.Repairs = actual.Repairs;
            existing.Management = actual.Management;
            existing.Payroll = actual.Payroll;
            existing.Marketing = actual.Marketing;
            existing.Administrative = actual.Administrative;
            existing.OtherExpenses = actual.OtherExpenses;
            existing.DebtService = actual.DebtService;
            existing.CapitalExpenditures = actual.CapitalExpenditures;
            existing.OccupiedUnits = actual.OccupiedUnits;
            existing.TotalUnits = actual.TotalUnits;
            existing.Notes = actual.Notes;
            existing.Recalculate();
            existing.EnteredAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return existing.Id;
        }

        actual.Recalculate();
        _db.MonthlyActuals.Add(actual);
        await _db.SaveChangesAsync();
        return actual.Id;
    }

    public async Task DeleteAsync(Guid id)
    {
        var actual = await _db.MonthlyActuals.FindAsync(id);
        if (actual is not null)
        {
            _db.MonthlyActuals.Remove(actual);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IReadOnlyList<MonthlyActual>> GetForYearAsync(Guid dealId, int year)
    {
        return await _db.MonthlyActuals
            .Where(a => a.DealId == dealId && a.Year == year)
            .OrderBy(a => a.Month)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MonthlyActual>> GetTrailingTwelveAsync(Guid dealId)
    {
        var now = DateTime.UtcNow;
        var cutoff = new DateTime(now.Year, now.Month, 1).AddMonths(-12);

        return await _db.MonthlyActuals
            .Where(a => a.DealId == dealId)
            .Where(a => new DateTime(a.Year, a.Month, 1) >= cutoff)
            .OrderBy(a => a.Year).ThenBy(a => a.Month)
            .ToListAsync();
    }

    public async Task<AnnualSummaryDto> GetAnnualSummaryAsync(Guid dealId, int year)
    {
        var actuals = await GetForYearAsync(dealId, year);

        if (actuals.Count == 0)
            return new AnnualSummaryDto { Year = year };

        return new AnnualSummaryDto
        {
            Year = year,
            TotalRevenue = actuals.Sum(a => a.EffectiveGrossIncome),
            TotalExpenses = actuals.Sum(a => a.TotalOperatingExpenses),
            TotalNoi = actuals.Sum(a => a.NetOperatingIncome),
            TotalCashFlow = actuals.Sum(a => a.CashFlow),
            AverageOccupancy = actuals.Average(a => a.OccupancyPercent),
            MonthsReported = actuals.Count
        };
    }
}
