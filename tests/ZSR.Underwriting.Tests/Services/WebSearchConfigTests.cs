using ZSR.Underwriting.Infrastructure.Configuration;

namespace ZSR.Underwriting.Tests.Services;

public class WebSearchConfigTests
{
    [Fact]
    public void WebSearchOptions_Has_ApiKey_Property()
    {
        var options = new WebSearchOptions();
        Assert.NotNull(options);
        Assert.Equal(string.Empty, options.ApiKey);
    }

    [Fact]
    public void WebSearchOptions_Has_SearchEngineId_Property()
    {
        var options = new WebSearchOptions();
        Assert.Equal(string.Empty, options.SearchEngineId);
    }

    [Fact]
    public void WebSearchOptions_Has_BaseUrl_With_Default()
    {
        var options = new WebSearchOptions();
        Assert.False(string.IsNullOrWhiteSpace(options.BaseUrl));
    }

    [Fact]
    public void WebSearchOptions_Has_MaxRequestsPerMinute_With_Default()
    {
        var options = new WebSearchOptions();
        Assert.True(options.MaxRequestsPerMinute > 0);
    }

    [Fact]
    public void WebSearchOptions_Properties_Can_Be_Set()
    {
        var options = new WebSearchOptions
        {
            ApiKey = "test-key",
            SearchEngineId = "test-engine",
            BaseUrl = "https://custom.api.com",
            MaxRequestsPerMinute = 30
        };

        Assert.Equal("test-key", options.ApiKey);
        Assert.Equal("test-engine", options.SearchEngineId);
        Assert.Equal("https://custom.api.com", options.BaseUrl);
        Assert.Equal(30, options.MaxRequestsPerMinute);
    }
}
