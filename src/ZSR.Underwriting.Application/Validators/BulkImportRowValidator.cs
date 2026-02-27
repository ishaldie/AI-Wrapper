using FluentValidation;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Validators;

public class BulkImportRowValidator : AbstractValidator<BulkImportRowDto>
{
    private static readonly HashSet<string> ValidPropertyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Multifamily", "AssistedLiving", "SkilledNursing", "MemoryCare", "CCRC",
        "Bridge", "Hospitality", "Commercial", "LIHTC",
        "BoardAndCare", "IndependentLiving", "SeniorApartment"
    };

    public BulkImportRowValidator()
    {
        RuleFor(x => x.PropertyName)
            .NotEmpty().WithMessage("Property name is required.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.");

        When(x => !string.IsNullOrWhiteSpace(x.PropertyType), () =>
        {
            RuleFor(x => x.PropertyType)
                .Must(pt => ValidPropertyTypes.Contains(pt!))
                .WithMessage("Property type must be one of: " + string.Join(", ", ValidPropertyTypes) + ".");
        });

        // Senior housing: LicensedBeds required instead of UnitCount
        When(x => x.IsSeniorType, () =>
        {
            RuleFor(x => x.LicensedBeds)
                .NotNull().WithMessage("Licensed beds is required for senior housing.")
                .GreaterThan(0).WithMessage("Licensed beds must be greater than 0.");

            When(x => x.PrivatePayPct.HasValue, () =>
            {
                RuleFor(x => x.PrivatePayPct)
                    .InclusiveBetween(0, 100).WithMessage("Private pay percentage must be between 0% and 100%.");
            });
        });

        When(x => x.UnitCount.HasValue, () =>
        {
            RuleFor(x => x.UnitCount)
                .GreaterThan(0).WithMessage("Unit count must be greater than 0.");
        });

        When(x => x.PurchasePrice.HasValue, () =>
        {
            RuleFor(x => x.PurchasePrice)
                .GreaterThan(0).WithMessage("Purchase price must be greater than 0.");
        });

        When(x => x.LoanLtv.HasValue, () =>
        {
            RuleFor(x => x.LoanLtv)
                .InclusiveBetween(0, 100).WithMessage("LTV must be between 0% and 100%.");
        });

        When(x => x.LoanRate.HasValue, () =>
        {
            RuleFor(x => x.LoanRate)
                .InclusiveBetween(0, 30).WithMessage("Interest rate must be between 0% and 30%.");
        });

        When(x => x.CapexBudget.HasValue, () =>
        {
            RuleFor(x => x.CapexBudget)
                .GreaterThanOrEqualTo(0).WithMessage("CapEx budget cannot be negative.");
        });
    }
}
