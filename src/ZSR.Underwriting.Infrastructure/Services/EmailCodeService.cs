using MailKit.Net.Smtp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Infrastructure.Configuration;

namespace ZSR.Underwriting.Infrastructure.Services;

public class EmailCodeService : IEmailCodeService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<EmailCodeService> _logger;
    private readonly SmtpOptions _smtp;
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(10);

    public EmailCodeService(IMemoryCache cache, ILogger<EmailCodeService> logger, IOptions<SmtpOptions> smtpOptions)
    {
        _cache = cache;
        _logger = logger;
        _smtp = smtpOptions.Value;
    }

    public async Task<string> GenerateCodeAsync(string email)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var key = CacheKey(email);
        _cache.Set(key, code, CodeTtl);

        if (!string.IsNullOrWhiteSpace(_smtp.Username) && !string.IsNullOrWhiteSpace(_smtp.Host))
        {
            try
            {
                await SendCodeEmailAsync(email, code);
                _logger.LogInformation("Verification code sent to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification code to {Email}, falling back to console", email);
                _logger.LogInformation("Verification code for {Email}: {Code}", email, code);
            }
        }
        else
        {
            _logger.LogInformation("Verification code for {Email}: {Code}", email, code);
        }

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

    private async Task SendCodeEmailAsync(string toEmail, string code)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your ZSR Underwriting Verification Code";

        message.Body = new TextPart("html")
        {
            Text = $"""
                <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 480px; margin: 0 auto; padding: 2rem;">
                    <h2 style="color: #1A1D23; margin-bottom: 1rem;">Verification Code</h2>
                    <p style="color: #6B7280; font-size: 1rem;">Your ZSR Underwriting verification code is:</p>
                    <div style="background: #F3F4F6; border-radius: 8px; padding: 1.25rem; text-align: center; margin: 1.5rem 0;">
                        <span style="font-size: 2rem; font-weight: 700; letter-spacing: 0.3em; color: #1A1D23;">{code}</span>
                    </div>
                    <p style="color: #9CA3AF; font-size: 0.875rem;">This code expires in 10 minutes. If you didn't request this, you can safely ignore this email.</p>
                </div>
                """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port, _smtp.UseSsl ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.Auto);
        await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static string CacheKey(string email) => $"email_code:{email.ToLowerInvariant()}";
}
