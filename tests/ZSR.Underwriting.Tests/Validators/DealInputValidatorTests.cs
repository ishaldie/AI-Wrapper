using FluentValidation.TestHelper;
using Xunit;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Validators;

namespace ZSR.Underwriting.Tests.Validators;

public class DealInputValidatorTests
{
    private readonly DealInputValidator _validator = new();

    private static DealInputDto ValidDeal() => new()
    {
        PropertyName = "Sunset Apartments",
        Address = "123 Main St, Austin TX",
        UnitCount = 50,
        PurchasePrice = 5_000_000m
    };

    // --- Required field tests ---

    [Fact]
    public void Valid_Deal_Passes()
    {
        var result = _validator.TestValidate(ValidDeal());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PropertyName_Required(string? name)
    {
        var deal = ValidDeal();
        deal.PropertyName = name!;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.PropertyName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Address_Required(string? address)
    {
        var deal = ValidDeal();
        deal.Address = address!;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.Address);
    }

    [Fact]
    public void UnitCount_Required()
    {
        var deal = ValidDeal();
        deal.UnitCount = null;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.UnitCount);
    }

    [Fact]
    public void UnitCount_MustBePositive()
    {
        var deal = ValidDeal();
        deal.UnitCount = 0;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.UnitCount);
    }

    [Fact]
    public void PurchasePrice_Required()
    {
        var deal = ValidDeal();
        deal.PurchasePrice = null;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.PurchasePrice);
    }

    [Fact]
    public void PurchasePrice_MustBePositive()
    {
        var deal = ValidDeal();
        deal.PurchasePrice = 0;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.PurchasePrice);
    }

    // --- Optional field range tests ---

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void LoanLtv_OutOfRange_Fails(decimal ltv)
    {
        var deal = ValidDeal();
        deal.LoanLtv = ltv;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.LoanLtv);
    }

    [Fact]
    public void LoanLtv_InRange_Passes()
    {
        var deal = ValidDeal();
        deal.LoanLtv = 65;
        var result = _validator.TestValidate(deal);
        result.ShouldNotHaveValidationErrorFor(x => x.LoanLtv);
    }

    [Fact]
    public void LoanLtv_Null_Passes()
    {
        var deal = ValidDeal();
        deal.LoanLtv = null;
        var result = _validator.TestValidate(deal);
        result.ShouldNotHaveValidationErrorFor(x => x.LoanLtv);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void LoanRate_OutOfRange_Fails(decimal rate)
    {
        var deal = ValidDeal();
        deal.LoanRate = rate;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.LoanRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(41)]
    public void HoldPeriod_OutOfRange_Fails(int years)
    {
        var deal = ValidDeal();
        deal.HoldPeriodYears = years;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.HoldPeriodYears);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void TargetOccupancy_OutOfRange_Fails(int pct)
    {
        var deal = ValidDeal();
        deal.TargetOccupancy = pct;
        var result = _validator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.TargetOccupancy);
    }

    [Fact]
    public void Optional_Fields_Null_AllPass()
    {
        var deal = ValidDeal();
        // All optional fields are null by default
        var result = _validator.TestValidate(deal);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
