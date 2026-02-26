using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components;
using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class DealTabsPhaseTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;

    public DealTabsPhaseTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"DealTabsPhaseTests_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");

        _ctx.Services.AddSingleton<IDocumentUploadService>(new StubDocumentUploadService());
        _ctx.Services.AddSingleton<IDocumentMatchingService>(new StubDocumentMatchingService());
        _ctx.Services.AddSingleton<IActivityTracker>(new NoOpActivityTracker());
        _ctx.Services.AddSingleton<ISensitivityCalculator>(new SensitivityCalculatorService());
        _ctx.Services.AddSingleton<IMarketDataService>(new StubMarketDataService());
        _ctx.Services.AddSingleton<IDealService>(new NoOpDealService());
        _ctx.Services.AddSingleton<IContractService>(new NoOpContractService());
        _ctx.Services.AddSingleton<IRentRollService>(new NoOpRentRollService());
        _ctx.Services.AddSingleton<IPortfolioService>(new NoOpPortfolioService());
        _ctx.Services.AddSingleton<IActualsService>(new NoOpActualsService());
        _ctx.Services.AddSingleton<ICapExService>(new NoOpCapExService());
        _ctx.Services.AddSingleton<IVarianceCalculator>(new NoOpVarianceCalculator());

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _ctx.DisposeAsync();
    }

    private Guid SeedDeal(DealStatus status)
    {
        var deal = new Deal("Test Property", "test-user-id");
        deal.PropertyName = $"Phase Test {status}";
        deal.Address = "123 Phase St";
        deal.UnitCount = 100;
        deal.UpdateStatus(status);
        _db.Deals.Add(deal);
        _db.SaveChanges();
        return deal.Id;
    }

    private RenderFragment RenderDealTabs(Guid dealId) => builder =>
    {
        builder.OpenComponent<MudPopoverProvider>(0);
        builder.CloseComponent();
        builder.OpenComponent<DealTabs>(1);
        builder.AddAttribute(2, "DealId", dealId);
        builder.CloseComponent();
    };

    // === Acquisition phase: only base 5 tabs ===

    [Fact]
    public void AcquisitionPhase_ShowsBaseTabs()
    {
        var dealId = SeedDeal(DealStatus.Draft);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("General", cut.Markup);
        Assert.Contains("Underwriting", cut.Markup);
        Assert.Contains("Investors", cut.Markup);
        Assert.Contains("Checklist", cut.Markup);
        Assert.Contains("Documents", cut.Markup);
    }

    [Fact]
    public void AcquisitionPhase_DoesNotShowContractTab()
    {
        var dealId = SeedDeal(DealStatus.Draft);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Contract tab should NOT appear for Draft deals
        var tabs = cut.FindAll(".mud-tab");
        Assert.DoesNotContain(tabs, t => t.TextContent.Contains("Contract"));
    }

    [Fact]
    public void AcquisitionPhase_DoesNotShowOwnershipTabs()
    {
        var dealId = SeedDeal(DealStatus.Screening);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        var tabs = cut.FindAll(".mud-tab");
        Assert.DoesNotContain(tabs, t => t.TextContent.Contains("Rent Roll"));
        Assert.DoesNotContain(tabs, t => t.TextContent.Contains("Actuals"));
        Assert.DoesNotContain(tabs, t => t.TextContent.Contains("Performance"));
        Assert.DoesNotContain(tabs, t => t.TextContent.Contains("CapEx"));
    }

    // === Contract phase: base + Contract tab ===

    [Fact]
    public void ContractPhase_ShowsContractTab()
    {
        var dealId = SeedDeal(DealStatus.UnderContract);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Contract", cut.Markup);
    }

    [Fact]
    public void ContractPhase_DoesNotShowOwnershipTabs()
    {
        var dealId = SeedDeal(DealStatus.UnderContract);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        var tabs = cut.FindAll(".mud-tab");
        Assert.DoesNotContain(tabs, t => t.TextContent.Contains("Rent Roll"));
        Assert.DoesNotContain(tabs, t => t.TextContent.Contains("Actuals"));
    }

    // === Ownership phase: base + Contract + ownership tabs ===

    [Fact]
    public void OwnershipPhase_ShowsOwnershipTabs()
    {
        var dealId = SeedDeal(DealStatus.Active);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Rent Roll", cut.Markup);
        Assert.Contains("Actuals", cut.Markup);
        Assert.Contains("Performance", cut.Markup);
        Assert.Contains("CapEx", cut.Markup);
    }

    [Fact]
    public void OwnershipPhase_AlsoShowsContractTab()
    {
        var dealId = SeedDeal(DealStatus.Active);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Contract", cut.Markup);
    }

    // === Exit phase: all tabs ===

    [Fact]
    public void ExitPhase_ShowsDispositionTab()
    {
        var dealId = SeedDeal(DealStatus.Disposition);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Disposition", cut.Markup);
    }

    [Fact]
    public void ExitPhase_AlsoShowsOwnershipAndContractTabs()
    {
        var dealId = SeedDeal(DealStatus.Disposition);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Contract", cut.Markup);
        Assert.Contains("Rent Roll", cut.Markup);
        Assert.Contains("Actuals", cut.Markup);
        Assert.Contains("Performance", cut.Markup);
        Assert.Contains("CapEx", cut.Markup);
        Assert.Contains("Disposition", cut.Markup);
    }

    // === Status transition buttons ===

    [Fact]
    public void DraftDeal_ShowsStartScreeningButton()
    {
        var dealId = SeedDeal(DealStatus.Draft);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Start Screening", cut.Markup);
    }

    [Fact]
    public void ScreeningDeal_ShowsMarkCompleteButton()
    {
        var dealId = SeedDeal(DealStatus.Screening);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Mark Complete", cut.Markup);
    }

    [Fact]
    public void CompleteDeal_ShowsMoveToUnderContractButton()
    {
        var dealId = SeedDeal(DealStatus.Complete);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Move to Under Contract", cut.Markup);
    }

    [Fact]
    public void ActiveDeal_ShowsBeginDispositionButton()
    {
        var dealId = SeedDeal(DealStatus.Active);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Begin Disposition", cut.Markup);
    }

    [Fact]
    public void SoldDeal_HasNoTransitionButton()
    {
        var dealId = SeedDeal(DealStatus.Sold);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // No next transition from Sold
        Assert.DoesNotContain("Mark ", cut.Markup.Replace("Mark Complete", "").Replace("Mark Closed", "").Replace("Mark Sold", ""));
    }

    // === Status display names ===

    [Fact]
    public void UnderContractDeal_DisplaysUnderContract()
    {
        var dealId = SeedDeal(DealStatus.UnderContract);
        var cut = _ctx.Render(RenderDealTabs(dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Under Contract", cut.Markup);
    }
}
