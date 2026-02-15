namespace ZSR.Underwriting.Infrastructure.Configuration;

public class RealAiOptions
{
    public const string SectionName = "RealAI";

    public string BaseUrl { get; set; } = "https://app.realai.com/api";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
}
