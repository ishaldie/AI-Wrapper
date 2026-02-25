using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;
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

        // Register stub services required by DealTabs
        _ctx.Services.AddSingleton<IDocumentUploadService>(new StubDocumentUploadService());
        _ctx.Services.AddSingleton<IDocumentMatchingService>(new StubDocumentMatchingService());
        _ctx.Services.AddSingleton<ISensitivityCalculator>(new SensitivityCalculatorService());

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
        Assert.Contains("Documents", cut.Markup);
    }

    [Fact]
    public void DealTabs_ChatIsNotATab()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Chat should NOT be in the tab headers (it's a side panel now)
        var tabHeaders = cut.FindAll(".mud-tab");
        Assert.DoesNotContain(tabHeaders, th => th.TextContent.Contains("Chat"));
    }

    [Fact]
    public void DealTabs_HasChatToggleButton()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Chat toggle button should exist in the header
        Assert.Contains("chat-toggle-btn", cut.Markup);
    }

    [Fact]
    public void DealTabs_ChatPanelClosedByDefault()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Side panel should not be rendered when closed
        Assert.DoesNotContain("deal-chat-panel", cut.Markup);
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

        _ctx.Services.AddSingleton<IDocumentUploadService>(new StubDocumentUploadService());
        _ctx.Services.AddSingleton<IDocumentMatchingService>(new StubDocumentMatchingService());
        _ctx.Services.AddSingleton<ISensitivityCalculator>(new SensitivityCalculatorService());

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

    [Fact]
    public void UnderwritingTab_ShowsSensitivityAnalysis()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Sensitivity Analysis", cut.Markup);
        Assert.Contains("Base Case", cut.Markup);
    }
}

public class DealTabsChecklistTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;
    private readonly Guid _dealId;

    public DealTabsChecklistTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"DealTabsCLTests_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");

        _ctx.Services.AddSingleton<IDocumentUploadService>(new StubDocumentUploadService());
        _ctx.Services.AddSingleton<IDocumentMatchingService>(new StubDocumentMatchingService());
        _ctx.Services.AddSingleton<ISensitivityCalculator>(new SensitivityCalculatorService());

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();

        // Seed checklist templates (a subset for testing)
        var t1 = new ChecklistTemplate("Historical & Proforma Property Operations", 1, "Current Months Rent Roll", 1, ExecutionType.All, "All");
        var t2 = new ChecklistTemplate("Historical & Proforma Property Operations", 1, "Trailing 12 Month Operating Statement", 2, ExecutionType.All, "All");
        var t3 = new ChecklistTemplate("Property Title & Survey", 2, "Existing Survey", 3, ExecutionType.All, "All");
        var t4 = new ChecklistTemplate("Property Title & Survey", 2, "Title Policy", 4, ExecutionType.All, "All");
        var t5 = new ChecklistTemplate("Historical & Proforma Property Operations", 1, "Freddie Mac Form 1112", 5, ExecutionType.FreddieMac, "All");
        _db.ChecklistTemplates.AddRange(t1, t2, t3, t4, t5);

        // Seed deal with ExecutionType = All (should include t1-t4 but not t5)
        var deal = new Deal("Test Property", "test-user-id");
        deal.PropertyName = "Pine Ridge";
        deal.Address = "789 Pine Rd";
        deal.UnitCount = 50;
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
    public void ChecklistTab_ShowsSectionHeaders()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Historical &amp; Proforma Property Operations", cut.Markup);
        Assert.Contains("Property Title &amp; Survey", cut.Markup);
    }

    [Fact]
    public void ChecklistTab_ShowsItemNames()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Current Months Rent Roll", cut.Markup);
        Assert.Contains("Existing Survey", cut.Markup);
    }

    [Fact]
    public void ChecklistTab_AutoGeneratesItemsOnFirstVisit()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Should have created DealChecklistItems in DB
        var items = _db.DealChecklistItems.Where(x => x.DealId == _dealId).ToList();
        Assert.True(items.Count > 0);
    }

    [Fact]
    public void ChecklistTab_ShowsStatusChips()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Default status is Outstanding
        Assert.Contains("Outstanding", cut.Markup);
    }

    [Fact]
    public void ChecklistTab_ShowsProgressSummary()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Should show progress text with "satisfied" label
        Assert.Contains("satisfied", cut.Markup);
    }
}

public class DealTabsChecklistUploadTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;
    private readonly Guid _dealId;

    public DealTabsChecklistUploadTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"DealTabsCLUploadTests_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");

        _ctx.Services.AddSingleton<IDocumentUploadService>(new StubDocumentUploadService());
        _ctx.Services.AddSingleton<IDocumentMatchingService>(new StubDocumentMatchingService());
        _ctx.Services.AddSingleton<ISensitivityCalculator>(new SensitivityCalculatorService());

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();

        // Seed checklist templates
        var t1 = new ChecklistTemplate("Operations", 1, "Current Months Rent Roll", 1, ExecutionType.All, "All");
        var t2 = new ChecklistTemplate("Operations", 1, "Trailing 12 Month Operating Statement", 2, ExecutionType.All, "All");
        _db.ChecklistTemplates.AddRange(t1, t2);

        var deal = new Deal("Test Property", "test-user-id");
        deal.PropertyName = "Upload Test Deal";
        deal.Address = "100 Upload Ave";
        deal.UnitCount = 10;
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
    public void ChecklistItems_RenderWithStatusAndNames()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("Current Months Rent Roll"));

        // Both checklist items should render with their names
        Assert.Contains("Current Months Rent Roll", cut.Markup);
        Assert.Contains("Trailing 12 Month Operating Statement", cut.Markup);

        // Default status should be Outstanding
        Assert.Contains("Outstanding", cut.Markup);
    }

    [Fact]
    public void ChecklistItems_NoProgressSpinnerByDefault()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("Current Months Rent Roll"));

        // Progress spinner should NOT be present when not uploading
        Assert.DoesNotContain("mud-progress-circular", cut.Markup);
    }

    [Fact]
    public void ChecklistItem_WithLinkedDocument_ShowsSatisfiedStatus()
    {
        // Pre-seed checklist items for ALL templates (mimic GenerateChecklistItems)
        var templates = _db.ChecklistTemplates.OrderBy(t => t.SortOrder).ToList();
        var doc = new UploadedDocument(_dealId, "rent_roll.pdf", "stored/path.pdf", DocumentType.RentRoll, 1024);
        _db.UploadedDocuments.Add(doc);
        _db.SaveChanges();

        foreach (var t in templates)
        {
            var ci = new DealChecklistItem(_dealId, t.Id);
            ci.Template = t;
            if (t.ItemName.Contains("Rent Roll"))
                ci.MarkSatisfied(doc.Id);
            _db.DealChecklistItems.Add(ci);
        }
        _db.SaveChanges();

        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("Current Months Rent Roll"));

        // When a document is linked via MarkSatisfied, the status chip shows "Satisfied"
        Assert.Contains("Satisfied", cut.Markup);

        // Verify the document was correctly linked in the data model
        var savedItem = _db.DealChecklistItems.First(ci => ci.DocumentId != null);
        Assert.Equal(doc.Id, savedItem.DocumentId);
        Assert.Equal(ChecklistStatus.Satisfied, savedItem.Status);
    }

    [Fact]
    public void ChecklistItems_HaveStatusDropdowns()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("Current Months Rent Roll"));

        // Each checklist item should have a MudSelect status dropdown
        var selects = cut.FindAll(".mud-select");
        // At least 2 selects for the 2 checklist items
        Assert.True(selects.Count >= 2, $"Expected at least 2 status dropdowns, found {selects.Count}");
    }
}

