using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;

namespace ZSR.Underwriting.Tests.Services;

public class CredentialsFileParserTests
{
    [Fact]
    public void Parse_MinimalFormat_ExtractsApiKey()
    {
        var json = """{"api_key": "sk-ant-api03-minimal"}""";
        var result = CredentialsFileParser.Parse(json);

        Assert.Equal("sk-ant-api03-minimal", result.ApiKey);
        Assert.Null(result.Model);
        Assert.Null(result.Label);
    }

    [Fact]
    public void Parse_FullFormat_ExtractsAllFields()
    {
        var json = """
        {
            "api_key": "sk-ant-api03-full",
            "model": "claude-sonnet-4-5-20250514",
            "label": "Work account"
        }
        """;
        var result = CredentialsFileParser.Parse(json);

        Assert.Equal("sk-ant-api03-full", result.ApiKey);
        Assert.Equal("claude-sonnet-4-5-20250514", result.Model);
        Assert.Equal("Work account", result.Label);
    }

    [Fact]
    public void Parse_MissingApiKey_Throws()
    {
        var json = """{"model": "claude-sonnet-4-5-20250514"}""";
        Assert.Throws<ArgumentException>(() => CredentialsFileParser.Parse(json));
    }

    [Fact]
    public void Parse_EmptyApiKey_Throws()
    {
        var json = """{"api_key": ""}""";
        Assert.Throws<ArgumentException>(() => CredentialsFileParser.Parse(json));
    }

    [Fact]
    public void Parse_WhitespaceApiKey_Throws()
    {
        var json = """{"api_key": "   "}""";
        Assert.Throws<ArgumentException>(() => CredentialsFileParser.Parse(json));
    }

    [Fact]
    public void Parse_MalformedJson_Throws()
    {
        var json = "not valid json {{{";
        Assert.Throws<ArgumentException>(() => CredentialsFileParser.Parse(json));
    }

    [Fact]
    public void Parse_NullInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => CredentialsFileParser.Parse(null!));
    }

    [Fact]
    public void Parse_EmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() => CredentialsFileParser.Parse(""));
    }

    [Fact]
    public async Task ParseAsync_FromStream_ExtractsApiKey()
    {
        var json = """{"api_key": "sk-ant-api03-stream", "model": "claude-haiku-4-5-20251001"}""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var result = await CredentialsFileParser.ParseAsync(stream);

        Assert.Equal("sk-ant-api03-stream", result.ApiKey);
        Assert.Equal("claude-haiku-4-5-20251001", result.Model);
    }

    [Fact]
    public async Task ParseAsync_NullStream_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => CredentialsFileParser.ParseAsync(null!));
    }

    [Fact]
    public void Parse_ExtraFields_IgnoresUnknown()
    {
        var json = """{"api_key": "sk-ant-api03-extra", "unknown_field": "value", "another": 42}""";
        var result = CredentialsFileParser.Parse(json);

        Assert.Equal("sk-ant-api03-extra", result.ApiKey);
        Assert.Null(result.Model);
    }
}
