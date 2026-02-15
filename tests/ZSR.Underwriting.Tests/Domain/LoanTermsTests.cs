using ZSR.Underwriting.Domain.ValueObjects;

namespace ZSR.Underwriting.Tests.Domain;

public class LoanTermsTests
{
    [Fact]
    public void Constructor_Sets_All_Properties()
    {
        var lt = new LoanTerms(65m, 5.5m, false, 30, 10, 3_250_000m);

        Assert.Equal(65m, lt.LtvPercent);
        Assert.Equal(5.5m, lt.InterestRate);
        Assert.False(lt.IsInterestOnly);
        Assert.Equal(30, lt.AmortizationYears);
        Assert.Equal(10, lt.LoanTermYears);
        Assert.Equal(3_250_000m, lt.LoanAmount);
    }

    [Fact]
    public void Create_Calculates_LoanAmount_From_PurchasePrice()
    {
        var lt = LoanTerms.Create(5_000_000m, 65m, 5.5m, false, 30, 10);
        Assert.Equal(3_250_000m, lt.LoanAmount);
    }

    [Fact]
    public void Create_Rounds_LoanAmount_To_Two_Decimals()
    {
        // 1,000,000 * 33.33 / 100 = 333,300.00
        var lt = LoanTerms.Create(1_000_000m, 33.33m, 5m, false, 30, 10);
        Assert.Equal(333_300.00m, lt.LoanAmount);
    }

    [Fact]
    public void Value_Equality_Works()
    {
        var a = new LoanTerms(65m, 5.5m, false, 30, 10, 3_250_000m);
        var b = new LoanTerms(65m, 5.5m, false, 30, 10, 3_250_000m);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Different_Values_Are_Not_Equal()
    {
        var a = new LoanTerms(65m, 5.5m, false, 30, 10, 3_250_000m);
        var b = new LoanTerms(70m, 5.5m, false, 30, 10, 3_500_000m);
        Assert.NotEqual(a, b);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void LTV_Out_Of_Range_Throws(decimal ltv)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LoanTerms(ltv, 5m, false, 30, 10, 1_000_000m));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void InterestRate_Out_Of_Range_Throws(decimal rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LoanTerms(65m, rate, false, 30, 10, 1_000_000m));
    }

    [Fact]
    public void Zero_AmortizationYears_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LoanTerms(65m, 5m, false, 0, 10, 1_000_000m));
    }

    [Fact]
    public void Zero_LoanTermYears_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LoanTerms(65m, 5m, false, 30, 0, 1_000_000m));
    }

    [Fact]
    public void Negative_LoanAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LoanTerms(65m, 5m, false, 30, 10, -1m));
    }

    [Fact]
    public void InterestOnly_Flag_Is_Preserved()
    {
        var lt = new LoanTerms(65m, 5.5m, true, 30, 10, 3_250_000m);
        Assert.True(lt.IsInterestOnly);
    }
}
