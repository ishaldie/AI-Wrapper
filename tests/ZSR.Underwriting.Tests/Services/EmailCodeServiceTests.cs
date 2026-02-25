using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class EmailCodeServiceTests
{
    private readonly EmailCodeService _sut;
    private readonly CapturingLogger<EmailCodeService> _capturingLogger;

    public EmailCodeServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var smtpOptions = Options.Create(new SmtpOptions());
        _capturingLogger = new CapturingLogger<EmailCodeService>();
        _sut = new EmailCodeService(cache, _capturingLogger, smtpOptions);
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

    [Fact]
    public async Task ValidateCode_Rejects_After_5_Failed_Attempts()
    {
        var email = "brute@test.com";
        var code = await _sut.GenerateCodeAsync(email);

        // 5 wrong attempts
        for (int i = 0; i < 5; i++)
            _sut.ValidateCode(email, "000000");

        // 6th attempt with CORRECT code should still fail — code invalidated
        Assert.False(_sut.ValidateCode(email, code));
    }

    [Fact]
    public async Task ValidateCode_Allows_Up_To_4_Wrong_Attempts_Then_Correct()
    {
        var email = "retry@test.com";
        var code = await _sut.GenerateCodeAsync(email);

        // 4 wrong attempts — still under limit
        for (int i = 0; i < 4; i++)
            Assert.False(_sut.ValidateCode(email, "000000"));

        // 5th attempt with correct code should succeed
        Assert.True(_sut.ValidateCode(email, code));
    }

    [Fact]
    public async Task ValidateCode_BruteForce_Counter_Resets_On_New_Code()
    {
        var email = "reset@test.com";
        var code1 = await _sut.GenerateCodeAsync(email);

        // Burn 4 attempts on first code
        for (int i = 0; i < 4; i++)
            _sut.ValidateCode(email, "000000");

        // Generate new code — counter should reset
        var code2 = await _sut.GenerateCodeAsync(email);
        Assert.True(_sut.ValidateCode(email, code2));
    }

    [Fact]
    public async Task GenerateCodeAsync_DoesNotLogCodeValue()
    {
        var code = await _sut.GenerateCodeAsync("secret@test.com");

        // No log message should contain the actual code value
        foreach (var msg in _capturingLogger.Messages)
        {
            Assert.DoesNotContain(code, msg);
        }

        // Should still log that a code was sent (without the code)
        Assert.Contains(_capturingLogger.Messages,
            m => m.Contains("secret@test.com", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Logger that captures formatted log messages for assertion.
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
    }
}
