using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Web.Components.Pages.Admin;

namespace ZSR.Underwriting.Tests.Components;

public class AdminTokenDashboardTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;

    public AdminTokenDashboardTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"AdminTokenDashboard_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Admin User");
        authCtx.SetRoles("Admin");

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _ctx.DisposeAsync();
    }

    private RenderFragment RenderDashboard()
    {
        return builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<AdminTokenDashboard>(1);
            builder.CloseComponent();
        };
    }

    [Fact]
    public void Renders_PageTitle()
    {
        var cut = _ctx.Render(RenderDashboard());
        Assert.Contains("Token Usage", cut.Markup);
    }

    [Fact]
    public void Shows_ByokColumn_InUsersTable()
    {
        var cut = _ctx.Render(RenderDashboard());
        Assert.Contains("BYOK", cut.Markup);
    }

    [Fact]
    public async Task Displays_ByokBadge_ForUserWithKey()
    {
        // Seed a user with BYOK key
        var user = new ApplicationUser
        {
            Id = "byok-user-1",
            UserName = "byok@example.com",
            Email = "byok@example.com",
            FullName = "BYOK User",
            EncryptedAnthropicApiKey = "encrypted-key-value"
        };
        _db.Users.Add(user);

        _db.TokenUsageRecords.Add(
            new TokenUsageRecord("byok-user-1", null, OperationType.Chat, 1000, 500, "claude-sonnet-4-5-20250514", isByok: true));
        await _db.SaveChangesAsync();

        var cut = _ctx.Render(RenderDashboard());
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));

        Assert.Contains("Own Key", cut.Markup);
    }

    [Fact]
    public async Task Displays_SharedBadge_ForUserWithoutKey()
    {
        var user = new ApplicationUser
        {
            Id = "shared-user-1",
            UserName = "shared@example.com",
            Email = "shared@example.com",
            FullName = "Shared User"
        };
        _db.Users.Add(user);

        _db.TokenUsageRecords.Add(
            new TokenUsageRecord("shared-user-1", null, OperationType.Chat, 2000, 800, "claude-sonnet-4-5-20250514"));
        await _db.SaveChangesAsync();

        var cut = _ctx.Render(RenderDashboard());
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));

        Assert.Contains("Shared", cut.Markup);
    }

    [Fact]
    public async Task Shows_ByokFilter_Toggle()
    {
        var cut = _ctx.Render(RenderDashboard());
        // The filter should offer BYOK/Shared/All options
        Assert.Contains("Key Type", cut.Markup);
    }

    [Fact]
    public async Task Shows_TokenCounts_InTable()
    {
        var user = new ApplicationUser
        {
            Id = "token-user",
            UserName = "tokens@example.com",
            Email = "tokens@example.com",
            FullName = "Token User"
        };
        _db.Users.Add(user);

        _db.TokenUsageRecords.Add(
            new TokenUsageRecord("token-user", null, OperationType.Chat, 5000, 3000, "claude-sonnet-4-5-20250514"));
        await _db.SaveChangesAsync();

        var cut = _ctx.Render(RenderDashboard());
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));

        // Should display total tokens (8000 = 5000 + 3000)
        Assert.Contains("8,000", cut.Markup);
    }
}