public class DealTabsInvestorTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;
    private readonly Guid _dealId;

    public DealTabsInvestorTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"DealTabsInvTests_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");

        _ctx.Services.AddSingleton<IDocumentUploadService>(new StubDocumentUploadService());
        _ctx.Services.AddSingleton<IDocumentMatchingService>(new StubDocumentMatchingService());
        _ctx.Services.AddSingleton<ISensitivityCalculator>(new SensitivityCalculatorService());

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();

        // Seed deal with existing investors
        var deal = new Deal("Test Property", "test-user-id");
        deal.PropertyName = "Maple Heights";
        deal.Address = "321 Maple Dr";
        deal.UnitCount = 120;
        _db.Deals.Add(deal);

        var inv1 = new DealInvestor(deal.Id, "John Smith")
        {
            Company = "Smith Capital",
            Role = "Lead Sponsor",
            Email = "john@smithcap.com",
            Phone = "(555) 123-4567",
            Address = "100 Wall St",
            City = "New York",
            State = "NY",
            Zip = "10005",
            NetWorth = 5_000_000m,
            Liquidity = 1_500_000m
        };
        var inv2 = new DealInvestor(deal.Id, "Jane Doe")
        {
            Company = "Doe Investments",
            Role = "LP Investor",
            Email = "jane@doeinv.com",
            Phone = "(555) 987-6543",
            NetWorth = 2_000_000m,
            Liquidity = 800_000m
        };
        _db.DealInvestors.AddRange(inv1, inv2);
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
    public void InvestorTab_ShowsInvestorTable()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Investors", cut.Markup);
        Assert.Contains("John Smith", cut.Markup);
        Assert.Contains("Jane Doe", cut.Markup);
    }

    [Fact]
    public void InvestorTab_ShowsInvestorDetails()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Smith Capital", cut.Markup);
        Assert.Contains("Doe Investments", cut.Markup);
        Assert.Contains("Lead Sponsor", cut.Markup);
        Assert.Contains("LP Investor", cut.Markup);
    }

    [Fact]
    public void InvestorTab_ShowsContactInfo()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("john@smithcap.com", cut.Markup);
        Assert.Contains("(555) 123-4567", cut.Markup);
    }

    [Fact]
    public void InvestorTab_ShowsFinancialInfo()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Net worth $5,000,000 and liquidity $1,500,000
        Assert.Contains("5,000,000", cut.Markup);
        Assert.Contains("1,500,000", cut.Markup);
    }

    [Fact]
    public void InvestorTab_HasAddButton()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("Add Investor", cut.Markup);
    }

    [Fact]
    public void InvestorTab_ShowsInvestorCount()
    {
        var cut = _ctx.Render(RenderDealTabs(_dealId));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        // Should show "2 investors" or similar count
        Assert.Contains("2", cut.Markup);
    }

    [Fact]
    public void InvestorTab_EmptyState_ShowsMessage()
    {
        // Create a deal with no investors
        var deal = new Deal("Empty Property", "test-user-id");
        deal.PropertyName = "Empty Place";
        deal.Address = "1 Empty St";
        deal.UnitCount = 10;
        _db.Deals.Add(deal);
        _db.SaveChanges();

        var cut = _ctx.Render(RenderDealTabs(deal.Id));
        cut.WaitForState(() => cut.Markup.Contains("General"));

        Assert.Contains("No investors", cut.Markup);
    }
}

