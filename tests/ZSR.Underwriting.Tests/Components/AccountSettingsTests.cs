using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class AccountSettingsTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public AccountSettingsTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddLogging();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _ctx.DisposeAsync();

    private void SetupAuth()
    {
        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("testuser@example.com");
        authCtx.SetClaims(new System.Security.Claims.Claim(
            System.Security.Claims.ClaimTypes.NameIdentifier, "user-123"));
    }

    [Fact]
    public void Renders_AiConfigurationSection()
    {
        SetupAuth();
        _ctx.Services.AddSingleton<IApiKeyService>(new FakeApiKeyService());
        _ctx.Render<MudPopoverProvider>();

        var cut = _ctx.Render<AccountSettings>();

        Assert.Contains("AI Configuration", cut.Markup);
        Assert.Contains("Anthropic API Key", cut.Markup);
    }

    [Fact]
    public void ShowsMaskedKey_WhenKeyExists()
    {
        SetupAuth();
        _ctx.Services.AddSingleton<IApiKeyService>(
            new FakeApiKeyService(hasKey: true, apiKey: "sk-ant-api03-abcdefghij1234"));
        _ctx.Render<MudPopoverProvider>();

        var cut = _ctx.Render<AccountSettings>();

        Assert.Contains("sk-ant-", cut.Markup);
        Assert.Contains("1234", cut.Markup);
        Assert.Contains("****", cut.Markup);
        Assert.Contains("Remove Key", cut.Markup);
    }

    [Fact]
    public void HidesRemoveButton_WhenNoKey()
    {
        SetupAuth();
        _ctx.Services.AddSingleton<IApiKeyService>(new FakeApiKeyService());
        _ctx.Render<MudPopoverProvider>();

        var cut = _ctx.Render<AccountSettings>();

        Assert.DoesNotContain("Remove Key", cut.Markup);
    }

    private class FakeApiKeyService : IApiKeyService
    {
        private readonly bool _hasKey;
        private readonly string? _apiKey;
        private readonly string? _model;

        public FakeApiKeyService(bool hasKey = false, string? apiKey = null, string? model = null)
        {
            _hasKey = hasKey;
            _apiKey = apiKey;
            _model = model;
        }

        public Task SaveKeyAsync(string userId, string apiKey, string? model = null) =>
            Task.CompletedTask;

        public Task<(string ApiKey, string? Model)?> GetDecryptedKeyAsync(string userId) =>
            _hasKey && _apiKey is not null
                ? Task.FromResult<(string, string?)?>((ApiKey: _apiKey, Model: _model))
                : Task.FromResult<(string, string?)?>(null);

        public Task RemoveKeyAsync(string userId) => Task.CompletedTask;
        public Task<bool> HasKeyAsync(string userId) => Task.FromResult(_hasKey);
        public Task<(bool Success, string? ErrorMessage)> ValidateKeyAsync(string apiKey) =>
            Task.FromResult<(bool, string?)>((true, null));
    }
}
