using ZSR.Underwriting.Infrastructure.Configuration;

namespace ZSR.Underwriting.Tests.RealAi;

public class RealAiOptionsTests
{
    [Fact]
    public void Defaults_BaseUrl_IsNotEmpty()
    {
        var options = new RealAiOptions();
        Assert.False(string.IsNullOrWhiteSpace(options.BaseUrl));
    }

    [Fact]
    public void Defaults_TimeoutSeconds_IsPositive()
    {
        var options = new RealAiOptions();
        Assert.True(options.TimeoutSeconds > 0);
    }

    [Fact]
    public void Defaults_RetryCount_IsPositive()
    {
        var options = new RealAiOptions();
        Assert.True(options.RetryCount > 0);
    }

    [Fact]
    public void ApiKey_CanBeSet()
    {
        var options = new RealAiOptions { ApiKey = "test-key-123" };
        Assert.Equal("test-key-123", options.ApiKey);
    }

    [Fact]
    public void SectionName_IsRealAI()
    {
        Assert.Equal("RealAI", RealAiOptions.SectionName);
    }
}
