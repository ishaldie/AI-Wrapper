namespace ZSR.Underwriting.Application.Interfaces;

public interface IApiKeyResolver
{
    Task<ApiKeyResolution> ResolveAsync(string? userId);
}

public record ApiKeyResolution(string ApiKey, string? Model, bool IsByok);
