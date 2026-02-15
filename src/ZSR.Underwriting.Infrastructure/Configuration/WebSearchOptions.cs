namespace ZSR.Underwriting.Infrastructure.Configuration;

public class WebSearchOptions
{
    public const string SectionName = "WebSearch";

    public string ApiKey { get; set; } = string.Empty;
    public string SearchEngineId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.googleapis.com/customsearch/v1";
    public int MaxRequestsPerMinute { get; set; } = 10;
}
