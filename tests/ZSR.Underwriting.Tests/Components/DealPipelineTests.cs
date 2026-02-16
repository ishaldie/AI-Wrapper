using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class DealPipelineTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public DealPipelineTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton<IDealService>(new PipelineStubDealService());
        _ctx.Services.AddSingleton<IActivityTracker, NoOpActivityTracker>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _ctx.DisposeAsync();
    }

    private RenderFragment WrapWithProviders<TComponent>() where TComponent : IComponent
    {
        return builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TComponent>(1);
            builder.CloseComponent();
        };
    }

    [Fact]
    public void Pipeline_RendersPageHeading()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Deal Pipeline"));
        Assert.Contains("Deal Pipeline", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsDealPropertyNames()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Sunset Apts"));
        Assert.Contains("Sunset Apts", cut.Markup);
        Assert.Contains("Oak Manor", cut.Markup);
        Assert.Contains("Pine Ridge", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsDealAddresses()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("123 Main St"));
        Assert.Contains("123 Main St", cut.Markup);
        Assert.Contains("456 Oak Ave", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsStatusChips()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Draft"));
        Assert.Contains("Draft", cut.Markup);
        Assert.Contains("InProgress", cut.Markup);
        Assert.Contains("Complete", cut.Markup);
    }

    [Fact]
    public void Pipeline_HasSearchField()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Deal Pipeline"));
        Assert.Contains("Search", cut.Markup);
    }

    [Fact]
    public void Pipeline_HasStatusFilterChips()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Deal Pipeline"));
        Assert.Contains("Archived", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsCapRateColumn()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("Cap Rate"));
        Assert.Contains("Cap Rate", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsIrrColumn()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("IRR"));
        Assert.Contains("IRR", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsCapRateValues()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("6.5"));
        Assert.Contains("6.5", cut.Markup);
    }

    [Fact]
    public void Pipeline_ShowsIrrValues()
    {
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("14.2"));
        Assert.Contains("14.2", cut.Markup);
    }

    [Fact]
    public void Pipeline_EmptyState_ShowsNoDealMessage()
    {
        _ctx.Services.AddSingleton<IDealService>(new EmptyPipelineStubDealService());
        var cut = _ctx.Render(WrapWithProviders<DealPipeline>());
        cut.WaitForState(() => cut.Markup.Contains("No deals"));
        Assert.Contains("No deals", cut.Markup);
    }

    private class NoOpActivityTracker : IActivityTracker
    {
        public Task<Guid> StartSessionAsync(string userId) => Task.FromResult(Guid.NewGuid());
        public Task TrackPageViewAsync(string pageUrl) => Task.CompletedTask;
        public Task TrackEventAsync(ActivityEventType eventType, Guid? dealId = null, string? metadata = null) => Task.CompletedTask;
    }

    private class PipelineStubDealService : IDealService
    {
        public Task<Guid> CreateDealAsync(DealInputDto input) => Task.FromResult(Guid.NewGuid());
        public Task UpdateDealAsync(Guid id, DealInputDto input) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id) => Task.FromResult<DealInputDto?>(null);
        public Task SetStatusAsync(Guid id, string status) => Task.CompletedTask;
        public Task DeleteDealAsync(Guid id) => Task.CompletedTask;
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync()
        {
            var deals = new List<DealSummaryDto>
            {
                new() { Id = Guid.NewGuid(), PropertyName = "Sunset Apts", Address = "123 Main St", Status = "Draft", PurchasePrice = 5_000_000m, CapRate = 0.065m, Irr = 0.142m, UpdatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), PropertyName = "Oak Manor", Address = "456 Oak Ave", Status = "InProgress", PurchasePrice = 3_000_000m, CapRate = 0.072m, Irr = 0.118m, UpdatedAt = DateTime.UtcNow.AddHours(-1) },
                new() { Id = Guid.NewGuid(), PropertyName = "Pine Ridge", Address = "789 Pine Rd", Status = "Complete", PurchasePrice = 7_000_000m, CapRate = 0.058m, Irr = 0.165m, UpdatedAt = DateTime.UtcNow.AddHours(-2) },
                new() { Id = Guid.NewGuid(), PropertyName = "Elm Court", Address = "321 Elm Blvd", Status = "Archived", PurchasePrice = 4_000_000m, UpdatedAt = DateTime.UtcNow.AddHours(-3) },
            };
            return Task.FromResult<IReadOnlyList<DealSummaryDto>>(deals);
        }
    }

    private class EmptyPipelineStubDealService : IDealService
    {
        public Task<Guid> CreateDealAsync(DealInputDto input) => Task.FromResult(Guid.NewGuid());
        public Task UpdateDealAsync(Guid id, DealInputDto input) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id) => Task.FromResult<DealInputDto?>(null);
        public Task SetStatusAsync(Guid id, string status) => Task.CompletedTask;
        public Task DeleteDealAsync(Guid id) => Task.CompletedTask;
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync()
            => Task.FromResult<IReadOnlyList<DealSummaryDto>>(new List<DealSummaryDto>());
    }
}
