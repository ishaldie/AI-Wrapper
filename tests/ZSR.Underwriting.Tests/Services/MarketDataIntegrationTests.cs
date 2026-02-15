using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Tests.Services;

public class MarketDataIntegrationTests
{
    private static MarketDataService CreateServiceWithResults(
        Dictionary<MarketSearchCategory, List<WebSearchResult>>? results = null)
    {
        var searchService = new FakeWebSearchService(results ?? new());
        return new MarketDataService(searchService);
    }

    [Fact]
    public void EnrichPropertyComps_Adds_Market_Comps_To_Section()
    {
        var marketContext = new MarketContextDto
        {
            ComparableTransactions = new List<MarketDataItem>
            {
                new() { Name = "Sunset Apartments sold for $5M", Description = "120 units at $41,667/unit, 5.2% cap rate", SourceUrl = "https://example.com/comp1" },
                new() { Name = "River Oaks Complex", Description = "200 units sold at $38,000/unit", SourceUrl = "https://example.com/comp2" }
            },
            SourceUrls = new() { ["ComparableTransactions"] = new() { "https://example.com/comp1", "https://example.com/comp2" } }
        };

        var section = MarketDataEnricher.EnrichPropertyComps(marketContext);

        Assert.NotEqual("[AI-generated comparables analysis pending]", section.Narrative);
        Assert.Contains("market", section.Narrative, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnrichPropertyComps_Returns_Fallback_When_Empty()
    {
        var marketContext = new MarketContextDto();

        var section = MarketDataEnricher.EnrichPropertyComps(marketContext);

        Assert.Contains("unavailable", section.Narrative, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnrichTenantMarket_Adds_Employers_And_Drivers()
    {
        var marketContext = new MarketContextDto
        {
            MajorEmployers = new List<MarketDataItem>
            {
                new() { Name = "Amazon", Description = "10,000+ employees", SourceUrl = "https://example.com/emp" }
            },
            EconomicDrivers = new List<MarketDataItem>
            {
                new() { Name = "Tech sector growth", Description = "Dallas tech growing 15% YoY", SourceUrl = "https://example.com/econ" }
            },
            ConstructionPipeline = new List<MarketDataItem>
            {
                new() { Name = "New apartment complex", Description = "500 units under construction", SourceUrl = "https://example.com/pipeline" }
            }
        };

        var section = MarketDataEnricher.EnrichTenantMarket(marketContext, 1200m, 95m);

        Assert.NotEqual("[AI-generated market intelligence pending]", section.Narrative);
        Assert.Contains("Amazon", section.Narrative);
    }

    [Fact]
    public void EnrichTenantMarket_Returns_Fallback_When_Empty()
    {
        var marketContext = new MarketContextDto();

        var section = MarketDataEnricher.EnrichTenantMarket(marketContext, 1200m, 95m);

        Assert.Contains("unavailable", section.Narrative, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetEffectiveLoanRate_Uses_FannieMae_When_No_User_Rate()
    {
        var marketContext = new MarketContextDto
        {
            CurrentFannieMaeRate = 5.75m
        };

        var rate = MarketDataEnricher.GetEffectiveLoanRate(null, marketContext);

        Assert.Equal(5.75m, rate);
    }

    [Fact]
    public void GetEffectiveLoanRate_Prefers_User_Rate()
    {
        var marketContext = new MarketContextDto
        {
            CurrentFannieMaeRate = 5.75m
        };

        var rate = MarketDataEnricher.GetEffectiveLoanRate(6.25m, marketContext);

        Assert.Equal(6.25m, rate);
    }

    [Fact]
    public void GetEffectiveLoanRate_Returns_Null_When_No_Data()
    {
        var marketContext = new MarketContextDto();

        var rate = MarketDataEnricher.GetEffectiveLoanRate(null, marketContext);

        Assert.Null(rate);
    }

    [Fact]
    public void BuildSourceAttribution_Returns_Formatted_Sources()
    {
        var marketContext = new MarketContextDto
        {
            SourceUrls = new Dictionary<string, List<string>>
            {
                ["MajorEmployers"] = new() { "https://example.com/emp1", "https://example.com/emp2" },
                ["FannieMaeRates"] = new() { "https://example.com/rates" }
            }
        };

        var attribution = MarketDataEnricher.BuildSourceAttribution(marketContext);

        Assert.NotEmpty(attribution);
        Assert.True(attribution.Count >= 2);
    }

    [Fact]
    public async Task FullFlow_Search_Parse_Enrich_Works()
    {
        var searchResults = new Dictionary<MarketSearchCategory, List<WebSearchResult>>
        {
            [MarketSearchCategory.MajorEmployers] = new()
            {
                new() { Title = "Top Employers", Snippet = "Amazon and AT&T", SourceUrl = "https://example.com/emp", Category = MarketSearchCategory.MajorEmployers }
            },
            [MarketSearchCategory.FannieMaeRates] = new()
            {
                new() { Title = "Rates", Snippet = "Current rate 5.50%", SourceUrl = "https://example.com/rates", Category = MarketSearchCategory.FannieMaeRates }
            }
        };

        var service = CreateServiceWithResults(searchResults);
        var context = await service.GetMarketContextAsync("Dallas", "TX");

        // Enrich sections
        var compsSection = MarketDataEnricher.EnrichPropertyComps(context);
        var marketSection = MarketDataEnricher.EnrichTenantMarket(context, 1200m, 95m);
        var rate = MarketDataEnricher.GetEffectiveLoanRate(null, context);
        var sources = MarketDataEnricher.BuildSourceAttribution(context);

        Assert.NotNull(compsSection);
        Assert.NotNull(marketSection);
        Assert.Equal(5.50m, rate);
        Assert.NotNull(sources);
    }

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
}
