using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Domain;

public class PropertyTests
{
    [Fact]
    public void New_Property_Has_NonEmpty_Id()
    {
        var property = new Property("123 Main St", 50);
        Assert.NotEqual(Guid.Empty, property.Id);
    }

    [Fact]
    public void New_Property_Sets_Address()
    {
        var property = new Property("456 Oak Ave, Austin TX", 100);
        Assert.Equal("456 Oak Ave, Austin TX", property.Address);
    }

    [Fact]
    public void New_Property_Sets_UnitCount()
    {
        var property = new Property("123 Main St", 120);
        Assert.Equal(120, property.UnitCount);
    }

    [Fact]
    public void New_Property_Has_Null_Optional_Fields()
    {
        var property = new Property("123 Main St", 50);
        Assert.Null(property.YearBuilt);
        Assert.Null(property.BuildingType);
        Assert.Null(property.Acreage);
        Assert.Null(property.SquareFootage);
    }

    [Fact]
    public void Can_Set_Optional_Fields()
    {
        var property = new Property("123 Main St", 50);
        property.YearBuilt = 1985;
        property.BuildingType = "Garden";
        property.Acreage = 5.2m;
        property.SquareFootage = 120_000;

        Assert.Equal(1985, property.YearBuilt);
        Assert.Equal("Garden", property.BuildingType);
        Assert.Equal(5.2m, property.Acreage);
        Assert.Equal(120_000, property.SquareFootage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_When_Address_Is_Empty(string? address)
    {
        Assert.Throws<ArgumentException>(() => new Property(address!, 50));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Throws_When_UnitCount_Is_Not_Positive(int units)
    {
        Assert.Throws<ArgumentException>(() => new Property("123 Main St", units));
    }

    [Fact]
    public void Property_Has_DealId_For_Relationship()
    {
        var property = new Property("123 Main St", 50);
        Assert.Equal(Guid.Empty, property.DealId);
    }
}
