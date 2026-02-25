using Microsoft.Extensions.Options;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class ApiKeyResolverTests
{
    private static readonly ClaudeOptions SharedOptions = new()
    {
        ApiKey = "shared-platform-key",
        Model = "claude-opus-4-6-20250918"
    };

    [Fact]
    public async Task Resolve_WithByokKey_ReturnsUserKey()
    {
        var apiKeyService = new StubApiKeyService("sk-ant-api03-user-key", "claude-sonnet-4-5-20250514");
        var resolver = new ApiKeyResolver(apiKeyService, Options.Create(SharedOptions));

        var result = await resolver.ResolveAsync("user-123");

        Assert.Equal("sk-ant-api03-user-key", result.ApiKey);
        Assert.Equal("claude-sonnet-4-5-20250514", result.Model);
        Assert.True(result.IsByok);
    }

    [Fact]
    public async Task Resolve_WithByokKeyNoModel_ReturnsUserKeyWithNullModel()
    {
        var apiKeyService = new StubApiKeyService("sk-ant-api03-user-key", null);
        var resolver = new ApiKeyResolver(apiKeyService, Options.Create(SharedOptions));

        var result = await resolver.ResolveAsync("user-123");

        Assert.Equal("sk-ant-api03-user-key", result.ApiKey);
        Assert.Null(result.Model);
        Assert.True(result.IsByok);
    }

    [Fact]
    public async Task Resolve_WithoutByokKey_FallsBackToShared()
    {
        var apiKeyService = new StubApiKeyService(null, null);
        var resolver = new ApiKeyResolver(apiKeyService, Options.Create(SharedOptions));

        var result = await resolver.ResolveAsync("user-123");

        Assert.Equal("shared-platform-key", result.ApiKey);
        Assert.Null(result.Model);
        Assert.False(result.IsByok);
    }

    [Fact]
    public async Task Resolve_WithNullUserId_FallsBackToShared()
    {
        var apiKeyService = new StubApiKeyService("sk-ant-api03-user-key", null);
        var resolver = new ApiKeyResolver(apiKeyService, Options.Create(SharedOptions));

        var result = await resolver.ResolveAsync(null);

        Assert.Equal("shared-platform-key", result.ApiKey);
        Assert.Null(result.Model);
        Assert.False(result.IsByok);
    }

    private class StubApiKeyService : IApiKeyService
    {
        private readonly string? _apiKey;
        private readonly string? _model;

        public StubApiKeyService(string? apiKey, string? model)
        {
            _apiKey = apiKey;
            _model = model;
        }

        public Task SaveKeyAsync(string userId, string apiKey, string? model = null) =>
            Task.CompletedTask;

        public Task<(string ApiKey, string? Model)?> GetDecryptedKeyAsync(string userId) =>
            _apiKey is null
                ? Task.FromResult<(string, string?)?>(null)
                : Task.FromResult<(string, string?)?>((ApiKey: _apiKey, Model: _model));

        public Task RemoveKeyAsync(string userId) => Task.CompletedTask;
        public Task<bool> HasKeyAsync(string userId) => Task.FromResult(_apiKey is not null);
        public Task<(bool Success, string? ErrorMessage)> ValidateKeyAsync(string apiKey) =>
            Task.FromResult<(bool, string?)>((true, null));
    }
}
