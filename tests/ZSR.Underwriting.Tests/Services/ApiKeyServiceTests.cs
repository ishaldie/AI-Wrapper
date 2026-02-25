using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class ApiKeyServiceTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private IApiKeyService _sut = null!;
    private string _userId = null!;

    public async Task InitializeAsync()
    {
        var dbName = $"ApiKeyTests_{Guid.NewGuid()}";
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(o =>
            o.UseInMemoryDatabase(dbName));

        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddDataProtection()
            .SetApplicationName("ZSR.Underwriting.Tests");

        services.AddLogging();
        services.AddHttpClient();
        services.AddScoped<IApiKeyService, ApiKeyService>();

        _provider = services.BuildServiceProvider();
        _userManager = _provider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FullName = "Test User"
        };
        await _userManager.CreateAsync(user, "Test123!");
        _userId = user.Id;
        _sut = _provider.GetRequiredService<IApiKeyService>();
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveAndRetrieve_RoundTripsCorrectly()
    {
        var apiKey = "sk-ant-api03-test-key-12345";
        await _sut.SaveKeyAsync(_userId, apiKey);

        var result = await _sut.GetDecryptedKeyAsync(_userId);

        Assert.NotNull(result);
        Assert.Equal(apiKey, result!.Value.ApiKey);
        Assert.Null(result.Value.Model);
    }

    [Fact]
    public async Task SaveWithModel_RoundTripsCorrectly()
    {
        var apiKey = "sk-ant-api03-test-key-67890";
        var model = "claude-sonnet-4-5-20250514";
        await _sut.SaveKeyAsync(_userId, apiKey, model);

        var result = await _sut.GetDecryptedKeyAsync(_userId);

        Assert.NotNull(result);
        Assert.Equal(apiKey, result!.Value.ApiKey);
        Assert.Equal(model, result.Value.Model);
    }

    [Fact]
    public async Task EncryptedKey_IsNotPlaintext()
    {
        var apiKey = "sk-ant-api03-plaintext-check";
        await _sut.SaveKeyAsync(_userId, apiKey);

        // Read encrypted value directly from DB
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FindAsync(_userId);

        Assert.NotNull(user!.EncryptedAnthropicApiKey);
        Assert.NotEqual(apiKey, user.EncryptedAnthropicApiKey);
    }

    [Fact]
    public async Task HasKeyAsync_ReturnsTrueWhenKeyExists()
    {
        await _sut.SaveKeyAsync(_userId, "sk-ant-api03-has-key");

        Assert.True(await _sut.HasKeyAsync(_userId));
    }

    [Fact]
    public async Task HasKeyAsync_ReturnsFalseWhenNoKey()
    {
        Assert.False(await _sut.HasKeyAsync(_userId));
    }

    [Fact]
    public async Task RemoveKeyAsync_ClearsKey()
    {
        await _sut.SaveKeyAsync(_userId, "sk-ant-api03-to-remove");
        Assert.True(await _sut.HasKeyAsync(_userId));

        await _sut.RemoveKeyAsync(_userId);

        Assert.False(await _sut.HasKeyAsync(_userId));
        Assert.Null(await _sut.GetDecryptedKeyAsync(_userId));
    }

    [Fact]
    public async Task GetDecryptedKeyAsync_ReturnsNullWhenNoKey()
    {
        var result = await _sut.GetDecryptedKeyAsync(_userId);
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveKeyAsync_ThrowsOnNullApiKey()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SaveKeyAsync(_userId, null!));
    }

    [Fact]
    public async Task SaveKeyAsync_ThrowsOnEmptyApiKey()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SaveKeyAsync(_userId, ""));
    }

    [Fact]
    public async Task SaveKeyAsync_ThrowsOnWhitespaceApiKey()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SaveKeyAsync(_userId, "   "));
    }

    [Fact]
    public async Task SaveKeyAsync_OverwritesPreviousKey()
    {
        await _sut.SaveKeyAsync(_userId, "sk-ant-api03-first");
        await _sut.SaveKeyAsync(_userId, "sk-ant-api03-second", "claude-haiku-4-5-20251001");

        var result = await _sut.GetDecryptedKeyAsync(_userId);
        Assert.Equal("sk-ant-api03-second", result!.Value.ApiKey);
        Assert.Equal("claude-haiku-4-5-20251001", result.Value.Model);
    }
}
