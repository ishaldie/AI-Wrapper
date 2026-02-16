using Bunit;
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
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _ctx.DisposeAsync();
    }

    [Fact]
    public void Dashboard_RendersPageHeading()
    {
        var cut = _ctx.Render<Dashboard>();
        Assert.Contains("Dashboard", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsWelcomeText()
    {
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.Markup.Contains("Welcome"));
        Assert.Contains("ZSR Underwriting", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsTotalDealsCount()
    {
        _ctx.Services.AddSingleton<IDealService>(new StubDealServiceWithData());
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));
        Assert.Contains("4", cut.Markup);
        Assert.Contains("Total Deals", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsActiveDealsCount()
    {
        _ctx.Services.AddSingleton<IDealService>(new StubDealServiceWithData());
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));
        Assert.Contains("Active", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsCompletedDealsCount()
    {
        _ctx.Services.AddSingleton<IDealService>(new StubDealServiceWithData());
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));
        Assert.Contains("Completed", cut.Markup);
    }

    [Fact]
    public void Dashboard_EmptyState_ShowsZeroCounts()
    {
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.Markup.Contains("Total Deals"));
        Assert.Contains("0", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsRecentActivityHeading()
    {
        _ctx.Services.AddSingleton<IDealService>(new StubDealServiceWithData());
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));
        Assert.Contains("Recent Activity", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsRecentDealNames()
    {
        _ctx.Services.AddSingleton<IDealService>(new StubDealServiceWithData());
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => !cut.Markup.Contains("mud-progress"));
        Assert.Contains("Sunset Apts", cut.Markup);
        Assert.Contains("Oak Manor", cut.Markup);
    }

    [Fact]
    public void Dashboard_EmptyState_ShowsNoRecentActivity()
    {
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.Markup.Contains("Total Deals"));
        Assert.Contains("No deals yet", cut.Markup);
    }

    [Fact]
    public void Dashboard_HasNewDealButton()
    {
        var cut = _ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.Markup.Contains("Total Deals"));
        Assert.Contains("New Deal", cut.Markup);
        Assert.Contains("deals/new", cut.Markup);
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
