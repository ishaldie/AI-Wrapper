using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Application.Services;

public class MarketDataService
{
    private readonly IWebSearchService _searchService;
    private readonly ConcurrentDictionary<Guid, MarketContextDto> _dealCache = new();

    public MarketDataService(IWebSearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<MarketContextDto> GetMarketContextForDealAsync(Guid dealId, string city, string state)
    {
        if (_dealCache.TryGetValue(dealId, out var cached))
            return cached;

        var context = await GetMarketContextAsync(city, state);
        _dealCache.TryAdd(dealId, context);
        return context;
    }

    public async Task<MarketContextDto> GetMarketContextAsync(string city, string state)
    {
        var context = new MarketContextDto
        {
            RetrievedAt = DateTime.UtcNow
        };

        var categories = Enum.GetValues<MarketSearchCategory>();
        foreach (var category in categories)
        {
            var query = MarketSearchQueryBuilder.BuildQuery(city, state, category);
            var results = await _searchService.SearchAsync(query, category);
            PopulateCategory(context, category, results);
        }

        return context;
    }

    private static void PopulateCategory(
        MarketContextDto context,
        MarketSearchCategory category,
        IReadOnlyList<WebSearchResult> results)
    {
        var items = results.Select(r => new MarketDataItem
        {
            Name = r.Title,
            Description = r.Snippet,
            SourceUrl = r.SourceUrl
        }).ToList();

        var urls = results.Select(r => r.SourceUrl).Where(u => !string.IsNullOrEmpty(u)).ToList();
        if (urls.Count > 0)
        {
            context.SourceUrls[category.ToString()] = urls;
        }

        switch (category)
        {
            case MarketSearchCategory.MajorEmployers:
                context.MajorEmployers = items;
                break;
            case MarketSearchCategory.ConstructionPipeline:
                context.ConstructionPipeline = items;
                break;
            case MarketSearchCategory.EconomicDrivers:
                context.EconomicDrivers = items;
                break;
            case MarketSearchCategory.Infrastructure:
                context.InfrastructureProjects = items;
                break;
            case MarketSearchCategory.ComparableTransactions:
                context.ComparableTransactions = items;
                break;
            case MarketSearchCategory.FannieMaeRates:
                context.CurrentFannieMaeRate = ParseRate(results);
                break;
        }
    }

    private static decimal? ParseRate(IReadOnlyList<WebSearchResult> results)
    {
        foreach (var result in results)
        {
            var match = Regex.Match(result.Snippet, @"(\d+\.?\d*)\s*%");
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var rate))
                return rate;
        }

        return null;
    }
}