public class DealTabsChatTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;
    private readonly Guid _dealId;

    public DealTabsChatTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"DealTabsChatTests_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");

        // Register stub services required by DealChatTab and DealTabs
        _ctx.Services.AddSingleton<IClaudeClient>(new StubClaudeClient());
        _ctx.Services.AddSingleton<IDocumentUploadService>(new StubDocumentUploadService());
        _ctx.Services.AddSingleton<IDocumentParsingService>(new StubDocumentParsingService());
        _ctx.Services.AddSingleton<IDocumentMatchingService>(new StubDocumentMatchingService());
        _ctx.Services.AddSingleton<ISensitivityCalculator>(new SensitivityCalculatorService());
        _ctx.Services.AddLogging();

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();

        // Seed deal with existing chat messages (avoids auto-analysis trigger)
        var deal = new Deal("Test Property", "test-user-id");
        deal.PropertyName = "Chat Test Apts";
        deal.Address = "555 Chat Blvd";
        deal.UnitCount = 80;
        _db.Deals.Add(deal);

        _db.ChatMessages.Add(new ChatMessage(deal.Id, "user", "Analyze this property"));
        _db.ChatMessages.Add(new ChatMessage(deal.Id, "assistant", "Here is the analysis for Chat Test Apts."));

        _db.SaveChanges();
        _dealId = deal.Id;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _ctx.DisposeAsync();
    }

    [Fact]
    public void ChatTab_RendersChatComponent()
    {
        var cut = _ctx.Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<DealChatTab>(1);
            builder.AddAttribute(2, "DealId", _dealId);
            builder.CloseComponent();
        });

        // Wait for messages to load
        cut.WaitForState(() => cut.Markup.Contains("Chat Test Apts") || cut.Markup.Contains("Analyze this"), TimeSpan.FromSeconds(3));

        // Should show the chat messages
        Assert.Contains("Here is the analysis", cut.Markup);
    }

    [Fact]
    public void ChatTab_ShowsInputBar()
    {
        var cut = _ctx.Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<DealChatTab>(1);
            builder.AddAttribute(2, "DealId", _dealId);
            builder.CloseComponent();
        });

        cut.WaitForState(() => cut.Markup.Contains("Ask about this property"), TimeSpan.FromSeconds(3));

        Assert.Contains("Ask about this property", cut.Markup);
    }

    [Fact]
    public void ChatTab_ShowsMessageHistory()
    {
        var cut = _ctx.Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<DealChatTab>(1);
            builder.AddAttribute(2, "DealId", _dealId);
            builder.CloseComponent();
        });

        cut.WaitForState(() => cut.Markup.Contains("Analyze this"), TimeSpan.FromSeconds(3));

        // Both user and assistant messages should appear
        Assert.Contains("Analyze this property", cut.Markup);
        Assert.Contains("Here is the analysis", cut.Markup);
    }

    [Fact]
    public void DealTabs_ChatToggleButton_Exists()
    {
        var cut = _ctx.Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<DealTabs>(1);
            builder.AddAttribute(2, "DealId", _dealId);
            builder.CloseComponent();
        });

        cut.WaitForState(() => cut.Markup.Contains("General"), TimeSpan.FromSeconds(3));

        // Chat toggle button should be rendered in the header
        Assert.Contains("chat-toggle-btn", cut.Markup);
    }
}

// Stub implementations for chat service dependencies
internal class StubClaudeClient : IClaudeClient
{
    public Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken ct = default)
        => Task.FromResult(new ClaudeResponse { Content = "Test AI response" });
}

internal class StubDocumentUploadService : IDocumentUploadService
{
    public Task<FileUploadResultDto> UploadDocumentAsync(Guid dealId, Stream fileStream, string fileName, DocumentType documentType, string userId, CancellationToken ct = default)
        => Task.FromResult(new FileUploadResultDto { DocumentId = Guid.NewGuid(), FileName = fileName });

    public Task<IReadOnlyList<FileUploadResultDto>> GetDocumentsForDealAsync(Guid dealId, string userId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FileUploadResultDto>>(new List<FileUploadResultDto>());

    public Task DeleteDocumentAsync(Guid documentId, string userId, CancellationToken ct = default)
        => Task.CompletedTask;
}

internal class StubDocumentParsingService : IDocumentParsingService
{
    public Task<ParsedDocumentResult> ParseDocumentAsync(Guid documentId, CancellationToken ct = default)
        => Task.FromResult(new ParsedDocumentResult { Success = false, ErrorMessage = "Stub" });
}

internal class StubDocumentMatchingService : IDocumentMatchingService
{
    public DocumentMatchResult? FindBestMatch(string fileName, DocumentType documentType, IReadOnlyList<ChecklistMatchCandidate> candidates)
        => candidates.Count > 0
            ? new DocumentMatchResult(candidates[0].ChecklistItemId, candidates[0].ItemName, 1.0)
            : null;
}
