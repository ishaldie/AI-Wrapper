using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class DashboardTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public DashboardTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton<IDealService, StubDealService>();
        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _ctx.DisposeAsync();
    }

    [Fact]
    public void Dashboard_RendersWelcomeHeading()
    {
        var cut = _ctx.Render<Dashboard>();
        Assert.Contains("Welcome back", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsStartNewAnalysis()
    {
        var cut = _ctx.Render<Dashboard>();
        Assert.Contains("Start a new analysis", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsRecommendedSection()
    {
        var cut = _ctx.Render<Dashboard>();
        Assert.Contains("Recommended for you", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsYourAnalysesSection()
    {
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));
        Assert.Contains("Your analyses", cut.Markup);
    }

    [Fact]
    public void Dashboard_EmptyState_ShowsNoAnalysesMessage()
    {
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));
        Assert.Contains("No analyses yet", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_WithDeals_ShowsDealNames()
    {
        // Separate BunitContext since we need a different IDealService registration
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IDealService>(new StubDealServiceWithData());
        var authCtx = ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");

        try
        {
            // MudDataGrid with MudChip requires MudPopoverProvider in the render tree
            ctx.Render<MudPopoverProvider>();
            var dashboard = ctx.Render<Dashboard>();
            dashboard.WaitForState(() => !dashboard.Markup.Contains("mud-progress"));
            Assert.Contains("Sunset Apts", dashboard.Markup);
            Assert.Contains("Oak Manor", dashboard.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public void Dashboard_ShowsSearchBar()
    {
        var cut = _ctx.Render<Dashboard>();
        Assert.Contains("search-bar", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsViewAllLink()
    {
        var cut = _ctx.Render<Dashboard>();
        Assert.Contains("View All", cut.Markup);
    }

    private class StubDealService : IDealService
    {
        public Task<Guid> CreateDealAsync(DealInputDto input) => Task.FromResult(Guid.NewGuid());
        public Task UpdateDealAsync(Guid id, DealInputDto input) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id) => Task.FromResult<DealInputDto?>(null);
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync()
            => Task.FromResult<IReadOnlyList<DealSummaryDto>>(new List<DealSummaryDto>());
        public Task SetStatusAsync(Guid id, string status) => Task.CompletedTask;
        public Task DeleteDealAsync(Guid id) => Task.CompletedTask;
    }

    private class StubDealServiceWithData : IDealService
    {
        public Task<Guid> CreateDealAsync(DealInputDto input) => Task.FromResult(Guid.NewGuid());
        public Task UpdateDealAsync(Guid id, DealInputDto input) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id) => Task.FromResult<DealInputDto?>(null);
        public Task DeleteDealAsync(Guid id) => Task.CompletedTask;
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync()
        {
            var deals = new List<DealSummaryDto>
            {
                new() { Id = Guid.NewGuid(), PropertyName = "Sunset Apts", Status = "Draft", PurchasePrice = 5_000_000m, UpdatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), PropertyName = "Oak Manor", Status = "InProgress", PurchasePrice = 3_000_000m, UpdatedAt = DateTime.UtcNow.AddHours(-1) },
                new() { Id = Guid.NewGuid(), PropertyName = "Pine Ridge", Status = "Complete", PurchasePrice = 7_000_000m, UpdatedAt = DateTime.UtcNow.AddHours(-2) },
                new() { Id = Guid.NewGuid(), PropertyName = "Elm Court", Status = "InProgress", PurchasePrice = 4_000_000m, UpdatedAt = DateTime.UtcNow.AddHours(-3) },
            };
            return Task.FromResult<IReadOnlyList<DealSummaryDto>>(deals);
        }
        public Task SetStatusAsync(Guid id, string status) => Task.CompletedTask;
    }
}
