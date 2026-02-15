using System.Text.Json;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.ValueObjects;
using ZSR.Underwriting.Infrastructure.Mapping;

namespace ZSR.Underwriting.Tests.RealAi;

public class RealAiDataMapperTests
{
    private readonly Guid _dealId = Guid.NewGuid();

    // --- Property Data Mapping ---

    [Fact]
    public void MapPropertyData_PopulatesAllFields()
    {
        var property = new PropertyData
        {
            InPlaceRent = 1200m,
            Occupancy = 0.95m,
            YearBuilt = 1990,
            Acreage = 2.5m,
            SquareFootage = 50000,
            Amenities = "Pool,Gym",
            BuildingType = "Garden"
        };

        var result = RealAiDataMapper.Map(_dealId, property, null, null, null, null);

        Assert.Equal(1200m, result.InPlaceRent);
        Assert.Equal(0.95m, result.Occupancy);
        Assert.Equal(1990, result.YearBuilt);
        Assert.Equal(2.5m, result.Acreage);
        Assert.Equal(50000, result.SquareFootage);
        Assert.Equal("Pool,Gym", result.Amenities);
        Assert.Equal("Garden", result.BuildingType);
    }

    [Fact]
    public void MapPropertyData_Null_LeavesFieldsNull()
    {
        var result = RealAiDataMapper.Map(_dealId, null, null, null, null, null);

        Assert.Null(result.InPlaceRent);
        Assert.Null(result.Occupancy);
        Assert.Null(result.YearBuilt);
        Assert.Null(result.BuildingType);
    }

    // --- Tenant Metrics Mapping ---

    [Fact]
    public void MapTenantMetrics_UsesSubjectLevel()
    {
        var tenant = new TenantMetrics
        {
            Subject = new MetricLevel
            {
                AverageFico = 720,
                RentToIncomeRatio = 0.28m,
                MedianHhi = 55000m
            },
            Zipcode = new MetricLevel { AverageFico = 710 },
            Metro = new MetricLevel { AverageFico = 700 }
        };

        var result = RealAiDataMapper.Map(_dealId, null, tenant, null, null, null);

        Assert.Equal(720, result.AverageFico);
        Assert.Equal(0.28m, result.RentToIncomeRatio);
        Assert.Equal(55000m, result.MedianHhi);
    }

    [Fact]
    public void MapTenantMetrics_Null_LeavesFieldsNull()
    {
        var result = RealAiDataMapper.Map(_dealId, null, null, null, null, null);

        Assert.Null(result.AverageFico);
        Assert.Null(result.RentToIncomeRatio);
        Assert.Null(result.MedianHhi);
    }

    // --- Market Data Mapping ---

    [Fact]
    public void MapMarketData_PopulatesAllFields()
    {
        var market = new MarketData
        {
            CapRate = 0.065m,
            RentGrowth = 0.035m,
            JobGrowth = 0.025m,
            NetMigration = 15000,
            Permits = 8500
        };

        var result = RealAiDataMapper.Map(_dealId, null, null, market, null, null);

        Assert.Equal(0.065m, result.MarketCapRate);
        Assert.Equal(0.035m, result.RentGrowth);
        Assert.Equal(0.025m, result.JobGrowth);
        Assert.Equal(15000, result.NetMigration);
        Assert.Equal(8500, result.Permits);
    }

    [Fact]
    public void MapMarketData_Null_LeavesFieldsNull()
    {
        var result = RealAiDataMapper.Map(_dealId, null, null, null, null, null);

        Assert.Null(result.MarketCapRate);
        Assert.Null(result.RentGrowth);
    }

    // --- Sales Comps Mapping ---

