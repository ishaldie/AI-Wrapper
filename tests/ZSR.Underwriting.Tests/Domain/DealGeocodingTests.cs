using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Domain;

public class DealGeocodingTests
{
    [Fact]
    public void New_Deal_Has_Null_Coordinates()
    {
        var deal = new Deal("Test Property");

        Assert.Null(deal.Latitude);
        Assert.Null(deal.Longitude);
    }

    [Fact]
    public void Deal_Stores_Latitude_And_Longitude()
    {
        var deal = new Deal("Test Property");

        deal.Latitude = 40.712776;
        deal.Longitude = -74.005974;

        Assert.Equal(40.712776, deal.Latitude);
        Assert.Equal(-74.005974, deal.Longitude);
    }

    [Fact]
    public void Deal_Coordinates_Can_Be_Cleared()
    {
        var deal = new Deal("Test Property");
        deal.Latitude = 40.712776;
        deal.Longitude = -74.005974;

        deal.Latitude = null;
        deal.Longitude = null;

        Assert.Null(deal.Latitude);
        Assert.Null(deal.Longitude);
    }
}
