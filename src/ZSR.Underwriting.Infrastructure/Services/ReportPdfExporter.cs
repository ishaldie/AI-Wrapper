using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Formatting;
using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ReportPdfExporter : IReportPdfExporter
{
    public byte[] GeneratePdf(UnderwritingReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(h => ComposeHeader(h, report));
                page.Content().Element(c => ComposeContent(c, report));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, UnderwritingReportDto report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(report.PropertyName).Bold().FontSize(18);
                col.Item().Text(report.Address).FontSize(11).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"Generated {report.GeneratedAt:MMMM d, yyyy}").FontSize(8).FontColor(Colors.Grey.Medium);
            });
            row.ConstantItem(100).AlignRight().AlignMiddle()
                .Background(GetDecisionBgColor(report.ExecutiveSummary.Decision))
                .Padding(8)
                .Text(report.ExecutiveSummary.DecisionLabel)
                .Bold().FontSize(14).FontColor(Colors.White);
        });
    }

    private static void ComposeContent(IContainer container, UnderwritingReportDto report)
    {
        container.Column(col =>
        {
            col.Spacing(15);

            // Section 1: Core Metrics
            col.Item().Element(c => ComposeSectionHeader(c, report.CoreMetrics));
            col.Item().Element(c => ComposeMetricsTable(c, report.CoreMetrics));

            // Section 2: Executive Summary
            col.Item().Element(c => ComposeSectionHeader(c, report.ExecutiveSummary));
            col.Item().Text(report.ExecutiveSummary.Narrative);
            if (report.ExecutiveSummary.KeyHighlights.Count > 0)
            {
                col.Item().Text("Key Highlights:").Bold();
                foreach (var h in report.ExecutiveSummary.KeyHighlights)
                    col.Item().PaddingLeft(10).Text($"+ {h}");
            }
            if (report.ExecutiveSummary.KeyRisks.Count > 0)
            {
                col.Item().Text("Key Risks:").Bold();
                foreach (var r in report.ExecutiveSummary.KeyRisks)
                    col.Item().PaddingLeft(10).Text($"- {r}");
            }

            // Section 3: Assumptions
            col.Item().Element(c => ComposeSectionHeader(c, report.Assumptions));
            col.Item().Element(c => ComposeAssumptionsTable(c, report.Assumptions));

            // Section 4: Property Comps
            col.Item().Element(c => ComposeSectionHeader(c, report.PropertyComps));
            if (!string.IsNullOrEmpty(report.PropertyComps.Narrative))
                col.Item().Text(report.PropertyComps.Narrative);

            // Section 5: Tenant & Market
            col.Item().Element(c => ComposeSectionHeader(c, report.TenantMarket));
            if (!string.IsNullOrEmpty(report.TenantMarket.Narrative))
                col.Item().Text(report.TenantMarket.Narrative);

            // Section 6: Operations
            col.Item().Element(c => ComposeSectionHeader(c, report.Operations));
            col.Item().Element(c => ComposeOperationsTable(c, report.Operations));

            // Section 7: Financial Analysis
            col.Item().Element(c => ComposeSectionHeader(c, report.FinancialAnalysis));
            col.Item().Element(c => ComposeSourcesAndUses(c, report.FinancialAnalysis.SourcesAndUses));

            // Section 8: Value Creation
            col.Item().Element(c => ComposeSectionHeader(c, report.ValueCreation));
            if (!string.IsNullOrEmpty(report.ValueCreation.Narrative))
                col.Item().Text(report.ValueCreation.Narrative);

            // Section 9: Risk Assessment
            col.Item().Element(c => ComposeSectionHeader(c, report.RiskAssessment));
            if (!string.IsNullOrEmpty(report.RiskAssessment.Narrative))
                col.Item().Text(report.RiskAssessment.Narrative);
            if (report.RiskAssessment.Risks.Count > 0)
                col.Item().Element(c => ComposeRiskTable(c, report.RiskAssessment));

            // Section 10: Investment Decision
            col.Item().Element(c => ComposeSectionHeader(c, report.InvestmentDecision));
            col.Item().Text(report.InvestmentDecision.InvestmentThesis);
        });
    }

    private static void ComposeSectionHeader(IContainer container, ReportSectionBase section)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(4)
            .Text($"{section.SectionNumber}. {section.Title}").Bold().FontSize(14);
    }

    private static void ComposeMetricsTable(IContainer container, CoreMetricsSection metrics)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn(2);
                cols.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Metric").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Value").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Source").Bold();
            });

            foreach (var row in metrics.Metrics)
            {
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(row.Label);
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(row.Value).Bold();
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(row.Source.ToString()).FontSize(8);
            }
        });
    }

    private static void ComposeAssumptionsTable(IContainer container, AssumptionsSection assumptions)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn(2);
                cols.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Parameter").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Value").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Source").Bold();
            });

            foreach (var row in assumptions.Assumptions)
            {
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(row.Parameter);
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(row.Value).Bold();
                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(row.Source.ToString()).FontSize(8);
            }
        });
    }

    private static void ComposeOperationsTable(IContainer container, OperationsSection ops)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(3);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
                cols.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Line Item").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Annual").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Per Unit").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("% EGI").Bold();
            });

            foreach (var row in ops.RevenueItems)
            {
                table.Cell().Padding(3).Text(row.LineItem);
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.Currency(row.Annual));
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.CurrencyExact(row.PerUnit));
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.Percent(row.PercentOfEgi));
            }

            foreach (var row in ops.ExpenseItems)
            {
                table.Cell().Padding(3).Text(row.LineItem);
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.Currency(row.Annual));
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.CurrencyExact(row.PerUnit));
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.Percent(row.PercentOfEgi));
            }

            table.Cell().BorderTop(1).BorderColor(Colors.Black).Padding(3).Text("NOI").Bold();
            table.Cell().BorderTop(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(ProtocolFormatter.Currency(ops.Noi)).Bold();
            table.Cell().BorderTop(1).BorderColor(Colors.Black).Padding(3);
            table.Cell().BorderTop(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(ProtocolFormatter.Percent(ops.NoiMargin)).Bold();
        });
    }

    private static void ComposeSourcesAndUses(IContainer container, SourcesAndUses su)
    {
        container.Row(row =>
        {
            row.RelativeItem().Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });
                table.Header(h => { h.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).Padding(4).Text("Sources").Bold(); });
                table.Cell().Padding(3).Text("Loan");
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.Currency(su.LoanAmount));
                table.Cell().Padding(3).Text("Equity");
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.Currency(su.EquityRequired));
                table.Cell().BorderTop(1).Padding(3).Text("Total").Bold();
                table.Cell().BorderTop(1).Padding(3).AlignRight().Text(ProtocolFormatter.Currency(su.TotalSources)).Bold();
            });

            row.ConstantItem(20);

            row.RelativeItem().Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });
                table.Header(h => { h.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten3).Padding(4).Text("Uses").Bold(); });
                table.Cell().Padding(3).Text("Purchase Price");
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.Currency(su.PurchasePrice));
                table.Cell().Padding(3).Text("Closing Costs");
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.Currency(su.ClosingCosts));
                table.Cell().Padding(3).Text("CapEx Reserve");
                table.Cell().Padding(3).AlignRight().Text(ProtocolFormatter.Currency(su.CapexReserve));
                table.Cell().BorderTop(1).Padding(3).Text("Total").Bold();
                table.Cell().BorderTop(1).Padding(3).AlignRight().Text(ProtocolFormatter.Currency(su.TotalUses)).Bold();
            });
        });
    }

    private static void ComposeRiskTable(IContainer container, RiskAssessmentSection riskSection)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);
                cols.RelativeColumn(3);
                cols.RelativeColumn(1);
                cols.RelativeColumn(3);
            });

            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Category").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Description").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Severity").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Mitigation").Bold();
            });

            foreach (var risk in riskSection.Risks)
            {
                table.Cell().Padding(3).Text(risk.Category);
                table.Cell().Padding(3).Text(risk.Description);
                table.Cell().Padding(3).Background(GetSeverityBgColor(risk.Severity))
                    .Text(risk.Severity.ToString()).Bold().FontColor(Colors.White);
                table.Cell().Padding(3).Text(risk.Mitigation);
            }
        });
    }

    private static string GetDecisionBgColor(InvestmentDecisionType decision) => decision switch
    {
        InvestmentDecisionType.Go => Colors.Green.Darken2,
        InvestmentDecisionType.ConditionalGo => Colors.Orange.Darken2,
        InvestmentDecisionType.NoGo => Colors.Red.Darken2,
        _ => Colors.Grey.Darken2
    };

    private static string GetSeverityBgColor(RiskSeverity severity) => severity switch
    {
        RiskSeverity.High => Colors.Red.Darken1,
        RiskSeverity.Medium => Colors.Orange.Darken1,
        RiskSeverity.Low => Colors.Green.Darken1,
        _ => Colors.Grey.Medium
    };
}
