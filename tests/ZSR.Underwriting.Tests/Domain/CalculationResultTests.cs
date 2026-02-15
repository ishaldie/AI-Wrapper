using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Domain;

public class CalculationResultTests
{
    [Fact]
    public void New_CalculationResult_Has_NonEmpty_Id()
    {
        var result = new CalculationResult(Guid.NewGuid());
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public void New_CalculationResult_Sets_DealId()
    {
        var dealId = Guid.NewGuid();
        var result = new CalculationResult(dealId);
        Assert.Equal(dealId, result.DealId);
    }

    [Fact]
    public void New_CalculationResult_Sets_CalculatedAt()
    {
        var before = DateTime.UtcNow;
        var result = new CalculationResult(Guid.NewGuid());
        var after = DateTime.UtcNow;
        Assert.InRange(result.CalculatedAt, before, after);
    }

    [Fact]
    public void Constructor_Throws_When_DealId_Empty()
    {
        Assert.Throws<ArgumentException>(() => new CalculationResult(Guid.Empty));
    }

    [Fact]
    public void Revenue_Fields_Default_To_Null()
    {
        var result = new CalculationResult(Guid.NewGuid());
        Assert.Null(result.GrossPotentialRent);
        Assert.Null(result.VacancyLoss);
        Assert.Null(result.EffectiveGrossIncome);
        Assert.Null(result.OtherIncome);
        Assert.Null(result.OperatingExpenses);
        Assert.Null(result.NetOperatingIncome);
        Assert.Null(result.NoiMargin);
    }

    [Fact]
    public void Return_Fields_Default_To_Null()
    {
        var result = new CalculationResult(Guid.NewGuid());
        Assert.Null(result.CashOnCashReturn);
        Assert.Null(result.InternalRateOfReturn);
        Assert.Null(result.EquityMultiple);
        Assert.Null(result.ExitValue);
        Assert.Null(result.TotalProfit);
    }

    [Fact]
    public void Debt_Fields_Default_To_Null()
    {
        var result = new CalculationResult(Guid.NewGuid());
        Assert.Null(result.LoanAmount);
        Assert.Null(result.AnnualDebtService);
        Assert.Null(result.DebtServiceCoverageRatio);
    }

    [Fact]
    public void Can_Set_Metrics()
    {
        var result = new CalculationResult(Guid.NewGuid());
        result.NetOperatingIncome = 693_872.35m;
        result.GoingInCapRate = 0.0555m;
        result.InternalRateOfReturn = 0.182m;
        result.DebtServiceCoverageRatio = 1.35m;

        Assert.Equal(693_872.35m, result.NetOperatingIncome);
        Assert.Equal(0.0555m, result.GoingInCapRate);
        Assert.Equal(0.182m, result.InternalRateOfReturn);
        Assert.Equal(1.35m, result.DebtServiceCoverageRatio);
    }

    [Fact]
    public void Json_Fields_Default_To_Null()
    {
        var result = new CalculationResult(Guid.NewGuid());
        Assert.Null(result.CashFlowProjectionsJson);
        Assert.Null(result.SensitivityAnalysisJson);
    }
}
