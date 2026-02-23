using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class DealService : IDealService
{
    private readonly AppDbContext _db;

    public DealService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> CreateDealAsync(DealInputDto input, string userId)
    {
        var deal = new Deal(input.PropertyName, userId);

        MapFromDto(deal, input);

        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        return deal.Id;
    }

    public async Task UpdateDealAsync(Guid id, DealInputDto input, string userId)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException($"Deal {id} not found.");

        MapFromDto(deal, input);

        await _db.SaveChangesAsync();
    }

    public async Task<DealInputDto?> GetDealAsync(Guid id, string userId)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (deal is null) return null;

        return MapToDto(deal);
    }

    public async Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync(string userId)
    {
        return await _db.Deals
            .Where(d => d.UserId == userId)
            .Include(d => d.CalculationResult)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new DealSummaryDto
            {
                Id = d.Id,
                PropertyName = d.PropertyName,
                Address = d.Address,
                UnitCount = d.UnitCount,
                PurchasePrice = d.PurchasePrice,
                Status = d.Status.ToString(),
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                CapRate = d.CalculationResult != null ? d.CalculationResult.GoingInCapRate : null,
                Irr = d.CalculationResult != null ? d.CalculationResult.InternalRateOfReturn : null
            })
            .ToListAsync();
    }

    public async Task SetStatusAsync(Guid id, string status, string userId)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException($"Deal {id} not found.");

        deal.UpdateStatus(Enum.Parse<DealStatus>(status));

        await _db.SaveChangesAsync();
    }

    public async Task DeleteDealAsync(Guid id, string userId)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException($"Deal {id} not found.");

        _db.Deals.Remove(deal);
        await _db.SaveChangesAsync();
    }

    private static void MapFromDto(Deal deal, DealInputDto input)
    {
        deal.PropertyName = input.PropertyName;
        deal.Address = input.Address;
        deal.UnitCount = input.UnitCount ?? 0;
        deal.PurchasePrice = input.PurchasePrice ?? 0;
        deal.RentRollSummary = input.RentRollSummary;
        deal.T12Summary = input.T12Summary;
        deal.LoanLtv = input.LoanLtv;
        deal.LoanRate = input.LoanRate;
        deal.IsInterestOnly = input.IsInterestOnly;
        deal.AmortizationYears = input.AmortizationYears;
        deal.LoanTermYears = input.LoanTermYears;
        deal.HoldPeriodYears = input.HoldPeriodYears;
        deal.CapexBudget = input.CapexBudget;
        deal.TargetOccupancy = input.TargetOccupancy;
        deal.ValueAddPlans = string.IsNullOrWhiteSpace(input.ValueAddPlans) ? null : input.ValueAddPlans;
    }

    private static DealInputDto MapToDto(Deal deal)
    {
        return new DealInputDto
        {
            PropertyName = deal.PropertyName,
            Address = deal.Address,
            UnitCount = deal.UnitCount,
            PurchasePrice = deal.PurchasePrice,
            RentRollSummary = deal.RentRollSummary,
            T12Summary = deal.T12Summary,
            LoanLtv = deal.LoanLtv,
            LoanRate = deal.LoanRate,
            IsInterestOnly = deal.IsInterestOnly,
            AmortizationYears = deal.AmortizationYears,
            LoanTermYears = deal.LoanTermYears,
            HoldPeriodYears = deal.HoldPeriodYears,
            CapexBudget = deal.CapexBudget,
            TargetOccupancy = deal.TargetOccupancy,
            ValueAddPlans = deal.ValueAddPlans ?? string.Empty
        };
    }
}
