using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class EmailCodeServiceTests
{
    private readonly EmailCodeService _sut;

    public EmailCodeServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new EmailCodeService(cache, NullLogger<EmailCodeService>.Instance);
    }

    [Fact]
    public void GenerateCode_Returns_SixDigitString()
    {
        var code = _sut.GenerateCode("test@example.com");
        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out var num));
        Assert.InRange(num, 100000, 999999);
    }

    [Fact]
    public void ValidateCode_Correct_Code_Returns_True()
    {
        var code = _sut.GenerateCode("user@test.com");
        Assert.True(_sut.ValidateCode("user@test.com", code));
    }

    [Fact]
    public void ValidateCode_Wrong_Code_Returns_False()
    {
        _sut.GenerateCode("user@test.com");
        Assert.False(_sut.ValidateCode("user@test.com", "000000"));
    }

    [Fact]
    public void ValidateCode_Is_OneTimeUse()
    {
        var code = _sut.GenerateCode("user@test.com");
        Assert.True(_sut.ValidateCode("user@test.com", code));
        Assert.False(_sut.ValidateCode("user@test.com", code));
    }

    [Fact]
    public void ValidateCode_No_Code_Generated_Returns_False()
    {
        Assert.False(_sut.ValidateCode("nobody@test.com", "123456"));
    }

    [Fact]
    public void ValidateCode_Is_CaseInsensitive_For_Email()
    {
        var code = _sut.GenerateCode("User@Test.com");
        Assert.True(_sut.ValidateCode("user@test.com", code));
    }
}
