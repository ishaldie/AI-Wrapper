using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Calculations;

public class FannieRiskRatingTests
{
    // === Task 1: Product-aware RateDscr ===

    [Fact]
    public void RateDscr_Legacy_Above125_Low()
    {
        // Legacy overload still works with hardcoded 1.25x
        Assert.Equal(RiskSeverity.Low, RiskRatingCalculator.RateDscr(1.30m));
    }

    [Fact]
    public void RateDscr_ProductAware_AtMin_Low()
    {
        // Seniors AL min = 1.40x; DSCR at 1.40 → Low
        Assert.Equal(RiskSeverity.Low, RiskRatingCalculator.RateDscr(1.40m, 1.40m));
    }

    [Fact]
    public void RateDscr_ProductAware_AboveMin_Low()
    {
        Assert.Equal(RiskSeverity.Low, RiskRatingCalculator.RateDscr(1.50m, 1.25m));
    }

    [Fact]
    public void RateDscr_ProductAware_SlightlyBelowMin_Moderate()
    {
        // 1.20 is 4% below 1.25 min (within 0-8% band) → Moderate
        Assert.Equal(RiskSeverity.Moderate, RiskRatingCalculator.RateDscr(1.20m, 1.25m));
    }

    [Fact]
    public void RateDscr_ProductAware_WellBelowMin_High()
    {
        // 1.10 is 12% below 1.25 min (within 8-20% band) → High
        Assert.Equal(RiskSeverity.High, RiskRatingCalculator.RateDscr(1.10m, 1.25m));
    }

    [Fact]
    public void RateDscr_ProductAware_FarBelowMin_Critical()
    {
        // 0.90 is 28% below 1.25 min (>20%) → Critical
        Assert.Equal(RiskSeverity.Critical, RiskRatingCalculator.RateDscr(0.90m, 1.25m));
    }

    [Fact]
    public void RateDscr_SeniorsALZ_Boundary()
    {
        // ALZ min = 1.45x
        Assert.Equal(RiskSeverity.Low, RiskRatingCalculator.RateDscr(1.45m, 1.45m));
        Assert.Equal(RiskSeverity.Moderate, RiskRatingCalculator.RateDscr(1.40m, 1.45m));
        // 1.45 * 0.80 = 1.16 → below 1.16 is Critical
        Assert.Equal(RiskSeverity.Critical, RiskRatingCalculator.RateDscr(1.15m, 1.45m));
    }

    [Fact]
    public void RateDscr_Cooperative_1_00x_Min()
    {
        // Co-op actual ops min = 1.00x
        Assert.Equal(RiskSeverity.Low, RiskRatingCalculator.RateDscr(1.00m, 1.00m));
        Assert.Equal(RiskSeverity.Moderate, RiskRatingCalculator.RateDscr(0.95m, 1.00m));
        Assert.Equal(RiskSeverity.Critical, RiskRatingCalculator.RateDscr(0.75m, 1.00m));
    }

    // === Task 2: RateSeniorsSkilledNursing ===

    [Theory]
    [InlineData(5, RiskSeverity.Low)]
    [InlineData(10, RiskSeverity.Low)]
    [InlineData(12, RiskSeverity.Moderate)]
    [InlineData(15, RiskSeverity.Moderate)]
    [InlineData(18, RiskSeverity.High)]
    [InlineData(20, RiskSeverity.High)]
    [InlineData(25, RiskSeverity.Critical)]
    public void RateSeniorsSkilledNursing_Thresholds(decimal snfPct, RiskSeverity expected)
    {
        Assert.Equal(expected, RiskRatingCalculator.RateSeniorsSkilledNursing(snfPct));
    }

    // === Task 3: RateStudentEnrollment ===

    [Theory]
    [InlineData(3000, RiskSeverity.Critical)]
    [InlineData(5000, RiskSeverity.High)]
    [InlineData(8000, RiskSeverity.High)]
    [InlineData(10000, RiskSeverity.Moderate)]
    [InlineData(12000, RiskSeverity.Moderate)]
    [InlineData(15000, RiskSeverity.Low)]
    [InlineData(30000, RiskSeverity.Low)]
    public void RateStudentEnrollment_Thresholds(int enrollment, RiskSeverity expected)
    {
        Assert.Equal(expected, RiskRatingCalculator.RateStudentEnrollment(enrollment));
    }

    // === Task 4: RateMhcTenantOccupied ===

    [Theory]
    [InlineData(20, RiskSeverity.Low)]
    [InlineData(25, RiskSeverity.Low)]
    [InlineData(30, RiskSeverity.Moderate)]
    [InlineData(35, RiskSeverity.Moderate)]
    [InlineData(40, RiskSeverity.High)]
    [InlineData(50, RiskSeverity.High)]
    [InlineData(55, RiskSeverity.Critical)]
    public void RateMhcTenantOccupied_Thresholds(decimal pct, RiskSeverity expected)
    {
        Assert.Equal(expected, RiskRatingCalculator.RateMhcTenantOccupied(pct));
    }

    // === Task 5: RateCoopSponsorConcentration ===

    [Theory]
    [InlineData(20, RiskSeverity.Low)]
    [InlineData(40, RiskSeverity.Low)]
    [InlineData(45, RiskSeverity.Moderate)]
    [InlineData(60, RiskSeverity.Moderate)]
    [InlineData(65, RiskSeverity.High)]
    public void RateCoopSponsorConcentration_Thresholds(decimal pct, RiskSeverity expected)
    {
        Assert.Equal(expected, RiskRatingCalculator.RateCoopSponsorConcentration(pct));
    }

