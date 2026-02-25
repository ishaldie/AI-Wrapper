namespace ZSR.Underwriting.Application.Interfaces;

public interface IApiKeyService
{
    Task SaveKeyAsync(string userId, string apiKey, string? model = null);
    Task<(string ApiKey, string? Model)?> GetDecryptedKeyAsync(string userId);
    Task RemoveKeyAsync(string userId);
    Task<bool> HasKeyAsync(string userId);
    Task<(bool Success, string? ErrorMessage)> ValidateKeyAsync(string apiKey);
}
