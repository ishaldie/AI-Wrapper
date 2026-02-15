using System.Text.Json;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Infrastructure.Mapping;

/// <summary>
/// Maps RealAI API response value objects to the flat RealAiData entity for persistence.
/// </summary>
public static class RealAiDataMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a RealAiData entity from the individual API response value objects.
    /// Null inputs result in null fields (graceful "data unavailable").
    /// </summary>
    public static RealAiData Map(
        Guid dealId,
        PropertyData? property,
        TenantMetrics? tenant,
        MarketData? market,
        IReadOnlyList<SalesComp>? salesComps,
        TimeSeriesData? timeSeries)
    {
        var entity = new RealAiData(dealId);

        MapProperty(entity, property);
        MapTenant(entity, tenant);
        MapMarket(entity, market);
        MapSalesComps(entity, salesComps);
        MapTimeSeries(entity, timeSeries);

        return entity;
    }

    private static void MapProperty(RealAiData entity, PropertyData? property)
    {
        if (property is null) return;

        entity.InPlaceRent = property.InPlaceRent;
        entity.Occupancy = property.Occupancy;
        entity.YearBuilt = property.YearBuilt;
        entity.Acreage = property.Acreage;
        entity.SquareFootage = property.SquareFootage;
        entity.Amenities = property.Amenities;
        entity.BuildingType = property.BuildingType;
    }

    private static void MapTenant(RealAiData entity, TenantMetrics? tenant)
    {
        if (tenant?.Subject is null) return;

        entity.AverageFico = tenant.Subject.AverageFico;
        entity.RentToIncomeRatio = tenant.Subject.RentToIncomeRatio;
        entity.MedianHhi = tenant.Subject.MedianHhi;
    }

    private static void MapMarket(RealAiData entity, MarketData? market)
    {
        if (market is null) return;

        entity.MarketCapRate = market.CapRate;
        entity.RentGrowth = market.RentGrowth;
        entity.JobGrowth = market.JobGrowth;
        entity.NetMigration = market.NetMigration;
        entity.Permits = market.Permits;
    }

    private static void MapSalesComps(RealAiData entity, IReadOnlyList<SalesComp>? salesComps)
    {
        if (salesComps is null || salesComps.Count == 0)
        {
            entity.SalesCompsJson = null;
            return;
        }

        entity.SalesCompsJson = JsonSerializer.Serialize(salesComps, JsonOptions);
    }

    private static void MapTimeSeries(RealAiData entity, TimeSeriesData? timeSeries)
    {
        if (timeSeries is null) return;

        entity.RentTrendJson = timeSeries.RentTrend.Count > 0
            ? JsonSerializer.Serialize(timeSeries.RentTrend, JsonOptions)
            : null;

        entity.OccupancyTrendJson = timeSeries.OccupancyTrend.Count > 0
            ? JsonSerializer.Serialize(timeSeries.OccupancyTrend, JsonOptions)
            : null;
    }
}
