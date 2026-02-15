using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Services;

public class MarketSearchQueryBuilderTests
{
    [Fact]
    public void BuildQuery_MajorEmployers_IncludesLocationAndEmployers()
    {
        var query = MarketSearchQueryBuilder.BuildQuery("Dallas", "TX", MarketSearchCategory.MajorEmployers);

        Assert.Contains("Dallas", query);
        Assert.Contains("TX", query);
        Assert.Contains("employer", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildQuery_ConstructionPipeline_IncludesLocationAndConstruction()
    {
        var query = MarketSearchQueryBuilder.BuildQuery("Dallas", "TX", MarketSearchCategory.ConstructionPipeline);

        Assert.Contains("Dallas", query);
        Assert.Contains("construction", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildQuery_EconomicDrivers_IncludesLocationAndEconomy()
    {
        var query = MarketSearchQueryBuilder.BuildQuery("Dallas", "TX", MarketSearchCategory.EconomicDrivers);

        Assert.Contains("Dallas", query);
        Assert.Contains("econom", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildQuery_Infrastructure_IncludesLocationAndInfrastructure()
    {
        var query = MarketSearchQueryBuilder.BuildQuery("Dallas", "TX", MarketSearchCategory.Infrastructure);

        Assert.Contains("Dallas", query);
        Assert.Contains("infrastructure", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildQuery_ComparableTransactions_IncludesLocationAndMultifamily()
    {
        var query = MarketSearchQueryBuilder.BuildQuery("Dallas", "TX", MarketSearchCategory.ComparableTransactions);

        Assert.Contains("Dallas", query);
        Assert.Contains("multifamily", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildQuery_FannieMaeRates_IncludesFannieMae()
    {
        var query = MarketSearchQueryBuilder.BuildQuery("Dallas", "TX", MarketSearchCategory.FannieMaeRates);

        Assert.Contains("Fannie Mae", query);
        Assert.Contains("rate", query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildQuery_FannieMaeRates_DoesNotIncludeLocation()
    {
        var query = MarketSearchQueryBuilder.BuildQuery("Dallas", "TX", MarketSearchCategory.FannieMaeRates);

        // Rates are national, not location-specific
        Assert.DoesNotContain("Dallas", query);
    }

    [Theory]
    [InlineData(MarketSearchCategory.MajorEmployers)]
    [InlineData(MarketSearchCategory.ConstructionPipeline)]
    [InlineData(MarketSearchCategory.EconomicDrivers)]
    [InlineData(MarketSearchCategory.Infrastructure)]
    [InlineData(MarketSearchCategory.ComparableTransactions)]
    [InlineData(MarketSearchCategory.FannieMaeRates)]
    public void BuildQuery_AllCategories_ReturnsNonEmptyString(MarketSearchCategory category)
    {
        var query = MarketSearchQueryBuilder.BuildQuery("Austin", "TX", category);

        Assert.False(string.IsNullOrWhiteSpace(query));
    }
}
