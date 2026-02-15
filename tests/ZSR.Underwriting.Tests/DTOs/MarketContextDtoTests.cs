using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Tests.DTOs;

public class MarketContextDtoTests
{
    [Fact]
    public void MarketContextDto_Has_MajorEmployers_List()
    {
        var dto = new MarketContextDto();
        Assert.NotNull(dto.MajorEmployers);
        Assert.Empty(dto.MajorEmployers);
    }

    [Fact]
    public void MarketContextDto_Has_ConstructionPipeline_List()
    {
        var dto = new MarketContextDto();
        Assert.NotNull(dto.ConstructionPipeline);
        Assert.Empty(dto.ConstructionPipeline);
    }

    [Fact]
    public void MarketContextDto_Has_EconomicDrivers_List()
    {
        var dto = new MarketContextDto();
        Assert.NotNull(dto.EconomicDrivers);
        Assert.Empty(dto.EconomicDrivers);
    }

    [Fact]
    public void MarketContextDto_Has_InfrastructureProjects_List()
    {
        var dto = new MarketContextDto();
        Assert.NotNull(dto.InfrastructureProjects);
        Assert.Empty(dto.InfrastructureProjects);
    }

    [Fact]
    public void MarketContextDto_Has_ComparableTransactions_List()
    {
        var dto = new MarketContextDto();
        Assert.NotNull(dto.ComparableTransactions);
        Assert.Empty(dto.ComparableTransactions);
    }

    [Fact]
    public void MarketContextDto_Has_FannieMaeRate_Nullable()
    {
        var dto = new MarketContextDto();
        Assert.Null(dto.CurrentFannieMaeRate);
    }

    [Fact]
    public void MarketContextDto_Has_SourceUrls_Dictionary()
    {
        var dto = new MarketContextDto();
        Assert.NotNull(dto.SourceUrls);
        Assert.Empty(dto.SourceUrls);
    }

    [Fact]
    public void MarketContextDto_Has_RetrievedAt_Timestamp()
    {
        var dto = new MarketContextDto();
        Assert.Equal(default, dto.RetrievedAt);
    }

    [Fact]
    public void MarketDataItem_Has_Name_And_Description()
    {
        var item = new MarketDataItem
        {
            Name = "Amazon",
            Description = "Major tech employer with 10,000+ local employees",
            SourceUrl = "https://example.com/employers"
        };

        Assert.Equal("Amazon", item.Name);
        Assert.Equal("Major tech employer with 10,000+ local employees", item.Description);
        Assert.Equal("https://example.com/employers", item.SourceUrl);
    }

    [Fact]
    public void MarketContextDto_Can_Be_Populated()
    {
        var dto = new MarketContextDto
        {
            MajorEmployers = new List<MarketDataItem>
            {
                new() { Name = "Amazon", Description = "Tech", SourceUrl = "https://example.com" }
            },
            CurrentFannieMaeRate = 5.75m,
            RetrievedAt = DateTime.UtcNow
        };

        Assert.Single(dto.MajorEmployers);
        Assert.Equal(5.75m, dto.CurrentFannieMaeRate);
        Assert.True(dto.RetrievedAt > default(DateTime));
    }
}
