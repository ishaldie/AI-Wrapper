using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Web.Components.Pages;

namespace ZSR.Underwriting.Tests.Components;

public class ReportViewerTests : IAsyncLifetime
{
    private readonly BunitContext _ctx;

    public ReportViewerTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.Services.AddMudServices();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _ctx.DisposeAsync();

    private static UnderwritingReportDto CreateTestReport() => new()
    {
        DealId = Guid.NewGuid(),
        PropertyName = "Sunset Apartments",
        Address = "123 Main St, Dallas, TX",
        GeneratedAt = new DateTime(2026, 2, 15),
        CoreMetrics = new CoreMetricsSection
        {
            PurchasePrice = 10_000_000m,
            UnitCount = 100,
            PricePerUnit = 100_000m,
            CapRate = 6.5m,
            Noi = 650_000m,
            LoanAmount = 6_500_000m,
            LtvPercent = 65m,
            Metrics =
            [
                new MetricRow { Label = "Purchase Price", Value = "$10,000,000", Source = DataSource.UserInput },
                new MetricRow { Label = "Units", Value = "100", Source = DataSource.UserInput }
            ]
        },
        ExecutiveSummary = new ExecutiveSummarySection
        {
            Decision = InvestmentDecisionType.Go,
            DecisionLabel = "GO",
            Narrative = "Strong investment opportunity.",
            KeyHighlights = ["Below market rents", "Strong market"],
            KeyRisks = ["Deferred maintenance"]
        },
        Assumptions = new AssumptionsSection
        {
            Assumptions =
            [
                new AssumptionRow { Parameter = "LTV", Value = "65.0%", Source = DataSource.ProtocolDefault },
                new AssumptionRow { Parameter = "Hold Period", Value = "5 years", Source = DataSource.ProtocolDefault }
            ]
        },
        InvestmentDecision = new InvestmentDecisionSection
        {
            Decision = InvestmentDecisionType.Go,
            DecisionLabel = "GO",
            InvestmentThesis = "Proceed with acquisition.",
            Conditions = ["Complete Phase I ESA"],
            NextSteps = ["Submit LOI"]
        }
    };

    [Fact]
    public void ReportViewer_Renders_PropertyName()
    {
        var report = CreateTestReport();
        _ctx.Services.AddSingleton<UnderwritingReportDto>(report);

        var cut = _ctx.Render<ReportViewer>(p => p
            .Add(x => x.Report, report));

        Assert.Contains("Sunset Apartments", cut.Markup);
    }

    [Fact]
    public void ReportViewer_Renders_Address()
    {
        var report = CreateTestReport();

        var cut = _ctx.Render<ReportViewer>(p => p
            .Add(x => x.Report, report));

        Assert.Contains("123 Main St, Dallas, TX", cut.Markup);
    }

    [Fact]
    public void ReportViewer_Renders_All10SectionTitles()
    {
        var report = CreateTestReport();

        var cut = _ctx.Render<ReportViewer>(p => p
            .Add(x => x.Report, report));

        var markup = cut.Markup;
        Assert.Contains("Core Investment Metrics", markup);
        Assert.Contains("Executive Summary", markup);
        Assert.Contains("Underwriting Assumptions", markup);
        Assert.Contains("Property &amp; Sales Comparables", markup);
        Assert.Contains("Tenant &amp; Market Intelligence", markup);
        Assert.Contains("Operations T12 P&amp;L", markup);
        Assert.Contains("Financial Analysis", markup);
        Assert.Contains("Value Creation Strategy", markup);
        Assert.Contains("Risk Assessment", markup);
        Assert.Contains("Investment Decision", markup);
    }

    [Fact]
    public void ReportViewer_Shows_DecisionBadge()
    {
        var report = CreateTestReport();

        var cut = _ctx.Render<ReportViewer>(p => p
            .Add(x => x.Report, report));

        Assert.Contains("GO", cut.Markup);
    }

    [Fact]
    public void ReportViewer_Shows_CoreMetricValues()
    {
        var report = CreateTestReport();

        var cut = _ctx.Render<ReportViewer>(p => p
            .Add(x => x.Report, report));

        var markup = cut.Markup;
        Assert.Contains("$10,000,000", markup);
        Assert.Contains("100", markup);
    }
}
