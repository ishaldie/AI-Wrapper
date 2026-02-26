using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;
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
        _ctx.Services.AddSingleton<IQuickAnalysisService, StubQuickAnalysisService>();
        _ctx.Services.AddSingleton<IActivityTracker, NoOpActivityTracker>();
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
        ctx.Services.AddSingleton<IQuickAnalysisService, StubQuickAnalysisService>();
        ctx.Services.AddSingleton<IActivityTracker, NoOpActivityTracker>();
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

    private class NoOpActivityTracker : IActivityTracker
    {
        public Task<Guid> StartSessionAsync(string userId) => Task.FromResult(Guid.NewGuid());
        public Task TrackPageViewAsync(string pageUrl) => Task.CompletedTask;
        public Task TrackEventAsync(ActivityEventType eventType, Guid? dealId = null, string? metadata = null) => Task.CompletedTask;
    }

    private class StubQuickAnalysisService : IQuickAnalysisService
    {
        public Task<QuickAnalysisProgress> StartAnalysisAsync(string searchQuery, string userId, CancellationToken ct = default, IActivityTracker? activityTracker = null)
            => Task.FromResult(new QuickAnalysisProgress { DealId = Guid.NewGuid(), SearchQuery = searchQuery });
    }

    private class StubDealService : IDealService
    {
        public Task<Guid> CreateDealAsync(DealInputDto input, string userId) => Task.FromResult(Guid.NewGuid());
        public Task UpdateDealAsync(Guid id, DealInputDto input, string userId) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id, string userId) => Task.FromResult<DealInputDto?>(null);
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync(string userId)
            => Task.FromResult<IReadOnlyList<DealSummaryDto>>(new List<DealSummaryDto>());
        public Task SetStatusAsync(Guid id, string status, string userId) => Task.CompletedTask;
        public Task DeleteDealAsync(Guid id, string userId) => Task.CompletedTask;
        public Task<IReadOnlyList<DealMapPinDto>> GetDealsForMapAsync(string userId)
            => Task.FromResult<IReadOnlyList<DealMapPinDto>>(new List<DealMapPinDto>());
    }

    private class StubDealServiceWithData : IDealService
    {
        public Task<Guid> CreateDealAsync(DealInputDto input, string userId) => Task.FromResult(Guid.NewGuid());
        public Task UpdateDealAsync(Guid id, DealInputDto input, string userId) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id, string userId) => Task.FromResult<DealInputDto?>(null);
        public Task DeleteDealAsync(Guid id, string userId) => Task.CompletedTask;
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync(string userId)
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
        public Task SetStatusAsync(Guid id, string status, string userId) => Task.CompletedTask;
        public Task<IReadOnlyList<DealMapPinDto>> GetDealsForMapAsync(string userId)
            => Task.FromResult<IReadOnlyList<DealMapPinDto>>(new List<DealMapPinDto>());
    }
}
