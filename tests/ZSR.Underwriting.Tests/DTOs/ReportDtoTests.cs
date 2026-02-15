using ZSR.Underwriting.Application.DTOs.Report;

namespace ZSR.Underwriting.Tests.DTOs;

public class ReportDtoTests
{
    [Fact]
    public void UnderwritingReportDto_GetSectionsInOrder_Returns10Sections()
    {
        var report = new UnderwritingReportDto();
        var sections = report.GetSectionsInOrder();
        Assert.Equal(10, sections.Count);
    }

    [Fact]
    public void UnderwritingReportDto_GetSectionsInOrder_SectionNumbersAre1Through10()
    {
        var report = new UnderwritingReportDto();
        var sections = report.GetSectionsInOrder();
        for (int i = 0; i < 10; i++)
            Assert.Equal(i + 1, sections[i].SectionNumber);
    }

    [Fact]
    public void UnderwritingReportDto_GetSectionsInOrder_CorrectTitles()
    {
        var report = new UnderwritingReportDto();
        var sections = report.GetSectionsInOrder();
        var expectedTitles = new[]
        {
            "Core Investment Metrics", "Executive Summary", "Underwriting Assumptions",
            "Property & Sales Comparables", "Tenant & Market Intelligence", "Operations T12 P&L",
            "Financial Analysis", "Value Creation Strategy", "Risk Assessment", "Investment Decision"
        };
        for (int i = 0; i < 10; i++)
            Assert.Equal(expectedTitles[i], sections[i].Title);
    }

    [Fact]
    public void UnderwritingReportDto_GetSectionsInOrder_CorrectConcreteTypes()
    {
        var report = new UnderwritingReportDto();
        var sections = report.GetSectionsInOrder();
        Assert.IsType<CoreMetricsSection>(sections[0]);
        Assert.IsType<ExecutiveSummarySection>(sections[1]);
        Assert.IsType<AssumptionsSection>(sections[2]);
        Assert.IsType<PropertyCompsSection>(sections[3]);
        Assert.IsType<TenantMarketSection>(sections[4]);
        Assert.IsType<OperationsSection>(sections[5]);
        Assert.IsType<FinancialAnalysisSection>(sections[6]);
        Assert.IsType<ValueCreationSection>(sections[7]);
        Assert.IsType<RiskAssessmentSection>(sections[8]);
        Assert.IsType<InvestmentDecisionSection>(sections[9]);
    }

    [Fact]
    public void RiskSeverity_HasExactlyThreeValues()
    {
        Assert.Equal(3, Enum.GetValues<RiskSeverity>().Length);
    }

    [Fact]
    public void DataSource_HasExpectedValues()
    {
        var values = Enum.GetValues<DataSource>();
        Assert.Equal(6, values.Length);
    }
}
