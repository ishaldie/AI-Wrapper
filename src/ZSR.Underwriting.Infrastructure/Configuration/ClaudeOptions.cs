namespace ZSR.Underwriting.Infrastructure.Configuration;

public class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";
    public int MaxTokens { get; set; } = 4096;
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public int TimeoutSeconds { get; set; } = 120;
    public int RetryCount { get; set; } = 3;
}
