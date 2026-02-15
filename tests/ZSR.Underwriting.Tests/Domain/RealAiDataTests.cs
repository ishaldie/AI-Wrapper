using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Domain;

public class RealAiDataTests
{
    [Fact]
    public void New_RealAiData_Has_NonEmpty_Id()
    {
        var dealId = Guid.NewGuid();
        var data = new RealAiData(dealId);
        Assert.NotEqual(Guid.Empty, data.Id);
    }

    [Fact]
    public void New_RealAiData_Sets_DealId()
    {
        var dealId = Guid.NewGuid();
        var data = new RealAiData(dealId);
        Assert.Equal(dealId, data.DealId);
    }

    [Fact]
    public void New_RealAiData_Sets_FetchedAt()
    {
        var before = DateTime.UtcNow;
        var data = new RealAiData(Guid.NewGuid());
        var after = DateTime.UtcNow;
        Assert.InRange(data.FetchedAt, before, after);
    }

    [Fact]
    public void Constructor_Throws_When_DealId_Empty()
    {
        Assert.Throws<ArgumentException>(() => new RealAiData(Guid.Empty));
    }

    [Fact]
    public void Property_Data_Fields_Default_To_Null()
    {
        var data = new RealAiData(Guid.NewGuid());
        Assert.Null(data.InPlaceRent);
        Assert.Null(data.Occupancy);
        Assert.Null(data.YearBuilt);
        Assert.Null(data.BuildingType);
        Assert.Null(data.Acreage);
        Assert.Null(data.SquareFootage);
        Assert.Null(data.Amenities);
    }

    [Fact]
    public void Tenant_Metrics_Fields_Default_To_Null()
    {
        var data = new RealAiData(Guid.NewGuid());
        Assert.Null(data.AverageFico);
        Assert.Null(data.RentToIncomeRatio);
        Assert.Null(data.MedianHhi);
    }

    [Fact]
    public void Market_Data_Fields_Default_To_Null()
    {
        var data = new RealAiData(Guid.NewGuid());
        Assert.Null(data.MarketCapRate);
        Assert.Null(data.RentGrowth);
        Assert.Null(data.JobGrowth);
        Assert.Null(data.NetMigration);
        Assert.Null(data.Permits);
    }

    [Fact]
    public void Can_Set_Property_Data()
    {
        var data = new RealAiData(Guid.NewGuid());
        data.InPlaceRent = 1200m;
        data.Occupancy = 0.94m;
        data.YearBuilt = 1985;
        data.BuildingType = "Garden";

        Assert.Equal(1200m, data.InPlaceRent);
        Assert.Equal(0.94m, data.Occupancy);
        Assert.Equal(1985, data.YearBuilt);
        Assert.Equal("Garden", data.BuildingType);
    }

    [Fact]
    public void Json_Fields_Default_To_Null()
    {
        var data = new RealAiData(Guid.NewGuid());
        Assert.Null(data.SalesCompsJson);
        Assert.Null(data.RentTrendJson);
        Assert.Null(data.OccupancyTrendJson);
    }
}
