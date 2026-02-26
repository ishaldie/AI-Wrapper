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

public class DealComparisonTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private static readonly Guid Deal1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Deal2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public DealComparisonTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
        _ctx.Services.AddSingleton<IDealService>(new ComparisonStubDealService());
        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _ctx.DisposeAsync();

    private RenderFragment WrapComparison(string ids)
    {
        return builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<DealComparison>(1);
            builder.AddAttribute(2, nameof(DealComparison.Ids), ids);
            builder.CloseComponent();
        };
    }

    [Fact]
    public void Comparison_RendersPageHeading()
    {
        var cut = _ctx.Render(WrapComparison($"{Deal1Id},{Deal2Id}"));
        cut.WaitForState(() => cut.Markup.Contains("Deal Comparison"));
        Assert.Contains("Deal Comparison", cut.Markup);
    }

    [Fact]
    public void Comparison_ShowsBothDealNames()
    {
        var cut = _ctx.Render(WrapComparison($"{Deal1Id},{Deal2Id}"));
        cut.WaitForState(() => cut.Markup.Contains("Sunset Apts"));
        Assert.Contains("Sunset Apts", cut.Markup);
        Assert.Contains("Oak Manor", cut.Markup);
    }

    [Fact]
    public void Comparison_ShowsPurchasePrices()
    {
        var cut = _ctx.Render(WrapComparison($"{Deal1Id},{Deal2Id}"));
        cut.WaitForState(() => cut.Markup.Contains("5,000,000"));
        Assert.Contains("5,000,000", cut.Markup);
        Assert.Contains("3,000,000", cut.Markup);
    }

    [Fact]
    public void Comparison_ShowsCapRates()
    {
        var cut = _ctx.Render(WrapComparison($"{Deal1Id},{Deal2Id}"));
        cut.WaitForState(() => cut.Markup.Contains("6.5"));
        Assert.Contains("6.5", cut.Markup);
        Assert.Contains("7.2", cut.Markup);
    }

    [Fact]
    public void Comparison_ShowsIrrValues()
    {
        var cut = _ctx.Render(WrapComparison($"{Deal1Id},{Deal2Id}"));
        cut.WaitForState(() => cut.Markup.Contains("14.2"));
        Assert.Contains("14.2", cut.Markup);
        Assert.Contains("11.8", cut.Markup);
    }

    [Fact]
    public void Comparison_ShowsMetricLabels()
    {
        var cut = _ctx.Render(WrapComparison($"{Deal1Id},{Deal2Id}"));
        cut.WaitForState(() => cut.Markup.Contains("Purchase Price"));
        Assert.Contains("Purchase Price", cut.Markup);
        Assert.Contains("Cap Rate", cut.Markup);
        Assert.Contains("IRR", cut.Markup);
        Assert.Contains("Units", cut.Markup);
    }

    [Fact]
    public void Comparison_NoIds_ShowsMessage()
    {
        var cut = _ctx.Render(WrapComparison(""));
        cut.WaitForState(() => cut.Markup.Contains("Select"));
        Assert.Contains("Select", cut.Markup);
    }

    private class ComparisonStubDealService : IDealService
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
                new() { Id = Deal1Id, PropertyName = "Sunset Apts", Address = "123 Main St", UnitCount = 48, Status = "Draft", PurchasePrice = 5_000_000m, CapRate = 0.065m, Irr = 0.142m, UpdatedAt = DateTime.UtcNow },
                new() { Id = Deal2Id, PropertyName = "Oak Manor", Address = "456 Oak Ave", UnitCount = 32, Status = "InProgress", PurchasePrice = 3_000_000m, CapRate = 0.072m, Irr = 0.118m, UpdatedAt = DateTime.UtcNow.AddHours(-1) },
            };
            return Task.FromResult<IReadOnlyList<DealSummaryDto>>(deals);
        }
    }
}
