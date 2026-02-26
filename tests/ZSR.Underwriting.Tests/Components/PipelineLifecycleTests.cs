using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class PipelineLifecycleTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public PipelineLifecycleTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton<IDealService>(new FullLifecycleStubDealService());
        _ctx.Services.AddSingleton<IActivityTracker, NoOpActivityTracker>();
        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _ctx.DisposeAsync();

    private RenderFragment WrapWithProviders<TComponent>() where TComponent : IComponent => builder =>
    {
        builder.OpenComponent<MudPopoverProvider>(0);
        builder.CloseComponent();
        builder.OpenComponent<TComponent>(1);
        builder.CloseComponent();
    };

    [Fact]
    public void Pipeline_ShowsNewStatusFilterChips()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Deal Pipeline"));

        Assert.Contains("Screening", cut.Markup);
        Assert.Contains("Under Contract", cut.Markup);
        Assert.Contains("Closed", cut.Markup);
        Assert.Contains("Active", cut.Markup);
        Assert.Contains("Disposition", cut.Markup);
        Assert.Contains("Sold", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsPhaseColumn()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Deal Pipeline"));

        Assert.Contains("Phase", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsAllLifecycleDeals()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Deal Pipeline"));

        Assert.Contains("Draft Deal", cut.Markup);
        Assert.Contains("Active Property", cut.Markup);
        Assert.Contains("Exiting Asset", cut.Markup);
    }

    [Fact]
    public void Pipeline_DisplaysScreeningForInProgress()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Deal Pipeline"));

        // InProgress should display as "Screening"
        Assert.Contains("Screening", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsPhaseGroupingLabel()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Deal Pipeline"));

        Assert.Contains("ACQUISITION", cut.Markup);
    }
}

internal class FullLifecycleStubDealService : IDealService
{
    public Task<Guid> CreateDealAsync(DealInputDto input, string userId) => Task.FromResult(Guid.NewGuid());
    public Task UpdateDealAsync(Guid id, DealInputDto input, string userId) => Task.CompletedTask;
    public Task<DealInputDto?> GetDealAsync(Guid id, string userId) => Task.FromResult<DealInputDto?>(null);
    public Task SetStatusAsync(Guid id, string status, string userId) => Task.CompletedTask;
    public Task DeleteDealAsync(Guid id, string userId) => Task.CompletedTask;
    public Task<IReadOnlyList<DealMapPinDto>> GetDealsForMapAsync(string userId)
        => Task.FromResult<IReadOnlyList<DealMapPinDto>>(new List<DealMapPinDto>());
    public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync(string userId)
    {
        var deals = new List<DealSummaryDto>
        {
            new() { Id = Guid.NewGuid(), PropertyName = "Draft Deal", Address = "1 Draft St", Status = "Draft", Phase = "Acquisition", PurchasePrice = 1_000_000m, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PropertyName = "Screening Deal", Address = "2 Screen Rd", Status = "InProgress", Phase = "Acquisition", PurchasePrice = 2_000_000m, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PropertyName = "Active Property", Address = "3 Active Blvd", Status = "Active", Phase = "Ownership", PurchasePrice = 5_000_000m, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PropertyName = "Exiting Asset", Address = "4 Exit Ln", Status = "Disposition", Phase = "Exit", PurchasePrice = 6_000_000m, UpdatedAt = DateTime.UtcNow },
        };
        return Task.FromResult<IReadOnlyList<DealSummaryDto>>(deals);
    }
}
