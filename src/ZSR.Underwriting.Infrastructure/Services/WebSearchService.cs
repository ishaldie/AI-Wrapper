using System.Text.Json;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Infrastructure.Services;

public class WebSearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WebSearchService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(
        string query,
        MarketSearchCategory category,
        int maxResults = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"?q={Uri.EscapeDataString(query)}&num={maxResults}");

            if (!response.IsSuccessStatusCode)
                return Array.Empty<WebSearchResult>();

            var content = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<SearchApiResponse>(content, JsonOptions);

            if (searchResponse?.Items is null || searchResponse.Items.Length == 0)
                return Array.Empty<WebSearchResult>();

            var now = DateTime.UtcNow;
            return searchResponse.Items
                .Take(maxResults)
                .Select(item => new WebSearchResult
                {
                    Title = item.Title ?? string.Empty,
                    Snippet = item.Snippet ?? string.Empty,
                    SourceUrl = item.Link ?? string.Empty,
                    Category = category,
                    RetrievedAt = now
                })
                .ToList()
                .AsReadOnly();
        }
        catch (JsonException)
        {
            return Array.Empty<WebSearchResult>();
        }
        catch (HttpRequestException)
        {
            return Array.Empty<WebSearchResult>();
        }
    }

    private class SearchApiResponse
    {
        public SearchItem[]? Items { get; set; }
    }

    private class SearchItem
    {
        public string? Title { get; set; }
        public string? Snippet { get; set; }
        public string? Link { get; set; }
    }
}
