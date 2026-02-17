using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class EmailCodeServiceTests
{
    private readonly EmailCodeService _sut;

    public EmailCodeServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var smtpOptions = Options.Create(new SmtpOptions());
        _sut = new EmailCodeService(cache, NullLogger<EmailCodeService>.Instance, smtpOptions);
    }

    [Fact]
    public async Task GenerateCodeAsync_Returns_SixDigitString()
    {
        var code = await _sut.GenerateCodeAsync("test@example.com");
        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out var num));
        Assert.InRange(num, 100000, 999999);
    }

    [Fact]
    public async Task ValidateCode_Correct_Code_Returns_True()
    {
        var code = await _sut.GenerateCodeAsync("user@test.com");
        Assert.True(_sut.ValidateCode("user@test.com", code));
    }

    [Fact]
    public async Task ValidateCode_Wrong_Code_Returns_False()
    {
        await _sut.GenerateCodeAsync("user@test.com");
        Assert.False(_sut.ValidateCode("user@test.com", "000000"));
    }

    [Fact]
    public async Task ValidateCode_Is_OneTimeUse()
    {
        var code = await _sut.GenerateCodeAsync("user@test.com");
        Assert.True(_sut.ValidateCode("user@test.com", code));
        Assert.False(_sut.ValidateCode("user@test.com", code));
    }

    [Fact]
    public void ValidateCode_No_Code_Generated_Returns_False()
    {
        Assert.False(_sut.ValidateCode("nobody@test.com", "123456"));
    }

    [Fact]
    public async Task ValidateCode_Is_CaseInsensitive_For_Email()
    {
        var code = await _sut.GenerateCodeAsync("User@Test.com");
        Assert.True(_sut.ValidateCode("user@test.com", code));
    }

    [Fact]
    public async Task GenerateCodeAsync_WithNoSmtpConfig_FallsBackToConsole()
    {
        // Default SmtpOptions has empty Username/Host — should not throw
        var code = await _sut.GenerateCodeAsync("fallback@test.com");
        Assert.Equal(6, code.Length);
        Assert.True(_sut.ValidateCode("fallback@test.com", code));
    }
}
