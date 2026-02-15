using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Domain;

public class UnderwritingInputTests
{
    [Fact]
    public void New_Input_Has_NonEmpty_Id()
    {
        var input = new UnderwritingInput(5_000_000m);
        Assert.NotEqual(Guid.Empty, input.Id);
    }

    [Fact]
    public void New_Input_Sets_PurchasePrice()
    {
        var input = new UnderwritingInput(5_000_000m);
        Assert.Equal(5_000_000m, input.PurchasePrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Throws_When_PurchasePrice_Not_Positive(decimal price)
    {
        Assert.Throws<ArgumentException>(() => new UnderwritingInput(price));
    }

    [Fact]
    public void Optional_Loan_Fields_Default_To_Null()
    {
        var input = new UnderwritingInput(5_000_000m);
        Assert.Null(input.LoanLtv);
        Assert.Null(input.LoanRate);
        Assert.Null(input.AmortizationYears);
        Assert.Null(input.LoanTermYears);
        Assert.False(input.IsInterestOnly);
    }

    [Fact]
    public void Optional_Deal_Fields_Default_To_Null()
    {
        var input = new UnderwritingInput(5_000_000m);
        Assert.Null(input.HoldPeriodYears);
        Assert.Null(input.CapexBudget);
        Assert.Null(input.TargetOccupancy);
        Assert.Null(input.ValueAddPlans);
        Assert.Null(input.RentRollSummary);
        Assert.Null(input.T12Summary);
    }

    [Fact]
    public void Can_Set_Loan_Fields()
    {
        var input = new UnderwritingInput(5_000_000m);
        input.LoanLtv = 65m;
        input.LoanRate = 5.5m;
        input.IsInterestOnly = true;
        input.AmortizationYears = 30;
        input.LoanTermYears = 10;

        Assert.Equal(65m, input.LoanLtv);
        Assert.Equal(5.5m, input.LoanRate);
        Assert.True(input.IsInterestOnly);
        Assert.Equal(30, input.AmortizationYears);
        Assert.Equal(10, input.LoanTermYears);
    }

    [Fact]
    public void Can_Set_Optional_Deal_Fields()
    {
        var input = new UnderwritingInput(5_000_000m);
        input.HoldPeriodYears = 5;
        input.CapexBudget = 500_000m;
        input.TargetOccupancy = 93m;
        input.ValueAddPlans = "Interior renovations, amenity upgrades";
        input.RentRollSummary = 1_200m;
        input.T12Summary = 800_000m;

        Assert.Equal(5, input.HoldPeriodYears);
        Assert.Equal(500_000m, input.CapexBudget);
        Assert.Equal(93m, input.TargetOccupancy);
        Assert.Equal("Interior renovations, amenity upgrades", input.ValueAddPlans);
        Assert.Equal(1_200m, input.RentRollSummary);
        Assert.Equal(800_000m, input.T12Summary);
    }

    [Fact]
    public void Input_Has_DealId_For_Relationship()
    {
        var input = new UnderwritingInput(5_000_000m);
        Assert.Equal(Guid.Empty, input.DealId);
    }
}
