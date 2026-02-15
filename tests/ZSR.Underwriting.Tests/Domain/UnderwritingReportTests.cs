using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Domain;

public class UnderwritingReportTests
{
    [Fact]
    public void New_Report_Has_NonEmpty_Id()
    {
        var report = new UnderwritingReport(Guid.NewGuid());
        Assert.NotEqual(Guid.Empty, report.Id);
    }

    [Fact]
    public void New_Report_Sets_DealId()
    {
        var dealId = Guid.NewGuid();
        var report = new UnderwritingReport(dealId);
        Assert.Equal(dealId, report.DealId);
    }

    [Fact]
    public void New_Report_Sets_GeneratedAt()
    {
        var before = DateTime.UtcNow;
        var report = new UnderwritingReport(Guid.NewGuid());
        var after = DateTime.UtcNow;
        Assert.InRange(report.GeneratedAt, before, after);
    }

    [Fact]
    public void Constructor_Throws_When_DealId_Empty()
    {
        Assert.Throws<ArgumentException>(() => new UnderwritingReport(Guid.Empty));
    }

    [Fact]
    public void All_Sections_Default_To_Null()
    {
        var report = new UnderwritingReport(Guid.NewGuid());
        Assert.Null(report.ExecutiveSummary);
        Assert.Null(report.PropertyOverview);
        Assert.Null(report.MarketAnalysis);
        Assert.Null(report.FinancialAnalysis);
        Assert.Null(report.RentAnalysis);
        Assert.Null(report.ExpenseAnalysis);
        Assert.Null(report.DebtAnalysis);
        Assert.Null(report.ReturnAnalysis);
        Assert.Null(report.RiskAssessment);
        Assert.Null(report.InvestmentThesis);
    }

    [Fact]
    public void Decision_Fields_Default_To_Null()
    {
        var report = new UnderwritingReport(Guid.NewGuid());
        Assert.Null(report.IsGoDecision);
        Assert.Null(report.DecisionRationale);
    }

    [Fact]
    public void Can_Set_Section_Content()
    {
        var report = new UnderwritingReport(Guid.NewGuid());
        report.ExecutiveSummary = "Strong deal with upside potential.";
        report.IsGoDecision = true;
        report.DecisionRationale = "Meets all ZSR investment criteria.";

        Assert.Equal("Strong deal with upside potential.", report.ExecutiveSummary);
        Assert.True(report.IsGoDecision);
        Assert.Equal("Meets all ZSR investment criteria.", report.DecisionRationale);
    }

    [Fact]
    public void Report_Has_Ten_Section_Properties()
    {
        var sectionProps = typeof(UnderwritingReport).GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.Name != "DecisionRationale")
            .ToList();

        // 10 sections
        Assert.Equal(10, sectionProps.Count);
    }
}
