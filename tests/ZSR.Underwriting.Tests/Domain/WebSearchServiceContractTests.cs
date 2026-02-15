using System.Reflection;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Tests.Domain;

public class WebSearchServiceContractTests
{
    [Fact]
    public void IWebSearchService_Has_SearchAsync_Method()
    {
        var method = typeof(IWebSearchService).GetMethod("SearchAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<IReadOnlyList<WebSearchResult>>), method!.ReturnType);
    }

    [Fact]
    public void IWebSearchService_SearchAsync_Takes_Query_And_Category()
    {
        var method = typeof(IWebSearchService).GetMethod("SearchAsync");
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal("query", parameters[0].Name);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("category", parameters[1].Name);
        Assert.Equal(typeof(MarketSearchCategory), parameters[1].ParameterType);
        Assert.Equal("maxResults", parameters[2].Name);
        Assert.Equal(typeof(int), parameters[2].ParameterType);
    }

    [Fact]
    public void MarketSearchCategory_Has_All_Protocol_Values()
    {
        Assert.True(Enum.IsDefined(typeof(MarketSearchCategory), MarketSearchCategory.MajorEmployers));
        Assert.True(Enum.IsDefined(typeof(MarketSearchCategory), MarketSearchCategory.ConstructionPipeline));
        Assert.True(Enum.IsDefined(typeof(MarketSearchCategory), MarketSearchCategory.EconomicDrivers));
        Assert.True(Enum.IsDefined(typeof(MarketSearchCategory), MarketSearchCategory.Infrastructure));
        Assert.True(Enum.IsDefined(typeof(MarketSearchCategory), MarketSearchCategory.ComparableTransactions));
        Assert.True(Enum.IsDefined(typeof(MarketSearchCategory), MarketSearchCategory.FannieMaeRates));
    }

    [Fact]
    public void WebSearchResult_Has_Required_Properties()
    {
        var titleProp = typeof(WebSearchResult).GetProperty("Title");
        var snippetProp = typeof(WebSearchResult).GetProperty("Snippet");
        var sourceUrlProp = typeof(WebSearchResult).GetProperty("SourceUrl");
        var categoryProp = typeof(WebSearchResult).GetProperty("Category");
        var retrievedAtProp = typeof(WebSearchResult).GetProperty("RetrievedAt");

        Assert.NotNull(titleProp);
        Assert.NotNull(snippetProp);
        Assert.NotNull(sourceUrlProp);
        Assert.NotNull(categoryProp);
        Assert.NotNull(retrievedAtProp);

        Assert.Equal(typeof(string), titleProp!.PropertyType);
        Assert.Equal(typeof(string), snippetProp!.PropertyType);
        Assert.Equal(typeof(string), sourceUrlProp!.PropertyType);
        Assert.Equal(typeof(MarketSearchCategory), categoryProp!.PropertyType);
        Assert.Equal(typeof(DateTime), retrievedAtProp!.PropertyType);
    }

    [Fact]
    public void WebSearchResult_Can_Be_Instantiated()
    {
        var result = new WebSearchResult
        {
            Title = "Test Title",
            Snippet = "Test snippet content",
            SourceUrl = "https://example.com",
            Category = MarketSearchCategory.MajorEmployers,
            RetrievedAt = DateTime.UtcNow
        };

        Assert.Equal("Test Title", result.Title);
        Assert.Equal("Test snippet content", result.Snippet);
        Assert.Equal("https://example.com", result.SourceUrl);
        Assert.Equal(MarketSearchCategory.MajorEmployers, result.Category);
    }
}
