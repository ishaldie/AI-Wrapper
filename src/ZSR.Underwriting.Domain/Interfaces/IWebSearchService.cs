using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Domain.Interfaces;

public interface IWebSearchService
{
    Task<IReadOnlyList<WebSearchResult>> SearchAsync(string query, MarketSearchCategory category, int maxResults = 5);
}
