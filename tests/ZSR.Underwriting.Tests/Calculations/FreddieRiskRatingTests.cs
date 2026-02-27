using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Calculations;

public class FreddieRiskRatingTests
{
    // === DSCR rating at Freddie thresholds ===

    [Fact]
    public void Dscr_Above_Minimum_Is_Low()
    {
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.Conventional, dscr: 1.35m, ltvPercent: 75m);
        var dscrRating = summary.Ratings.First(r => r.Category == "DSCR");
        Assert.Equal(RiskSeverity.Low, dscrRating.Severity);
    }

    [Fact]
    public void Dscr_Below_Minimum_Is_Moderate()
    {
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.Conventional, dscr: 1.20m, ltvPercent: 75m);
        var dscrRating = summary.Ratings.First(r => r.Category == "DSCR");
        Assert.Equal(RiskSeverity.Moderate, dscrRating.Severity);
    }

    [Fact]
    public void Dscr_Far_Below_Minimum_Is_Critical()
    {
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.Conventional, dscr: 0.90m, ltvPercent: 75m);
        var dscrRating = summary.Ratings.First(r => r.Category == "DSCR");
        Assert.Equal(RiskSeverity.Critical, dscrRating.Severity);
    }

    // === SNF concentration ===

    [Fact]
    public void Seniors_With_SnfNoi_Gets_Rating()
    {
        var inputs = new FreddieComplianceInputs
        {
            SnfNoiPercent = 18m
        };
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.SeniorsSN, dscr: 1.55m, ltvPercent: 70m, inputs: inputs);

        var snfRating = summary.Ratings.FirstOrDefault(r => r.Category == "SNF NOI Concentration");
        Assert.NotNull(snfRating);
        Assert.Equal(RiskSeverity.High, snfRating.Severity);
    }

    // === MHC rental homes ===

    [Fact]
    public void Mhc_RentalHomes_Under15_Is_Low()
    {
        Assert.Equal(RiskSeverity.Low, FreddieComplianceRiskAssessment.RateMhcRentalHomes(10m));
    }

    [Fact]
    public void Mhc_RentalHomes_Over25_Is_High()
    {
        Assert.Equal(RiskSeverity.High, FreddieComplianceRiskAssessment.RateMhcRentalHomes(30m));
    }

    [Fact]
    public void Mhc_RentalHomes_Over35_Is_Critical()
    {
        Assert.Equal(RiskSeverity.Critical, FreddieComplianceRiskAssessment.RateMhcRentalHomes(40m));
    }

    [Fact]
    public void Mhc_With_RentalHomesPercent_Gets_Rating()
    {
        var inputs = new FreddieComplianceInputs
        {
            RentalHomesPercent = 30m
        };
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.ManufacturedHousing, dscr: 1.30m, ltvPercent: 75m, inputs: inputs);

        var mhcRating = summary.Ratings.FirstOrDefault(r => r.Category == "MHC Rental Homes");
        Assert.NotNull(mhcRating);
        Assert.Equal(RiskSeverity.High, mhcRating.Severity);
    }

    // === Student enrollment ===

    [Fact]
    public void Student_Enrollment_Gets_Rating()
    {
        var inputs = new FreddieComplianceInputs
        {
            NearbyEnrollment = 8_000
        };
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.StudentHousing, dscr: 1.35m, ltvPercent: 75m, inputs: inputs);

        var enrollRating = summary.Ratings.FirstOrDefault(r => r.Category == "University Enrollment");
        Assert.NotNull(enrollRating);
        Assert.Equal(RiskSeverity.High, enrollRating.Severity);
    }

    // === Floating rate cap ===

    [Fact]
    public void FloatingRate_NoCap_HighLtv_Gets_HighRating()
    {
        var inputs = new FreddieComplianceInputs
        {
            HasRateCap = false
        };
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.FloatingRate, dscr: 1.30m, ltvPercent: 75m, inputs: inputs);

        var capRating = summary.Ratings.FirstOrDefault(r => r.Category == "Floating Rate Cap");
        Assert.NotNull(capRating);
        Assert.Equal(RiskSeverity.High, capRating.Severity);
    }

    // === Value-Add rehab ===

    [Fact]
    public void ValueAdd_RehabPeriod_Gets_Rating()
    {
        var inputs = new FreddieComplianceInputs
        {
            IsRehabPeriod = true
        };
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.ValueAdd, dscr: 1.15m, ltvPercent: 80m, inputs: inputs);

        var rehabRating = summary.Ratings.FirstOrDefault(r => r.Category == "Value-Add Rehab DSCR");
        Assert.NotNull(rehabRating);
    }

    // === Overall severity ===

    [Fact]
    public void OverallSeverity_Is_Worst_Of_All_Ratings()
    {
        var inputs = new FreddieComplianceInputs
        {
            HasRateCap = false
        };
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.FloatingRate, dscr: 1.30m, ltvPercent: 75m, inputs: inputs);

        Assert.Equal(RiskSeverity.High, summary.OverallSeverity);
    }

    [Fact]
    public void No_Inputs_Only_Dscr_Rating()
    {
        var summary = FreddieComplianceRiskAssessment.Assess(
            FreddieProductType.Conventional, dscr: 1.35m, ltvPercent: 75m);

        Assert.Single(summary.Ratings);
        Assert.Equal("DSCR", summary.Ratings[0].Category);
    }
}
