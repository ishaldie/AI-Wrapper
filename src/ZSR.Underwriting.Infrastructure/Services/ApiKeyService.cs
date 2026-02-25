using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDataProtector _protector;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string ProtectorPurpose = "AnthropicApiKey";

    public ApiKeyService(
        UserManager<ApplicationUser> userManager,
        IDataProtectionProvider dataProtectionProvider,
        IHttpClientFactory httpClientFactory)
    {
        _userManager = userManager;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _httpClientFactory = httpClientFactory;
    }

    public async Task SaveKeyAsync(string userId, string apiKey, string? model = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        user.EncryptedAnthropicApiKey = _protector.Protect(apiKey);
        user.PreferredModel = model;

        await _userManager.UpdateAsync(user);
    }

    public async Task<(string ApiKey, string? Model)?> GetDecryptedKeyAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user?.EncryptedAnthropicApiKey is null)
            return null;

        var decrypted = _protector.Unprotect(user.EncryptedAnthropicApiKey);
        return (decrypted, user.PreferredModel);
    }

    public async Task RemoveKeyAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        user.EncryptedAnthropicApiKey = null;
        user.PreferredModel = null;

        await _userManager.UpdateAsync(user);
    }

    public async Task<bool> HasKeyAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.EncryptedAnthropicApiKey is not null;
    }

    public async Task<(bool Success, string? ErrorMessage)> ValidateKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, "API key cannot be empty.");

        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = JsonSerializer.Serialize(new
            {
                model = "claude-haiku-4-5-20251001",
                max_tokens = 1,
                messages = new[] { new { role = "user", content = "Hi" } }
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var body = await response.Content.ReadAsStringAsync();
            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => (false, "Invalid API key."),
                System.Net.HttpStatusCode.Forbidden => (false, "API key does not have permission."),
                _ => (false, $"Validation failed: {response.StatusCode}")
            };
        }
        catch (Exception ex)
        {
            return (false, $"Connection failed: {ex.Message}");
        }
    }
}
