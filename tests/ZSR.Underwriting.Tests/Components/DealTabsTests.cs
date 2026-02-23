using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class DealTabsTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;
    private readonly Guid _dealId;

    public DealTabsTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"DealTabsTests_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");

        // Build a separate scope to seed data
        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();
        var deal = new Deal("Test Property", "test-user-id");
        deal.PropertyName = "Sunset Apartments";
        deal.Address = "123 Main St, Phoenix, AZ";
        deal.UnitCount = 200;
        _db.Deals.Add(deal);
        _db.SaveChanges();
        _dealId = deal.Id;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _ctx.DisposeAsync();
    }

    private RenderFragment RenderDealTabs(Guid dealId)
    {
        return builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<DealTabs>(1);
            builder.AddAttribute(2, "DealId", dealId);
            builder.CloseComponent();
        };
    }

    [Fact]
    public void DealTabs_RendersFiveTabHeaders()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("General", cut.Markup);
        Assert.Contains("Underwriting", cut.Markup);
        Assert.Contains("Investors", cut.Markup);
        Assert.Contains("Checklist", cut.Markup);
        Assert.Contains("Chat", cut.Markup);
    }

    [Fact]
    public void DealTabs_ShowsDealNameInHeader()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("Sunset Apartments"));

        Assert.Contains("Sunset Apartments", cut.Markup);
    }

    [Fact]
    public void DealTabs_HasBackButton()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Back button renders as MudIconButton with an SVG icon
        Assert.Contains("mud-icon-button", cut.Markup);
    }

    [Fact]
    public void DealTabs_InvalidDeal_ShowsNotFoundMessage()
    {
        var cut = _ctx.Render(RenderDealTabs(Guid.NewGuid()));
        cut.WaitForState(() => cut.Markup.Contains("not found") || cut.Markup.Contains("General"), TimeSpan.FromSeconds(3));

        Assert.Contains("not found", cut.Markup);
    }

    [Fact]
    public void DealTabs_DefaultsToGeneralTab()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // The General tab panel should be visible with property info
        Assert.Contains("Sunset Apartments", cut.Markup);
    }

    [Fact]
    public void DealTabs_GeneralTab_ShowsPropertyFields()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("Property Information"));

        Assert.Contains("Property Name", cut.Markup);
        Assert.Contains("Address", cut.Markup);
        Assert.Contains("Unit Count", cut.Markup);
        Assert.Contains("Year Built", cut.Markup);
        Assert.Contains("Building Type", cut.Markup);
        Assert.Contains("Square Footage", cut.Markup);
        Assert.Contains("Purchase Price", cut.Markup);
    }

    [Fact]
    public void DealTabs_GeneralTab_ShowsDealClassification()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("Deal Classification"));

        Assert.Contains("Deal Classification", cut.Markup);
        Assert.Contains("Execution Type", cut.Markup);
        Assert.Contains("Transaction Type", cut.Markup);
    }

    [Fact]
    public void DealTabs_GeneralTab_HasSaveButton()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("Save Changes"));

        Assert.Contains("Save Changes", cut.Markup);
    }
}

public class DealTabsUnderwritingTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;
    private readonly Guid _dealId;

    public DealTabsUnderwritingTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"DealTabsUWTests_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();

        // Seed deal with CalculationResult and CapitalStackItems
        var deal = new Deal("Test Property", "test-user-id");
        deal.PropertyName = "Oak Manor";
        deal.Address = "456 Oak Ave";
        deal.UnitCount = 100;
        deal.PurchasePrice = 10_000_000m;
        _db.Deals.Add(deal);

        var calc = new CalculationResult(deal.Id)
        {
            GrossPotentialRent = 1_200_000m,
            VacancyLoss = 60_000m,
            EffectiveGrossIncome = 1_140_000m,
            OtherIncome = 25_000m,
            OperatingExpenses = 500_000m,
            NetOperatingIncome = 665_000m,
            GoingInCapRate = 0.0665m,
            ExitCapRate = 0.07m,
            PricePerUnit = 100_000m,
            LoanAmount = 7_000_000m,
            AnnualDebtService = 450_000m,
            DebtServiceCoverageRatio = 1.48m,
            CashOnCashReturn = 0.072m,
            InternalRateOfReturn = 0.142m,
            EquityMultiple = 1.85m
        };
        _db.CalculationResults.Add(calc);

        _db.CapitalStackItems.Add(new CapitalStackItem(deal.Id, CapitalSource.SeniorDebt, 7_000_000m) { Rate = 0.055m, TermYears = 10, SortOrder = 0 });
        _db.CapitalStackItems.Add(new CapitalStackItem(deal.Id, CapitalSource.SponsorEquity, 3_000_000m) { SortOrder = 1 });

        _db.SaveChanges();
        _dealId = deal.Id;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _ctx.DisposeAsync();
    }

    private RenderFragment RenderDealTabs(Guid dealId)
    {
        return builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<DealTabs>(1);
            builder.AddAttribute(2, "DealId", dealId);
            builder.CloseComponent();
        };
    }

    [Fact]
    public void UnderwritingTab_ShowsNOIBreakdown()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("NOI Breakdown", cut.Markup);
        Assert.Contains("Gross Potential Rent", cut.Markup);
        Assert.Contains("Net Operating Income", cut.Markup);
    }

    [Fact]
    public void UnderwritingTab_ShowsCapRateMetrics()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Going-In Cap Rate", cut.Markup);
        Assert.Contains("Exit Cap Rate", cut.Markup);
    }

    [Fact]
    public void UnderwritingTab_ShowsReturnMetrics()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Cash-on-Cash Return", cut.Markup);
        Assert.Contains("IRR", cut.Markup);
        Assert.Contains("Equity Multiple", cut.Markup);
    }

    [Fact]
    public void UnderwritingTab_ShowsDebtMetrics()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Loan Amount", cut.Markup);
        Assert.Contains("DSCR", cut.Markup);
    }

    [Fact]
    public void UnderwritingTab_ShowsCapitalStackSection()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Capital Stack", cut.Markup);
        Assert.Contains("SeniorDebt", cut.Markup);
        Assert.Contains("SponsorEquity", cut.Markup);
    }

    [Fact]
    public void UnderwritingTab_ShowsTotalSources()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Total of 7M + 3M = 10M
        Assert.Contains("10,000,000", cut.Markup);
    }
}
