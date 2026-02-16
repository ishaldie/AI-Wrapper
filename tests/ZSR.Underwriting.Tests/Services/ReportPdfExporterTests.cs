using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class ReportPdfExporterTests
{
    private readonly ReportPdfExporter _exporter = new();

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
            CapRate = 6.5m,
            Noi = 650_000m,
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
            KeyHighlights = ["Below market rents"],
            KeyRisks = ["Deferred maintenance"]
        },
        Assumptions = new AssumptionsSection
        {
            Assumptions =
            [
                new AssumptionRow { Parameter = "LTV", Value = "65.0%", Source = DataSource.ProtocolDefault }
            ]
        },
        Operations = new OperationsSection
        {
            RevenueItems = [new PnlRow { LineItem = "Gross Rent", Annual = 1_200_000m, PerUnit = 12_000m, PercentOfEgi = 100m }],
            ExpenseItems = [new PnlRow { LineItem = "OpEx", Annual = 550_000m, PerUnit = 5_500m, PercentOfEgi = 45.8m }],
            Noi = 650_000m,
            NoiMargin = 54.2m
        },
        RiskAssessment = new RiskAssessmentSection
        {
            Narrative = "Moderate risk profile.",
            Risks =
            [
                new RiskItem { Category = "Market", Description = "Overbuilding", Severity = RiskSeverity.Medium, Mitigation = "Monitor pipeline" }
            ]
        },
        InvestmentDecision = new InvestmentDecisionSection
        {
            Decision = InvestmentDecisionType.Go,
            DecisionLabel = "GO",
            InvestmentThesis = "Proceed with acquisition."
        }
    };

    [Fact]
    public void GeneratePdf_Returns_NonEmpty_ByteArray()
    {
        var report = CreateTestReport();
        var result = _exporter.GeneratePdf(report);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public void GeneratePdf_Starts_With_PDF_Header()
    {
        var report = CreateTestReport();
        var result = _exporter.GeneratePdf(report);

        // PDF files start with %PDF
        Assert.Equal(0x25, result[0]); // %
        Assert.Equal(0x50, result[1]); // P
        Assert.Equal(0x44, result[2]); // D
        Assert.Equal(0x46, result[3]); // F
    }

    [Fact]
    public void GeneratePdf_With_EmptyReport_DoesNotThrow()
    {
        var report = new UnderwritingReportDto();
        var result = _exporter.GeneratePdf(report);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }
}
