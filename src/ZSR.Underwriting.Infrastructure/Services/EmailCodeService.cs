using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Infrastructure.Services;

public class EmailCodeService : IEmailCodeService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<EmailCodeService> _logger;
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(10);

    public EmailCodeService(IMemoryCache cache, ILogger<EmailCodeService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public string GenerateCode(string email)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var key = CacheKey(email);
        _cache.Set(key, code, CodeTtl);
        _logger.LogInformation("Verification code for {Email}: {Code}", email, code);
        return code;
    }

    public bool ValidateCode(string email, string code)
    {
        var key = CacheKey(email);
        if (!_cache.TryGetValue(key, out string? stored))
            return false;

        if (!string.Equals(stored, code, StringComparison.Ordinal))
            return false;

        // One-time use: remove after successful validation
        _cache.Remove(key);
        return true;
    }

    private static string CacheKey(string email) => $"email_code:{email.ToLowerInvariant()}";
}
