using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class DealService : IDealService
{
    private readonly AppDbContext _db;
    private readonly IGeocodingService? _geocodingService;
    private readonly ILogger<DealService> _logger;

    public DealService(AppDbContext db, ILogger<DealService> logger, IGeocodingService? geocodingService = null)
    {
        _db = db;
        _logger = logger;
        _geocodingService = geocodingService;
    }

    public async Task<Guid> CreateDealAsync(DealInputDto input, string userId)
    {
        var deal = new Deal(input.PropertyName, userId);

        MapFromDto(deal, input);
        await TryGeocodeAsync(deal);

        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        return deal.Id;
    }

    public async Task UpdateDealAsync(Guid id, DealInputDto input, string userId)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException($"Deal {id} not found.");

        var previousAddress = deal.Address;
        MapFromDto(deal, input);

        if (!string.Equals(previousAddress, deal.Address, StringComparison.OrdinalIgnoreCase))
        {
            await TryGeocodeAsync(deal);
        }

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
                Phase = d.Phase.ToString(),
                PortfolioId = d.PortfolioId,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                CapRate = d.CalculationResult != null ? d.CalculationResult.GoingInCapRate : null,
                Irr = d.CalculationResult != null ? d.CalculationResult.InternalRateOfReturn : null
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<DealMapPinDto>> GetDealsForMapAsync(string userId)
    {
        return await _db.Deals
            .Where(d => d.UserId == userId && d.Latitude != null && d.Longitude != null)
            .Include(d => d.CalculationResult)
            .Select(d => new DealMapPinDto
            {
                Id = d.Id,
                PropertyName = d.PropertyName,
                Address = d.Address,
                Status = d.Status.ToString(),
                Phase = d.Phase.ToString(),
                Latitude = d.Latitude!.Value,
                Longitude = d.Longitude!.Value,
                UnitCount = d.UnitCount,
                PurchasePrice = d.PurchasePrice,
                CapRate = d.CalculationResult != null ? d.CalculationResult.GoingInCapRate : null,
                Irr = d.CalculationResult != null ? d.CalculationResult.InternalRateOfReturn : null
            })
            .ToListAsync();
    }

    public async Task SetStatusAsync(Guid id, string status, string userId)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException($"Deal {id} not found.");

        var newStatus = Enum.Parse<DealStatus>(status);

        if (!IsValidTransition(deal.Status, newStatus))
            throw new InvalidOperationException($"Cannot transition from {deal.Status} to {newStatus}.");

        deal.UpdateStatus(newStatus);

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Defines legal status transitions. Archived can be reached from any status.
    /// </summary>
    public static bool IsValidTransition(DealStatus from, DealStatus to)
    {
        // Archived is always a valid target (passing on a deal)
        if (to == DealStatus.Archived) return true;

        return (from, to) switch
        {
            // Acquisition phase transitions
            (DealStatus.Draft, DealStatus.Screening) => true,
            (DealStatus.Draft, DealStatus.InProgress) => true,  // backward compat alias
            (DealStatus.InProgress, DealStatus.Screening) => true,
            (DealStatus.InProgress, DealStatus.Complete) => true,
            (DealStatus.Screening, DealStatus.Complete) => true,

            // Contract phase transitions
            (DealStatus.Complete, DealStatus.UnderContract) => true,

            // Closing transitions
            (DealStatus.UnderContract, DealStatus.Closed) => true,

            // Ownership phase transitions
            (DealStatus.Closed, DealStatus.Active) => true,

            // Exit phase transitions
            (DealStatus.Active, DealStatus.Disposition) => true,
            (DealStatus.Disposition, DealStatus.Sold) => true,

            // Re-activate from archived
            (DealStatus.Archived, DealStatus.Draft) => true,

            _ => false
        };
    }

    public async Task DeleteDealAsync(Guid id, string userId)
    {
        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId)
            ?? throw new KeyNotFoundException($"Deal {id} not found.");

        _db.Deals.Remove(deal);
        await _db.SaveChangesAsync();
    }

    private async Task TryGeocodeAsync(Deal deal)
    {
        if (_geocodingService is null || string.IsNullOrWhiteSpace(deal.Address))
            return;

        try
        {
            var result = await _geocodingService.GeocodeAsync(deal.Address);
            if (result is not null)
            {
                deal.Latitude = result.Latitude;
                deal.Longitude = result.Longitude;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geocoding failed for deal {DealName}, continuing without coordinates", deal.Name);
        }
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