    // === Task 6: RateAffordableSubDebt ===

    [Theory]
    [InlineData(0.90, RiskSeverity.Critical)]
    [InlineData(1.00, RiskSeverity.High)]
    [InlineData(1.03, RiskSeverity.High)]
    [InlineData(1.05, RiskSeverity.Moderate)]
    [InlineData(1.08, RiskSeverity.Moderate)]
    [InlineData(1.10, RiskSeverity.Low)]
    [InlineData(1.25, RiskSeverity.Low)]
    public void RateAffordableSubDebt_Thresholds(decimal dscr, RiskSeverity expected)
    {
        Assert.Equal(expected, RiskRatingCalculator.RateAffordableSubDebt(dscr));
    }

    // === Task 7: FannieComplianceRiskAssessment ===

    [Fact]
    public void Assess_Conventional_LowDscr_ReturnsLow()
    {
        var summary = FannieComplianceRiskAssessment.Assess(
            FannieProductType.Conventional, dscr: 1.50m, ltvPercent: 65m);

        Assert.Equal(RiskSeverity.Low, summary.OverallSeverity);
        Assert.Single(summary.Ratings);
        Assert.Equal("DSCR", summary.Ratings[0].Category);
    }

    [Fact]
    public void Assess_Conventional_LowDscr_ReturnsCritical()
    {
        var summary = FannieComplianceRiskAssessment.Assess(
            FannieProductType.Conventional, dscr: 0.90m, ltvPercent: 65m);

        Assert.Equal(RiskSeverity.Critical, summary.OverallSeverity);
    }

    [Fact]
    public void Assess_StudentHousing_IncludesEnrollmentRating()
    {
        var inputs = new FannieComplianceInputs
        {
            ProductType = FannieProductType.StudentHousing,
            NearbyEnrollment = 8_000
        };

        var summary = FannieComplianceRiskAssessment.Assess(
            FannieProductType.StudentHousing, dscr: 1.40m, ltvPercent: 65m, inputs);

        Assert.Equal(2, summary.Ratings.Count);
        var enrollRating = summary.Ratings.First(r => r.Category == "University Enrollment");
        Assert.Equal(RiskSeverity.High, enrollRating.Severity);
    }

    [Fact]
    public void Assess_MHC_IncludesTenantOccupiedRating()
    {
        var inputs = new FannieComplianceInputs
        {
            ProductType = FannieProductType.ManufacturedHousing,
            TenantOccupiedPercent = 40m
        };

        var summary = FannieComplianceRiskAssessment.Assess(
            FannieProductType.ManufacturedHousing, dscr: 1.30m, ltvPercent: 65m, inputs);

        var mhcRating = summary.Ratings.First(r => r.Category == "MHC Tenant-Occupied Homes");
        Assert.Equal(RiskSeverity.High, mhcRating.Severity);
    }

    [Fact]
    public void Assess_Cooperative_IncludesSponsorRating()
    {
        var inputs = new FannieComplianceInputs
        {
            ProductType = FannieProductType.Cooperative,
            SponsorOwnershipPercent = 50m
        };

        var summary = FannieComplianceRiskAssessment.Assess(
            FannieProductType.Cooperative, dscr: 1.10m, ltvPercent: 50m, inputs);

        var sponsorRating = summary.Ratings.First(r => r.Category == "Co-op Sponsor Concentration");
        Assert.Equal(RiskSeverity.Moderate, sponsorRating.Severity);
    }

    [Fact]
    public void Assess_Affordable_IncludesSubDebtRating()
    {
        var inputs = new FannieComplianceInputs
        {
            ProductType = FannieProductType.AffordableHousing,
            SubDebtCombinedDscr = 1.02m
        };

        var summary = FannieComplianceRiskAssessment.Assess(
            FannieProductType.AffordableHousing, dscr: 1.25m, ltvPercent: 70m, inputs);

        var subDebtRating = summary.Ratings.First(r => r.Category == "Subordinate Debt DSCR");
        Assert.Equal(RiskSeverity.High, subDebtRating.Severity);
    }

    [Fact]
    public void Assess_OverallSeverity_IsWorstOfAllRatings()
    {
        var inputs = new FannieComplianceInputs
        {
            ProductType = FannieProductType.StudentHousing,
            NearbyEnrollment = 3_000 // Critical
        };

        var summary = FannieComplianceRiskAssessment.Assess(
            FannieProductType.StudentHousing, dscr: 1.50m, ltvPercent: 65m, inputs);

        // DSCR is Low, but enrollment is Critical → overall Critical
        Assert.Equal(RiskSeverity.Critical, summary.OverallSeverity);
    }

    [Fact]
    public void Assess_ProductDisplayName_IsSet()
    {
        var summary = FannieComplianceRiskAssessment.Assess(
            FannieProductType.SeniorsAL, dscr: 1.50m, ltvPercent: 65m);

        Assert.Equal("Seniors Housing — Assisted Living", summary.ProductDisplayName);
        Assert.Equal(FannieProductType.SeniorsAL, summary.ProductType);
    }

    [Fact]
    public void Assess_NoInputs_OnlyDscrRating()
    {
        var summary = FannieComplianceRiskAssessment.Assess(
            FannieProductType.ManufacturedHousing, dscr: 1.30m, ltvPercent: 65m);

        // Without inputs, only the DSCR rating is produced
        Assert.Single(summary.Ratings);
    }
}
