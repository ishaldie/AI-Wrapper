using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Domain;

public class DealTests
{
    [Fact]
    public void New_Deal_Has_Draft_Status()
    {
        var deal = new Deal("Test Property");
        Assert.Equal(DealStatus.Draft, deal.Status);
    }

    [Fact]
    public void New_Deal_Sets_Name()
    {
        var deal = new Deal("Sunset Apartments");
        Assert.Equal("Sunset Apartments", deal.Name);
    }

    [Fact]
    public void New_Deal_Has_NonEmpty_Id()
    {
        var deal = new Deal("Test Property");
        Assert.NotEqual(Guid.Empty, deal.Id);
    }

    [Fact]
    public void New_Deal_Sets_CreatedAt()
    {
        var before = DateTime.UtcNow;
        var deal = new Deal("Test Property");
        var after = DateTime.UtcNow;

        Assert.InRange(deal.CreatedAt, before, after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_When_Name_Is_Empty(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Deal(name!));
    }

    [Fact]
    public void UpdateStatus_Changes_Status()
    {
        var deal = new Deal("Test Property");
        deal.UpdateStatus(DealStatus.InProgress);
        Assert.Equal(DealStatus.InProgress, deal.Status);
    }

    [Fact]
    public void DealStatus_Has_Expected_Values()
    {
        Assert.True(Enum.IsDefined(typeof(DealStatus), DealStatus.Draft));
        Assert.True(Enum.IsDefined(typeof(DealStatus), DealStatus.InProgress));
        Assert.True(Enum.IsDefined(typeof(DealStatus), DealStatus.Complete));
        Assert.True(Enum.IsDefined(typeof(DealStatus), DealStatus.Archived));
    }

    [Fact]
    public void New_Deal_Has_Empty_Navigation_Collections()
    {
        var deal = new Deal("Test Property");
        Assert.Null(deal.Property);
        Assert.Null(deal.UnderwritingInput);
        Assert.Null(deal.CalculationResult);
        Assert.Null(deal.Report);
        Assert.NotNull(deal.UploadedDocuments);
        Assert.Empty(deal.UploadedDocuments);
    }
}
