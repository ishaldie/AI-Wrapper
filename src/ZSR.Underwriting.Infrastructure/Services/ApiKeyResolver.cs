using Microsoft.Extensions.Options;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Infrastructure.Configuration;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ApiKeyResolver : IApiKeyResolver
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ClaudeOptions _options;

    public ApiKeyResolver(
        IApiKeyService apiKeyService,
        IOptions<ClaudeOptions> options)
    {
        _apiKeyService = apiKeyService;
        _options = options.Value;
    }

    public async Task<ApiKeyResolution> ResolveAsync(string? userId)
    {
        if (userId is not null)
        {
            var byokKey = await _apiKeyService.GetDecryptedKeyAsync(userId);
            if (byokKey.HasValue)
            {
                return new ApiKeyResolution(
                    byokKey.Value.ApiKey,
                    byokKey.Value.Model,
                    IsByok: true);
            }
        }

        return new ApiKeyResolution(
            _options.ResolvedApiKey,
            Model: null,
            IsByok: false);
    }
}
