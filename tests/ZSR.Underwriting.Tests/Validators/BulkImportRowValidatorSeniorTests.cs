using FluentValidation.TestHelper;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Validators;

namespace ZSR.Underwriting.Tests.Validators;

public class BulkImportRowValidatorSeniorTests
{
    private readonly BulkImportRowValidator _validator = new();

    [Fact]
    public void SeniorType_RequiresLicensedBeds()
    {
        var row = new BulkImportRowDto
        {
            PropertyName = "Test Facility",
            Address = "123 Main St",
            PropertyType = "AssistedLiving",
            LicensedBeds = null,
        };

        var result = _validator.TestValidate(row);
        result.ShouldHaveValidationErrorFor(x => x.LicensedBeds);
    }

    [Fact]
    public void SeniorType_ValidLicensedBeds_Passes()
    {
        var row = new BulkImportRowDto
        {
            PropertyName = "Test Facility",
            Address = "123 Main St",
            PropertyType = "SkilledNursing",
            LicensedBeds = 120,
        };

        var result = _validator.TestValidate(row);
        result.ShouldNotHaveValidationErrorFor(x => x.LicensedBeds);
    }

    [Fact]
    public void SeniorType_ZeroBeds_Fails()
    {
        var row = new BulkImportRowDto
        {
            PropertyName = "Test Facility",
            Address = "123 Main St",
            PropertyType = "MemoryCare",
            LicensedBeds = 0,
        };

        var result = _validator.TestValidate(row);
        result.ShouldHaveValidationErrorFor(x => x.LicensedBeds);
    }

    [Fact]
    public void InvalidPropertyType_Fails()
    {
        var row = new BulkImportRowDto
        {
            PropertyName = "Test",
            Address = "123 Main St",
            PropertyType = "InvalidType",
        };

        var result = _validator.TestValidate(row);
        result.ShouldHaveValidationErrorFor(x => x.PropertyType);
    }

    [Fact]
    public void Multifamily_DoesNotRequireBeds()
    {
        var row = new BulkImportRowDto
        {
            PropertyName = "Sunset Apartments",
            Address = "123 Main St",
            PropertyType = "Multifamily",
        };

        var result = _validator.TestValidate(row);
        result.ShouldNotHaveValidationErrorFor(x => x.LicensedBeds);
    }

    [Fact]
    public void NullPropertyType_TreatedAsMultifamily()
    {
        var row = new BulkImportRowDto
        {
            PropertyName = "Sunset Apartments",
            Address = "123 Main St",
            PropertyType = null,
        };

        var result = _validator.TestValidate(row);
        result.ShouldNotHaveValidationErrorFor(x => x.LicensedBeds);
        result.ShouldNotHaveValidationErrorFor(x => x.PropertyType);
    }

    [Fact]
    public void PrivatePayPct_ValidRange_Passes()
    {
        var row = new BulkImportRowDto
        {
            PropertyName = "Test Facility",
            Address = "123 Main St",
            PropertyType = "AssistedLiving",
            LicensedBeds = 80,
            PrivatePayPct = 65m,
        };

        var result = _validator.TestValidate(row);
        result.ShouldNotHaveValidationErrorFor(x => x.PrivatePayPct);
    }
}
