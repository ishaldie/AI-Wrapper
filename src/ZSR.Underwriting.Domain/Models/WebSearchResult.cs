using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Models;

public class WebSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public MarketSearchCategory Category { get; set; }
    public DateTime RetrievedAt { get; set; }
}
