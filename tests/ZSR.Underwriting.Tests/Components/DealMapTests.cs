using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class DealMapTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public DealMapTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");
        authCtx.SetClaims(
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id"));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleMaps:ApiKey"] = "test-key"
            })
            .Build();
        _ctx.Services.AddSingleton<IConfiguration>(config);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _ctx.DisposeAsync();
    }

    [Fact]
    public void DealMap_Shows_Empty_State_When_No_Deals()
    {
        _ctx.Services.AddSingleton<IDealService, EmptyMapStubDealService>();
        var cut = _ctx.Render<DealMap>();

        cut.WaitForState(() => cut.Markup.Contains("deal-map-empty"), TimeSpan.FromSeconds(5));

        Assert.Contains("No geocoded deals yet", cut.Markup);
    }

    [Fact]
    public void DealMap_Renders_Map_Container_When_Deals_Exist()
    {
        _ctx.Services.AddSingleton<IDealService, PopulatedMapStubDealService>();
        var cut = _ctx.Render<DealMap>();

        cut.WaitForState(() => cut.Markup.Contains("deal-map-container"), TimeSpan.FromSeconds(5));

        Assert.Contains("deal-map-container", cut.Markup);
    }

    [Fact]
    public void DealMap_Has_Page_Title()
    {
        _ctx.Services.AddSingleton<IDealService, EmptyMapStubDealService>();
        var cut = _ctx.Render<DealMap>();

        Assert.Contains("Deal Map", cut.Markup);
    }

    // --- Stubs ---

    private class EmptyMapStubDealService : IDealService
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

    private class PopulatedMapStubDealService : IDealService
    {
        public Task<Guid> CreateDealAsync(DealInputDto input, string userId) => Task.FromResult(Guid.NewGuid());
        public Task UpdateDealAsync(Guid id, DealInputDto input, string userId) => Task.CompletedTask;
        public Task<DealInputDto?> GetDealAsync(Guid id, string userId) => Task.FromResult<DealInputDto?>(null);
        public Task<IReadOnlyList<DealSummaryDto>> GetAllDealsAsync(string userId)
            => Task.FromResult<IReadOnlyList<DealSummaryDto>>(new List<DealSummaryDto>());
        public Task SetStatusAsync(Guid id, string status, string userId) => Task.CompletedTask;
        public Task DeleteDealAsync(Guid id, string userId) => Task.CompletedTask;
        public Task<IReadOnlyList<DealMapPinDto>> GetDealsForMapAsync(string userId)
            => Task.FromResult<IReadOnlyList<DealMapPinDto>>(new List<DealMapPinDto>
            {
                new() { Id = Guid.NewGuid(), PropertyName = "Test Deal", Address = "NYC",
                         Status = "Draft", Latitude = 40.7, Longitude = -74.0, UnitCount = 10, PurchasePrice = 1000000 }
            });
    }
}
