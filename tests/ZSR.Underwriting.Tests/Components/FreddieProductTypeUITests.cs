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

public class FreddieProductTypeUITests : IAsyncLifetime
{
    private readonly BunitContext _ctx;
    private readonly AppDbContext _db;
    private readonly Guid _freddieDealId;
    private readonly Guid _nonFreddieDealId;
    private readonly Guid _freddieWithCalcId;

    public FreddieProductTypeUITests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();

        var dbName = $"FreddieUITests_{Guid.NewGuid()}";
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
        _ctx.Services.AddSingleton<IAssetReportService>(new NoOpAssetReportService());
        _ctx.Services.AddSingleton<IDispositionService>(new NoOpDispositionService());
        _ctx.Services.AddSingleton<ICmsProviderService>(new NoOpCmsProviderService());
        _ctx.Services.AddSingleton<ISecuritizationCompService>(new NoOpSecuritizationCompService());

        var sp = _ctx.Services.BuildServiceProvider();
        _db = sp.GetRequiredService<AppDbContext>();

        // Seed a Freddie Mac deal
        var freddieDeal = new Deal("Freddie Test", "test-user");
        freddieDeal.PropertyName = "Freddie Heights";
        freddieDeal.Address = "100 Freddie Ave, McLean, VA";
        freddieDeal.UnitCount = 150;
        freddieDeal.PurchasePrice = 20_000_000m;
        freddieDeal.ExecutionType = ExecutionType.FreddieMac;
        freddieDeal.FreddieProductType = FreddieProductType.SmallBalanceLoan;
        _db.Deals.Add(freddieDeal);
        _freddieDealId = freddieDeal.Id;

        // Seed a non-Freddie deal
        var nonFreddieDeal = new Deal("Non-Freddie Test", "test-user");
        nonFreddieDeal.PropertyName = "Regular Place";
        nonFreddieDeal.Address = "200 Normal Ave, Dallas, TX";
        nonFreddieDeal.UnitCount = 100;
        nonFreddieDeal.ExecutionType = ExecutionType.All;
        _db.Deals.Add(nonFreddieDeal);
        _nonFreddieDealId = nonFreddieDeal.Id;

        // Seed a Freddie deal with calculation results + compliance JSON
        var freddieCalcDeal = new Deal("Freddie Calc Test", "test-user");
        freddieCalcDeal.PropertyName = "Freddie Compliance Tower";
        freddieCalcDeal.Address = "300 Compliance Way, Reston, VA";
        freddieCalcDeal.UnitCount = 200;
        freddieCalcDeal.PurchasePrice = 25_000_000m;
        freddieCalcDeal.ExecutionType = ExecutionType.FreddieMac;
        freddieCalcDeal.FreddieProductType = FreddieProductType.Conventional;
        _db.Deals.Add(freddieCalcDeal);
        _freddieWithCalcId = freddieCalcDeal.Id;

        var calcResult = new CalculationResult(freddieCalcDeal.Id)
        {
            NetOperatingIncome = 1_800_000m,
            GoingInCapRate = 7.2m,
            LoanAmount = 18_000_000m,
            AnnualDebtService = 1_200_000m,
            DebtServiceCoverageRatio = 1.50m,
            CashOnCashReturn = 8.6m,
            InternalRateOfReturn = 16.0m,
            EquityMultiple = 1.9m,
            FreddieComplianceJson = JsonSerializer.Serialize(new FreddieComplianceResult
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

    [Fact]
    public void FreddieDeal_ShowsProductTypeDropdown()
    {
        var cut = _ctx.Render(RenderDealTabs(_freddieDealId));
        cut.WaitForState(() => cut.Markup.Contains("Freddie Heights"));

        Assert.Contains("Freddie Mac Product", cut.Markup);
    }

    [Fact]
    public void NonFreddieDeal_HidesProductTypeDropdown()
    {
        var cut = _ctx.Render(RenderDealTabs(_nonFreddieDealId));
        cut.WaitForState(() => cut.Markup.Contains("Regular Place"));

        Assert.DoesNotContain("Freddie Mac Product", cut.Markup);
    }

    [Fact]
    public void FreddieDealWithCalc_ShowsComplianceSummary()
    {
        var cut = _ctx.Render(RenderDealTabs(_freddieWithCalcId));
        cut.WaitForState(() => cut.Markup.Contains("Freddie Compliance Tower"));

        Assert.Contains("Freddie Mac Compliance", cut.Markup);
    }

    [Fact]
    public void FreddieDealWithCalc_ShowsPassFailStatus()
    {
        var cut = _ctx.Render(RenderDealTabs(_freddieWithCalcId));
        cut.WaitForState(() => cut.Markup.Contains("Freddie Compliance Tower"));

        Assert.Contains("PASS", cut.Markup);
    }

    [Fact]
    public void FreddieDealWithCalc_ShowsProductThresholds()
    {
        var cut = _ctx.Render(RenderDealTabs(_freddieWithCalcId));
        cut.WaitForState(() => cut.Markup.Contains("Freddie Compliance Tower"));

        Assert.Contains("1.25", cut.Markup); // Min DSCR
        Assert.Contains("80", cut.Markup);   // Max LTV
    }

    [Fact]
    public void NonFreddieDeal_NoFreddieComplianceCard()
    {
        var cut = _ctx.Render(RenderDealTabs(_nonFreddieDealId));
        cut.WaitForState(() => cut.Markup.Contains("Regular Place"));

        Assert.DoesNotContain("Freddie Mac Compliance", cut.Markup);
    }
}
