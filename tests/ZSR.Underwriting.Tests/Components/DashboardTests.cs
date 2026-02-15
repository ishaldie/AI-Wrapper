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

        var markup = cut.Markup;
        Assert.Contains("Dashboard", markup);
    }

    [Fact]
    public void Dashboard_ShowsWelcomeText()
    {
        var cut = _ctx.Render<Dashboard>();

        // Wait for async load to complete
        cut.WaitForState(() => cut.Markup.Contains("Welcome"));

        Assert.Contains("ZSR Underwriting", cut.Markup);
    }

    private class StubDealService : IDealService
    {
        public Task<Guid> CreateDealAsync(DealInputDto input) => Task.FromResult(Guid.NewGuid());
        public Task UpdateDealAsync(Guid id, DealInputDto input) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id) => Task.FromResult<DealInputDto?>(null);
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync()
            => Task.FromResult<IReadOnlyList<DealSummaryDto>>(new List<DealSummaryDto>());
        public Task SetStatusAsync(Guid id, string status) => Task.CompletedTask;
    }
}
