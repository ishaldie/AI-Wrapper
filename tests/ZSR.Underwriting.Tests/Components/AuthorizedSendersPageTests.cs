using System.Security.Claims;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Web.Components.Pages.Settings;

namespace ZSR.Underwriting.Tests.Components;

public class AuthorizedSendersPageTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;

    public AuthorizedSendersPageTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"AuthSendersPageTests_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");
        authCtx.SetClaims(new Claim(ClaimTypes.NameIdentifier, "test-user-id"));

        _ctx.Services.AddScoped<IAuthorizedSenderService, AuthorizedSenderService>();

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _ctx.DisposeAsync();
    }

    [Fact]
    public void Page_RendersTitle()
    {
        var cut = _ctx.Render<AuthorizedSenders>();
        cut.WaitForState(() => cut.Markup.Contains("Authorized Senders"));

        Assert.Contains("Authorized Senders", cut.Markup);
        Assert.Contains("Manage email addresses", cut.Markup);
    }

    [Fact]
    public void Page_ShowsAddForm()
    {
        var cut = _ctx.Render<AuthorizedSenders>();
        cut.WaitForState(() => cut.Markup.Contains("Add New Sender"));

        Assert.Contains("Add New Sender", cut.Markup);
        Assert.Contains("Email Address", cut.Markup);
        Assert.Contains("Label", cut.Markup);
        Assert.Contains("Add Sender", cut.Markup);
    }

    [Fact]
    public void Page_EmptyState_ShowsInfoMessage()
    {
        var cut = _ctx.Render<AuthorizedSenders>();
        cut.WaitForState(() => cut.Markup.Contains("No authorized senders"));

        Assert.Contains("No authorized senders yet", cut.Markup);
    }

    [Fact]
    public void Page_WithSenders_ShowsTable()
    {
        // Pre-seed a sender
        _db.AuthorizedSenders.Add(new AuthorizedSender("test-user-id", "broker@example.com", "My Broker"));
        _db.SaveChanges();

        var cut = _ctx.Render<AuthorizedSenders>();
        cut.WaitForState(() => cut.Markup.Contains("broker@example.com"), TimeSpan.FromSeconds(3));

        Assert.Contains("broker@example.com", cut.Markup);
        Assert.Contains("My Broker", cut.Markup);
    }

    [Fact]
    public void Page_WithMultipleSenders_ShowsAll()
    {
        _db.AuthorizedSenders.Add(new AuthorizedSender("test-user-id", "a@example.com", "Sender A"));
        _db.AuthorizedSenders.Add(new AuthorizedSender("test-user-id", "b@example.com", "Sender B"));
        _db.SaveChanges();

        var cut = _ctx.Render<AuthorizedSenders>();
        cut.WaitForState(() => cut.Markup.Contains("a@example.com"), TimeSpan.FromSeconds(3));

        Assert.Contains("a@example.com", cut.Markup);
        Assert.Contains("b@example.com", cut.Markup);
        Assert.Contains("Sender A", cut.Markup);
        Assert.Contains("Sender B", cut.Markup);

        // Empty state message should NOT be shown
        Assert.DoesNotContain("No authorized senders yet", cut.Markup);
    }
}
