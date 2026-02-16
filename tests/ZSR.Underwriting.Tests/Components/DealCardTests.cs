using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class DealCardTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public DealCardTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _ctx.DisposeAsync();
    }

    [Fact]
    public void DealCard_ShowsPropertyName()
    {
        var deal = CreateTestDeal();
        var cut = _ctx.Render<DealCard>(parameters => parameters
            .Add(p => p.Deal, deal));
        Assert.Contains("Sunset Apts", cut.Markup);
    }

    [Fact]
    public void DealCard_ShowsAddress()
    {
        var deal = CreateTestDeal();
        var cut = _ctx.Render<DealCard>(parameters => parameters
            .Add(p => p.Deal, deal));
        Assert.Contains("123 Main St", cut.Markup);
    }

    [Fact]
    public void DealCard_ShowsPurchasePrice()
    {
        var deal = CreateTestDeal();
        var cut = _ctx.Render<DealCard>(parameters => parameters
            .Add(p => p.Deal, deal));
        Assert.Contains("5,000,000", cut.Markup);
    }

    [Fact]
    public void DealCard_ShowsCapRate()
    {
        var deal = CreateTestDeal();
        var cut = _ctx.Render<DealCard>(parameters => parameters
            .Add(p => p.Deal, deal));
        Assert.Contains("6.5", cut.Markup);
    }

    [Fact]
    public void DealCard_ShowsIrr()
    {
        var deal = CreateTestDeal();
        var cut = _ctx.Render<DealCard>(parameters => parameters
            .Add(p => p.Deal, deal));
        Assert.Contains("14.2", cut.Markup);
    }

    [Fact]
    public void DealCard_ShowsStatusChip()
    {
        var deal = CreateTestDeal();
        var cut = _ctx.Render<DealCard>(parameters => parameters
            .Add(p => p.Deal, deal));
        Assert.Contains("Draft", cut.Markup);
    }

    [Fact]
    public void DealCard_ShowsUnitCount()
    {
        var deal = CreateTestDeal();
        var cut = _ctx.Render<DealCard>(parameters => parameters
            .Add(p => p.Deal, deal));
        Assert.Contains("48", cut.Markup);
    }

    [Fact]
    public void DealCard_HandlesNullCapRate()
    {
        var deal = CreateTestDeal();
        deal.CapRate = null;
        var cut = _ctx.Render<DealCard>(parameters => parameters
            .Add(p => p.Deal, deal));
        // Should not throw, should show dash or N/A
        Assert.DoesNotContain("6.5", cut.Markup);
    }

    private static DealSummaryDto CreateTestDeal() => new()
    {
        Id = Guid.NewGuid(),
        PropertyName = "Sunset Apts",
        Address = "123 Main St",
        UnitCount = 48,
        PurchasePrice = 5_000_000m,
        Status = "Draft",
        CapRate = 0.065m,
        Irr = 0.142m,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
