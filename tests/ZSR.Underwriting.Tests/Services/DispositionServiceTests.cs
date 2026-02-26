using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class DispositionServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly DispositionService _service;
    private readonly Guid _dealId;

    public DispositionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DispoTests_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        var actualsService = new ActualsService(_db);
        _service = new DispositionService(_db, actualsService);

        _dealId = Guid.NewGuid();
        var deal = new Deal("Dispo Test Property", "test-user");
        typeof(Deal).GetProperty("Id")!.SetValue(deal, _dealId);
        typeof(Deal).GetProperty("PurchasePrice")!.SetValue(deal, 1000000m);
        deal.ClosedDate = DateTime.UtcNow.AddMonths(-24);
        deal.ActualPurchasePrice = 980000m;

        var calc = new CalculationResult(_dealId)
        {
            NetOperatingIncome = 100000m,
            EffectiveGrossIncome = 160000m,
            OperatingExpenses = 60000m,
            LoanAmount = 700000m,
            AnnualDebtService = 42000m,
            GoingInCapRate = 6.5m,
            CashOnCashReturn = 8m
        };
        deal.CalculationResult = calc;

        _db.Deals.Add(deal);
        _db.CalculationResults.Add(calc);
        _db.SaveChanges();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private void SeedActuals(int months = 12, decimal gri = 14000m)
    {
        for (int m = 1; m <= months; m++)
        {
            var actual = new MonthlyActual(_dealId, 2025, m)
            {
                GrossRentalIncome = gri,
                VacancyLoss = gri * 0.05m,
                OtherIncome = 100m,
                PropertyTaxes = 1500m,
                Insurance = 600m,
                Utilities = 800m,
                Repairs = 400m,
                Management = 1000m,
                DebtService = 3500m,
                OccupiedUnits = 48,
                TotalUnits = 50
            };
            actual.Recalculate();
            _db.MonthlyActuals.Add(actual);
        }
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateOrUpdate_CreatesNewAnalysis()
    {
        SeedActuals();
        var analysis = await _service.CreateOrUpdateAsync(_dealId, marketCapRate: 6.0m);

        Assert.NotNull(analysis);
        Assert.Equal(_dealId, analysis.DealId);
        Assert.True(analysis.TrailingTwelveMonthNoi > 0);
    }

    [Fact]
    public async Task CreateOrUpdate_UpdatesExistingAnalysis()
    {
        SeedActuals();
        var first = await _service.CreateOrUpdateAsync(_dealId, marketCapRate: 6.0m);
        var updated = await _service.CreateOrUpdateAsync(_dealId, bov: 1200000m, marketCapRate: 5.5m);

        Assert.Equal(first.Id, updated.Id);
        Assert.Equal(1200000m, updated.BrokerOpinionOfValue);
        Assert.Equal(5.5m, updated.CurrentMarketCapRate);
    }

    [Fact]
    public async Task CreateOrUpdate_CalculatesImpliedValue()
    {
        SeedActuals();
        var analysis = await _service.CreateOrUpdateAsync(_dealId, marketCapRate: 6.0m);

        // Implied value = T12 NOI / (cap rate / 100)
        var expectedImplied = analysis.TrailingTwelveMonthNoi / 0.06m;
        Assert.Equal(expectedImplied, analysis.ImpliedValue);
    }

    [Fact]
    public async Task GetAnalysis_ReturnsNull_WhenNoneExists()
    {
        var result = await _service.GetAnalysisAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task SellScenario_CalculatesCorrectly()
    {
        SeedActuals();
        var scenario = await _service.CalculateSellScenarioAsync(_dealId, salePrice: 1200000m, sellingCostPercent: 3m);

        Assert.Equal(1200000m, scenario.EstimatedSalePrice);
        Assert.Equal(36000m, scenario.SellingCosts); // 3% of 1.2M
        Assert.Equal(1200000m - 36000m - 700000m, scenario.NetProceeds); // sale - costs - loan
        Assert.True(scenario.EquityMultiple > 0);
        Assert.True(scenario.HoldPeriodMonths > 0);
    }

    [Fact]
    public async Task HoldScenario_ProjectsFutureNoi()
    {
        SeedActuals();
        var scenario = await _service.CalculateHoldScenarioAsync(_dealId, additionalYears: 5, noiGrowthRate: 3m);

        Assert.Equal(5, scenario.AdditionalYears);
        Assert.True(scenario.ProjectedAnnualNoi > 0);
        Assert.True(scenario.ProjectedExitValue > 0);
    }

    [Fact]
    public async Task HoldScenario_WithNoActuals_UsesProjectedNoi()
    {
        // No actuals seeded — should fall back to CalculationResult.NOI
        var scenario = await _service.CalculateHoldScenarioAsync(_dealId, additionalYears: 3, noiGrowthRate: 2m);

        Assert.True(scenario.ProjectedAnnualNoi > 0);
    }

    [Fact]
    public async Task RefinanceScenario_CalculatesCashOut()
    {
        SeedActuals();
        var scenario = await _service.CalculateRefinanceScenarioAsync(_dealId, newLoanAmount: 850000m, newRate: 6.0m);

        Assert.Equal(850000m, scenario.NewLoanAmount);
        Assert.Equal(700000m, scenario.CurrentLoanBalance);
        Assert.Equal(150000m, scenario.CashOutAmount); // 850K - 700K
        Assert.Equal(6.0m, scenario.NewInterestRate);
        Assert.Equal(51000m, scenario.NewAnnualDebtService); // 850K × 6%
    }

    [Fact]
    public async Task RefinanceScenario_NoCashOut_WhenLowerLoan()
    {
        SeedActuals();
        var scenario = await _service.CalculateRefinanceScenarioAsync(_dealId, newLoanAmount: 600000m, newRate: 5.5m);

        Assert.Equal(0m, scenario.CashOutAmount);
    }

    [Fact]
    public async Task DeleteAnalysis_Removes()
    {
        SeedActuals();
        await _service.CreateOrUpdateAsync(_dealId, marketCapRate: 6.0m);

        await _service.DeleteAnalysisAsync(_dealId);

        var result = await _service.GetAnalysisAsync(_dealId);
        Assert.Null(result);
    }

    [Fact]
    public async Task DispositionAnalysis_RecalculateImpliedValue()
    {
        var analysis = new DispositionAnalysis(_dealId)
        {
            TrailingTwelveMonthNoi = 120000m,
            CurrentMarketCapRate = 6m
        };

        analysis.RecalculateImpliedValue();

        Assert.Equal(2000000m, analysis.ImpliedValue);
    }

    [Fact]
    public async Task DispositionAnalysis_ZeroCapRate_ImpliedValueIsZero()
    {
        var analysis = new DispositionAnalysis(_dealId)
        {
            TrailingTwelveMonthNoi = 120000m,
            CurrentMarketCapRate = 0m
        };

        analysis.RecalculateImpliedValue();

        Assert.Equal(0m, analysis.ImpliedValue);
    }
}
