using System.Net;
using System.Text.Json;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class MarketDataServiceTests
{
    private static IWebSearchService CreateMockSearchService(
        Dictionary<MarketSearchCategory, List<WebSearchResult>>? results = null)
    {
        return new FakeWebSearchService(results ?? new());
    }

    [Fact]
    public async Task GetMarketContextAsync_Returns_Populated_Dto()
    {
        var searchResults = new Dictionary<MarketSearchCategory, List<WebSearchResult>>
        {
            [MarketSearchCategory.MajorEmployers] = new()
            {
                new() { Title = "Top Employers Dallas", Snippet = "Amazon, AT&T, and Texas Instruments are major employers", SourceUrl = "https://example.com/1", Category = MarketSearchCategory.MajorEmployers }
            },
            [MarketSearchCategory.FannieMaeRates] = new()
            {
                new() { Title = "Fannie Mae Rates", Snippet = "Current multifamily rate is 5.75%", SourceUrl = "https://example.com/rates", Category = MarketSearchCategory.FannieMaeRates }
            }
        };

        var searchService = CreateMockSearchService(searchResults);
        var service = new MarketDataService(searchService);

        var context = await service.GetMarketContextAsync("Dallas", "TX");

        Assert.NotNull(context);
        Assert.NotEmpty(context.MajorEmployers);
        Assert.True(context.RetrievedAt > default(DateTime));
    }

    [Fact]
    public async Task GetMarketContextAsync_Parses_Employer_Names()
    {
        var searchResults = new Dictionary<MarketSearchCategory, List<WebSearchResult>>
        {
            [MarketSearchCategory.MajorEmployers] = new()
            {
                new() { Title = "Top Employers", Snippet = "Amazon, AT&T, and Texas Instruments are major employers in the area", SourceUrl = "https://example.com/1", Category = MarketSearchCategory.MajorEmployers }
            }
        };

        var searchService = CreateMockSearchService(searchResults);
        var service = new MarketDataService(searchService);

        var context = await service.GetMarketContextAsync("Dallas", "TX");

        Assert.NotEmpty(context.MajorEmployers);
        Assert.Equal("Top Employers", context.MajorEmployers[0].Name);
    }

    [Fact]
    public async Task GetMarketContextAsync_Tracks_Source_Urls()
    {
        var searchResults = new Dictionary<MarketSearchCategory, List<WebSearchResult>>
        {
            [MarketSearchCategory.EconomicDrivers] = new()
            {
                new() { Title = "Economy", Snippet = "Strong growth", SourceUrl = "https://example.com/econ", Category = MarketSearchCategory.EconomicDrivers }
            }
        };

        var searchService = CreateMockSearchService(searchResults);
        var service = new MarketDataService(searchService);

        var context = await service.GetMarketContextAsync("Dallas", "TX");

        Assert.NotEmpty(context.SourceUrls);
    }

    [Fact]
    public async Task GetMarketContextAsync_Returns_Fallback_When_No_Results()
    {
        var searchService = CreateMockSearchService(); // empty results
        var service = new MarketDataService(searchService);

        var context = await service.GetMarketContextAsync("Nowhere", "XX");

        // Should still return a valid DTO with fallback content
        Assert.NotNull(context);
        Assert.NotNull(context.MajorEmployers);
        Assert.True(context.RetrievedAt > default(DateTime));
    }

    [Fact]
    public async Task GetMarketContextAsync_Caches_Per_Deal()
    {
        var callCount = 0;
        var searchResults = new Dictionary<MarketSearchCategory, List<WebSearchResult>>
        {
            [MarketSearchCategory.MajorEmployers] = new()
            {
                new() { Title = "Employers", Snippet = "Test", SourceUrl = "https://example.com", Category = MarketSearchCategory.MajorEmployers }
            }
        };

        var searchService = new CountingWebSearchService(searchResults, () => callCount++);
        var service = new MarketDataService(searchService);

        var dealId = Guid.NewGuid();
        var result1 = await service.GetMarketContextForDealAsync(dealId, "Dallas", "TX");
        var result2 = await service.GetMarketContextForDealAsync(dealId, "Dallas", "TX");

        // Second call should use cache, not hit search again
        Assert.Equal(result1.RetrievedAt, result2.RetrievedAt);
    }

    [Fact]
    public async Task GetMarketContextAsync_Different_Deals_Not_Cached()
    {
        var callCount = 0;
        var searchResults = new Dictionary<MarketSearchCategory, List<WebSearchResult>>
        {
            [MarketSearchCategory.MajorEmployers] = new()
            {
                new() { Title = "Employers", Snippet = "Test", SourceUrl = "https://example.com", Category = MarketSearchCategory.MajorEmployers }
            }
        };

        var searchService = new CountingWebSearchService(searchResults, () => callCount++);
        var service = new MarketDataService(searchService);

        await service.GetMarketContextForDealAsync(Guid.NewGuid(), "Dallas", "TX");
        await service.GetMarketContextForDealAsync(Guid.NewGuid(), "Austin", "TX");

        // Each deal should trigger its own search
        Assert.True(callCount >= 2);
    }

    [Fact]
    public async Task GetMarketContextAsync_Parses_Rate_Value()
    {
        var searchResults = new Dictionary<MarketSearchCategory, List<WebSearchResult>>
        {
            [MarketSearchCategory.FannieMaeRates] = new()
            {
                new() { Title = "Rates", Snippet = "The current Fannie Mae multifamily rate is 5.75% for fixed terms", SourceUrl = "https://example.com/rates", Category = MarketSearchCategory.FannieMaeRates }
            }
        };

        var searchService = CreateMockSearchService(searchResults);
        var service = new MarketDataService(searchService);

        var context = await service.GetMarketContextAsync("Dallas", "TX");

        Assert.NotNull(context.CurrentFannieMaeRate);
        Assert.Equal(5.75m, context.CurrentFannieMaeRate);
    }

    // === Helper fakes ===

    private class FakeWebSearchService : IWebSearchService
    {
        private readonly Dictionary<MarketSearchCategory, List<WebSearchResult>> _results;

        public FakeWebSearchService(Dictionary<MarketSearchCategory, List<WebSearchResult>> results)
        {
            _results = results;
        }

        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(string query, MarketSearchCategory category, int maxResults = 5)
        {
            if (_results.TryGetValue(category, out var list))
                return Task.FromResult<IReadOnlyList<WebSearchResult>>(list.Take(maxResults).ToList().AsReadOnly());

            return Task.FromResult<IReadOnlyList<WebSearchResult>>(Array.Empty<WebSearchResult>());
        }
    }

    private class CountingWebSearchService : IWebSearchService
    {
        private readonly Dictionary<MarketSearchCategory, List<WebSearchResult>> _results;
        private readonly Action _onSearch;

        public CountingWebSearchService(Dictionary<MarketSearchCategory, List<WebSearchResult>> results, Action onSearch)
        {
            _results = results;
            _onSearch = onSearch;
        }

        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(string query, MarketSearchCategory category, int maxResults = 5)
        {
            _onSearch();
            if (_results.TryGetValue(category, out var list))
                return Task.FromResult<IReadOnlyList<WebSearchResult>>(list.Take(maxResults).ToList().AsReadOnly());

            return Task.FromResult<IReadOnlyList<WebSearchResult>>(Array.Empty<WebSearchResult>());
        }
    }
}
