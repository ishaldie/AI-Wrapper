using FluentValidation.TestHelper;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Validators;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Validators;

public class NewPropertyTypeValidatorTests
{
    private readonly DealInputValidator _dealValidator = new();
    private readonly BulkImportRowValidator _bulkValidator = new();

    // === DealInputDto.IsSeniorHousing classification ===

    [Theory]
    [InlineData(PropertyType.AssistedLiving, true)]
    [InlineData(PropertyType.SkilledNursing, true)]
    [InlineData(PropertyType.MemoryCare, true)]
    [InlineData(PropertyType.CCRC, true)]
    [InlineData(PropertyType.BoardAndCare, true)]
    [InlineData(PropertyType.IndependentLiving, true)]
    [InlineData(PropertyType.SeniorApartment, true)]
    [InlineData(PropertyType.Multifamily, false)]
    [InlineData(PropertyType.Bridge, false)]
    [InlineData(PropertyType.Hospitality, false)]
    [InlineData(PropertyType.Commercial, false)]
    [InlineData(PropertyType.LIHTC, false)]
    public void DealInputDto_IsSeniorHousing_CorrectForAllTypes(PropertyType type, bool expected)
    {
        var dto = new DealInputDto { PropertyType = type };
        Assert.Equal(expected, dto.IsSeniorHousing);
    }

    // === BulkImportRowDto.IsSeniorType classification ===

    [Theory]
    [InlineData("AssistedLiving", true)]
    [InlineData("SkilledNursing", true)]
    [InlineData("MemoryCare", true)]
    [InlineData("CCRC", true)]
    [InlineData("BoardAndCare", true)]
    [InlineData("IndependentLiving", true)]
    [InlineData("SeniorApartment", true)]
    [InlineData("Multifamily", false)]
    [InlineData("Bridge", false)]
    [InlineData("Hospitality", false)]
    [InlineData("Commercial", false)]
    [InlineData("LIHTC", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void BulkImportRowDto_IsSeniorType_CorrectForAllTypes(string? type, bool expected)
    {
        var dto = new BulkImportRowDto { PropertyType = type };
        Assert.Equal(expected, dto.IsSeniorType);
    }

    // === BulkImportRowValidator accepts all 12 property types ===

    [Theory]
    [InlineData("Multifamily")]
    [InlineData("AssistedLiving")]
    [InlineData("SkilledNursing")]
    [InlineData("MemoryCare")]
    [InlineData("CCRC")]
    [InlineData("Bridge")]
    [InlineData("Hospitality")]
    [InlineData("Commercial")]
    [InlineData("LIHTC")]
    [InlineData("BoardAndCare")]
    [InlineData("IndependentLiving")]
    [InlineData("SeniorApartment")]
    public void BulkImportValidator_AcceptsAll12PropertyTypes(string type)
    {
        var row = new BulkImportRowDto
        {
            PropertyName = "Test Property",
            Address = "123 Main St",
            PropertyType = type,
            UnitCount = 10,
            LicensedBeds = 50,
        };

        var result = _bulkValidator.TestValidate(row);
        result.ShouldNotHaveValidationErrorFor(x => x.PropertyType);
    }

    [Fact]
    public void BulkImportValidator_RejectsInvalidPropertyType()
    {
        var row = new BulkImportRowDto
        {
            PropertyName = "Test",
            Address = "123 Main St",
            PropertyType = "SomeFakeType",
        };

        var result = _bulkValidator.TestValidate(row);
        result.ShouldHaveValidationErrorFor(x => x.PropertyType);
    }

    // === DealInputValidator — non-senior, non-commercial types require UnitCount ===

    [Theory]
    [InlineData(PropertyType.Multifamily)]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.LIHTC)]
    public void DealValidator_NonSeniorNonCommercial_RequiresUnitCount(PropertyType type)
    {
        var deal = new DealInputDto
        {
            PropertyName = "Test",
            Address = "123 Main St",
            PurchasePrice = 1_000_000m,
            PropertyType = type,
            UnitCount = null,
        };

        var result = _dealValidator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.UnitCount);
    }

    [Fact]
    public void DealValidator_Commercial_DoesNotRequireUnitCount()
    {
        var deal = new DealInputDto
        {
            PropertyName = "Test Office",
            Address = "123 Main St",
            PurchasePrice = 5_000_000m,
            PropertyType = PropertyType.Commercial,
            UnitCount = null,
        };

        var result = _dealValidator.TestValidate(deal);
        result.ShouldNotHaveValidationErrorFor(x => x.UnitCount);
    }

    // === DealInputValidator — senior types require LicensedBeds ===

    [Theory]
    [InlineData(PropertyType.AssistedLiving)]
    [InlineData(PropertyType.SkilledNursing)]
    [InlineData(PropertyType.MemoryCare)]
    [InlineData(PropertyType.CCRC)]
    [InlineData(PropertyType.BoardAndCare)]
    [InlineData(PropertyType.IndependentLiving)]
    [InlineData(PropertyType.SeniorApartment)]
    public void DealValidator_SeniorTypes_RequireLicensedBeds(PropertyType type)
    {
        var deal = new DealInputDto
        {
            PropertyName = "Test Senior",
            Address = "123 Main St",
            PurchasePrice = 10_000_000m,
            PropertyType = type,
            LicensedBeds = null,
        };

        var result = _dealValidator.TestValidate(deal);
        result.ShouldHaveValidationErrorFor(x => x.LicensedBeds);
    }

    [Theory]
    [InlineData(PropertyType.Multifamily)]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    public void DealValidator_NonSeniorTypes_DoNotRequireLicensedBeds(PropertyType type)
    {
        var deal = new DealInputDto
        {
            PropertyName = "Test",
            Address = "123 Main St",
            PurchasePrice = 5_000_000m,
            PropertyType = type,
            UnitCount = 10,
        };

        var result = _dealValidator.TestValidate(deal);
        result.ShouldNotHaveValidationErrorFor(x => x.LicensedBeds);
    }

    // === DealInputDto.DetailedExpenses property ===

    [Fact]
    public void DealInputDto_DetailedExpenses_DefaultsToNull()
    {
        var dto = new DealInputDto();
        Assert.Null(dto.DetailedExpenses);
    }

    [Fact]
    public void DealInputDto_DetailedExpenses_CanBePopulated()
    {
        var dto = new DealInputDto
        {
            DetailedExpenses = new() { RealEstateTaxes = 50_000m, Insurance = 25_000m }
        };
        Assert.NotNull(dto.DetailedExpenses);
        Assert.True(dto.DetailedExpenses.HasAnyValues);
        Assert.Equal(75_000m, dto.DetailedExpenses.Total);
    }
}
