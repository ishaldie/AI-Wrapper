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

public class DealWizardTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public DealWizardTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton<IDealService>(new FakeDealService());
        _ctx.Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _ctx.DisposeAsync();
    }

    [Fact]
    public void DealWizard_RendersRequiredInputsText()
    {
        var cut = _ctx.Render<DealWizard>();

        var markup = cut.Markup;
        Assert.Contains("Required Inputs", markup);
    }

    [Fact]
    public void DealWizard_ShowsThreeSteps()
    {
        var cut = _ctx.Render<DealWizard>();

        var steps = cut.FindComponents<MudStep>();
        Assert.Equal(3, steps.Count);
    }

    [Fact]
    public void DealWizard_HasPropertyNameField()
    {
        var cut = _ctx.Render<DealWizard>();

        var markup = cut.Markup;
        Assert.Contains("Property Name", markup);
    }

    [Fact]
    public void DealWizard_InjectsDealService()
    {
        // DealWizard should render without error when IDealService is registered
        var cut = _ctx.Render<DealWizard>();
        Assert.NotNull(cut);
    }

    private class FakeDealService : IDealService
    {
        public Guid LastCreatedId { get; private set; }
        public DealInputDto? LastInput { get; private set; }
        public bool CreateWasCalled { get; private set; }

        public Task<Guid> CreateDealAsync(DealInputDto input)
        {
            CreateWasCalled = true;
            LastInput = input;
            LastCreatedId = Guid.NewGuid();
            return Task.FromResult(LastCreatedId);
        }

        public Task UpdateDealAsync(Guid id, DealInputDto input) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id) => Task.FromResult<DealInputDto?>(null);
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync() =>
            Task.FromResult<IReadOnlyList<DealSummaryDto>>(Array.Empty<DealSummaryDto>());
        public Task SetStatusAsync(Guid id, string status) => Task.CompletedTask;
        public Task DeleteDealAsync(Guid id) => Task.CompletedTask;
    }

    private class FakeNavigationManager : NavigationManager
    {
        public string? LastNavigatedUri { get; private set; }

        public FakeNavigationManager()
        {
            Initialize("https://localhost:5001/", "https://localhost:5001/deals/new");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            LastNavigatedUri = uri;
        }
    }
}
