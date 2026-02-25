using ZSR.Underwriting.Infrastructure.Configuration;

namespace ZSR.Underwriting.Tests.Claude;

public class ClaudeOptionsTests
{
    [Fact]
    public void SectionName_Is_Claude()
    {
        Assert.Equal("Claude", ClaudeOptions.SectionName);
    }

    [Fact]
    public void Defaults_AreReasonable()
    {
        var options = new ClaudeOptions();

        Assert.Equal("claude-opus-4-6-20250918", options.Model);
        Assert.Equal(4096, options.MaxTokens);
        Assert.Equal("https://api.anthropic.com", options.BaseUrl);
        Assert.Equal(120, options.TimeoutSeconds);
        Assert.Equal(3, options.RetryCount);
        Assert.Equal(string.Empty, options.ApiKey);
    }

    [Fact]
    public void AllProperties_AreSettable()
    {
        var options = new ClaudeOptions
        {
            ApiKey = "sk-ant-test",
            Model = "claude-opus-4-6",
            MaxTokens = 8192,
            BaseUrl = "https://custom.api.com",
            TimeoutSeconds = 60,
            RetryCount = 5
        };

        Assert.Equal("sk-ant-test", options.ApiKey);
        Assert.Equal("claude-opus-4-6", options.Model);
        Assert.Equal(8192, options.MaxTokens);
        Assert.Equal("https://custom.api.com", options.BaseUrl);
        Assert.Equal(60, options.TimeoutSeconds);
        Assert.Equal(5, options.RetryCount);
    }
}
