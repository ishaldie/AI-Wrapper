using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Components;

public class FannieProductTypeUITests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;
    private readonly Guid _fannieDealId;
    private readonly Guid _nonFannieDealId;
    private readonly Guid _fannieWithCalcId;

    public FannieProductTypeUITests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"FannieUITests_{Guid.NewGuid()}";
        _ctx.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var authCtx = _ctx.AddAuthorization();
        authCtx.SetAuthorized("Test User");

        // Register stub services required by DealTabs
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
        _ctx.Services.AddSingleton<IAssetReportService>(new NoOpAssetReportService());
        _ctx.Services.AddSingleton<IDispositionService>(new NoOpDispositionService());
        _ctx.Services.AddSingleton<ICmsProviderService>(new NoOpCmsProviderService());
        _ctx.Services.AddSingleton<ISecuritizationCompService>(new NoOpSecuritizationCompService());

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();

        // Seed a Fannie Mae deal
        var fannieDeal = new Deal("Fannie Test", "test-user");
        fannieDeal.PropertyName = "Fannie Towers";
        fannieDeal.Address = "100 Fannie Ln, Atlanta, GA";
        fannieDeal.UnitCount = 150;
        fannieDeal.PurchasePrice = 20_000_000m;
        fannieDeal.ExecutionType = ExecutionType.FannieMae;
        fannieDeal.FannieProductType = FannieProductType.SeniorsAL;
        _db.Deals.Add(fannieDeal);
        _fannieDealId = fannieDeal.Id;

        // Seed a non-Fannie deal
        var nonFannieDeal = new Deal("Non-Fannie Test", "test-user");
        nonFannieDeal.PropertyName = "Regular Apartments";
        nonFannieDeal.Address = "200 Normal St, Dallas, TX";
        nonFannieDeal.UnitCount = 100;
        nonFannieDeal.ExecutionType = ExecutionType.All;
        _db.Deals.Add(nonFannieDeal);
        _nonFannieDealId = nonFannieDeal.Id;

        // Seed a Fannie deal with calculation results + compliance JSON
        var fannieCalcDeal = new Deal("Fannie Calc Test", "test-user");
        fannieCalcDeal.PropertyName = "Compliance Manor";
        fannieCalcDeal.Address = "300 Compliance Blvd, Houston, TX";
        fannieCalcDeal.UnitCount = 200;
        fannieCalcDeal.PurchasePrice = 25_000_000m;
        fannieCalcDeal.ExecutionType = ExecutionType.FannieMae;
        fannieCalcDeal.FannieProductType = FannieProductType.Conventional;
        _db.Deals.Add(fannieCalcDeal);
        _fannieWithCalcId = fannieCalcDeal.Id;

        var calcResult = new CalculationResult(fannieCalcDeal.Id)
        {
            NetOperatingIncome = 1_800_000m,
            GoingInCapRate = 7.2m,
            LoanAmount = 18_000_000m,
            AnnualDebtService = 1_200_000m,
            DebtServiceCoverageRatio = 1.50m,
            CashOnCashReturn = 8.6m,
            InternalRateOfReturn = 16.0m,
            EquityMultiple = 1.9m,
            FannieComplianceJson = JsonSerializer.Serialize(new FannieComplianceResult
            {
                OverallPass = true,
                ProductMinDscr = 1.25m,
                ProductMaxLtvPercent = 80m,
                ProductMaxAmortYears = 30,
                DscrTest = new ComplianceTest { Name = "DSCR", Pass = true, ActualValue = 1.50m, RequiredValue = 1.25m },
                LtvTest = new ComplianceTest { Name = "LTV", Pass = true, ActualValue = 72m, RequiredValue = 80m },
                AmortizationTest = new ComplianceTest { Name = "Amortization", Pass = true, ActualValue = 30, RequiredValue = 30 }
            })
        };
        _db.CalculationResults.Add(calcResult);

        _db.SaveChanges();
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

    // === Task 1: FannieProductType dropdown visibility ===

    [Fact]
    public void FannieDeal_ShowsProductTypeDropdown()
    {
        var cut = _ctx.Render(RenderDealTabs(_fannieDealId));
        cut.WaitForState(() => cut.Markup.Contains("Fannie Towers"));

        Assert.Contains("Fannie Mae Product", cut.Markup);
    }

    [Fact]
    public void NonFannieDeal_HidesProductTypeDropdown()
    {
        var cut = _ctx.Render(RenderDealTabs(_nonFannieDealId));
        cut.WaitForState(() => cut.Markup.Contains("Regular Apartments"));

        Assert.DoesNotContain("Fannie Mae Product", cut.Markup);
    }

    // === Task 4: Compliance summary card on Analysis tab ===

    [Fact]
    public void FannieDealWithCalc_ShowsComplianceSummary()
    {
        var cut = _ctx.Render(RenderDealTabs(_fannieWithCalcId));
        cut.WaitForState(() => cut.Markup.Contains("Compliance Manor"));

        // Navigate to Underwriting tab by checking markup
        // The compliance card should be rendered in the Underwriting tab
        Assert.Contains("Fannie Mae Compliance", cut.Markup);
    }

    [Fact]
    public void FannieDealWithCalc_ShowsPassFailStatus()
    {
        var cut = _ctx.Render(RenderDealTabs(_fannieWithCalcId));
        cut.WaitForState(() => cut.Markup.Contains("Compliance Manor"));

        Assert.Contains("PASS", cut.Markup);
    }

    [Fact]
    public void FannieDealWithCalc_ShowsProductThresholds()
    {
        var cut = _ctx.Render(RenderDealTabs(_fannieWithCalcId));
        cut.WaitForState(() => cut.Markup.Contains("Compliance Manor"));

        Assert.Contains("1.25", cut.Markup); // Min DSCR
        Assert.Contains("80", cut.Markup);   // Max LTV
    }

    [Fact]
    public void NonFannieDeal_NoComplianceCard()
    {
        var cut = _ctx.Render(RenderDealTabs(_nonFannieDealId));
        cut.WaitForState(() => cut.Markup.Contains("Regular Apartments"));

        Assert.DoesNotContain("Fannie Mae Compliance", cut.Markup);
    }
}
