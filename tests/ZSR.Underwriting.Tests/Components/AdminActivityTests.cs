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

public class AdminActivityTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;

    public AdminActivityTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"AdminActivityTests_{Guid.NewGuid()}";
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

    private RenderFragment RenderAdminActivity()
    {
        return builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<AdminActivity>(1);
            builder.CloseComponent();
        };
    }

    [Fact]
    public void AdminActivity_RendersPageTitle()
    {
        var cut = _ctx.Render(RenderAdminActivity());
        Assert.Contains("Activity Log", cut.Markup);
    }

    [Fact]
    public void AdminActivity_ShowsSessionsTable()
    {
        var cut = _ctx.Render(RenderAdminActivity());
        Assert.Contains("Recent Sessions", cut.Markup);
    }

    [Fact]
    public void AdminActivity_ShowsEventLogTable()
    {
        var cut = _ctx.Render(RenderAdminActivity());
        Assert.Contains("Event Log", cut.Markup);
    }

    [Fact]
    public void AdminActivity_ShowsFilterControls()
    {
        var cut = _ctx.Render(RenderAdminActivity());
        var markup = cut.Markup;
        Assert.Contains("User ID", markup);
        Assert.Contains("Event Type", markup);
        Assert.Contains("Date Range", markup);
        Assert.Contains("Deal ID", markup);
    }

    [Fact]
    public async Task AdminActivity_DisplaysSeededSession()
    {
        var session = new UserSession("test-user-123");
        _db.UserSessions.Add(session);
        var evt = new ActivityEvent(session.Id, "test-user-123", ActivityEventType.SessionStart);
        _db.ActivityEvents.Add(evt);
        await _db.SaveChangesAsync();

        var cut = _ctx.Render(RenderAdminActivity());
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));

        Assert.Contains("test-user-123", cut.Markup);
    }
}
