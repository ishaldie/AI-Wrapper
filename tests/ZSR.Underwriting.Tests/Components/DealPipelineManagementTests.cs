using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class DealPipelineManagementTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public DealPipelineManagementTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton<IDealService>(new ManagementStubDealService());
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _ctx.DisposeAsync();

    private RenderFragment WrapPipeline()
    {
        return builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<DealPipeline>(1);
            builder.CloseComponent();
        };
    }

    [Fact]
    public void Pipeline_HasCompareButton()
    {
        var cut = _ctx.Render(WrapPipeline());
        cut.WaitForState(() => cut.Markup.Contains("Sunset Apts"));
        Assert.Contains("Compare", cut.Markup);
    }

    [Fact]
    public void Pipeline_HasSelectionCheckboxes()
    {
        var cut = _ctx.Render(WrapPipeline());
        cut.WaitForState(() => cut.Markup.Contains("Sunset Apts"));
        // MudDataGrid with MultiSelection renders checkboxes
        Assert.Contains("checkbox", cut.Markup.ToLower());
    }

    [Fact]
    public void Pipeline_HasArchiveAction()
    {
        var cut = _ctx.Render(WrapPipeline());
        cut.WaitForState(() => cut.Markup.Contains("Sunset Apts"));
        Assert.Contains("Archive", cut.Markup);
    }

    [Fact]
    public void Pipeline_HasDeleteAction()
    {
        var cut = _ctx.Render(WrapPipeline());
        cut.WaitForState(() => cut.Markup.Contains("Sunset Apts"));
        Assert.Contains("Delete", cut.Markup);
    }

    private class ManagementStubDealService : IDealService
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
                new() { Id = Guid.NewGuid(), PropertyName = "Oak Manor", Address = "456 Oak Ave", Status = "InProgress", PurchasePrice = 3_000_000m, UpdatedAt = DateTime.UtcNow.AddHours(-1) },
            };
            return Task.FromResult<IReadOnlyList<DealSummaryDto>>(deals);
        }
    }
}