    [Fact]
    public void MapSalesComps_SerializesToJson()
    {
        var comps = new List<SalesComp>
        {
            new() { Address = "100 Oak Ave", PricePerUnit = 95000m, Units = 48 },
            new() { Address = "200 Elm St", PricePerUnit = 88000m, Units = 36 }
        };

        var result = RealAiDataMapper.Map(_dealId, null, null, null, comps, null);

        Assert.NotNull(result.SalesCompsJson);
        var deserialized = JsonSerializer.Deserialize<List<SalesComp>>(
            result.SalesCompsJson!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(2, deserialized!.Count);
        Assert.Equal("100 Oak Ave", deserialized[0].Address);
        Assert.Equal(95000m, deserialized[0].PricePerUnit);
    }

    [Fact]
    public void MapSalesComps_Empty_SetsNullJson()
    {
        var result = RealAiDataMapper.Map(_dealId, null, null, null, Array.Empty<SalesComp>(), null);

        Assert.Null(result.SalesCompsJson);
    }

    [Fact]
    public void MapSalesComps_Null_SetsNullJson()
    {
        var result = RealAiDataMapper.Map(_dealId, null, null, null, null, null);

        Assert.Null(result.SalesCompsJson);
    }

    // --- Time Series Mapping ---

    [Fact]
    public void MapTimeSeries_SerializesTrendsToJson()
    {
        var timeSeries = new TimeSeriesData
        {
            RentTrend = new List<DataPoint>
            {
                new() { Date = new DateTime(2025, 1, 1), Value = 1150m },
                new() { Date = new DateTime(2025, 6, 1), Value = 1200m }
            },
            OccupancyTrend = new List<DataPoint>
            {
                new() { Date = new DateTime(2025, 1, 1), Value = 0.93m }
            }
        };

        var result = RealAiDataMapper.Map(_dealId, null, null, null, null, timeSeries);

        Assert.NotNull(result.RentTrendJson);
        Assert.NotNull(result.OccupancyTrendJson);

        var rentTrend = JsonSerializer.Deserialize<List<DataPoint>>(
            result.RentTrendJson!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(2, rentTrend!.Count);
        Assert.Equal(1150m, rentTrend[0].Value);

        var occTrend = JsonSerializer.Deserialize<List<DataPoint>>(
            result.OccupancyTrendJson!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Single(occTrend!);
    }

    [Fact]
    public void MapTimeSeries_Null_SetsNullJson()
    {
        var result = RealAiDataMapper.Map(_dealId, null, null, null, null, null);

        Assert.Null(result.RentTrendJson);
        Assert.Null(result.OccupancyTrendJson);
    }

    // --- Full Mapping ---

    [Fact]
    public void Map_SetsDealIdAndFetchedAt()
    {
        var before = DateTime.UtcNow;
        var result = RealAiDataMapper.Map(_dealId, null, null, null, null, null);
        var after = DateTime.UtcNow;

        Assert.Equal(_dealId, result.DealId);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.InRange(result.FetchedAt, before, after);
    }

    [Fact]
    public void Map_AllDataPopulated_MapsEverything()
    {
        var property = new PropertyData
        {
            InPlaceRent = 1200m, Occupancy = 0.95m, YearBuilt = 1990,
            Acreage = 2.5m, SquareFootage = 50000, Amenities = "Pool", BuildingType = "Garden"
        };
        var tenant = new TenantMetrics
        {
            Subject = new MetricLevel { AverageFico = 720, RentToIncomeRatio = 0.28m, MedianHhi = 55000m }
        };
        var market = new MarketData
        {
            CapRate = 0.065m, RentGrowth = 0.035m, JobGrowth = 0.025m, NetMigration = 15000, Permits = 8500
        };
        var comps = new List<SalesComp>
        {
            new() { Address = "100 Oak Ave", PricePerUnit = 95000m }
        };
        var timeSeries = new TimeSeriesData
        {
            RentTrend = new List<DataPoint> { new() { Date = DateTime.Today, Value = 1150m } },
            OccupancyTrend = new List<DataPoint> { new() { Date = DateTime.Today, Value = 0.93m } }
        };

        var result = RealAiDataMapper.Map(_dealId, property, tenant, market, comps, timeSeries);

        // Property
        Assert.Equal(1200m, result.InPlaceRent);
        Assert.Equal("Garden", result.BuildingType);
        // Tenant
        Assert.Equal(720, result.AverageFico);
        // Market
        Assert.Equal(0.065m, result.MarketCapRate);
        Assert.Equal(8500, result.Permits);
        // Comps JSON
        Assert.NotNull(result.SalesCompsJson);
        // Time series JSON
        Assert.NotNull(result.RentTrendJson);
        Assert.NotNull(result.OccupancyTrendJson);
    }
}
