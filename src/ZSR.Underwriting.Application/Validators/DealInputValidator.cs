using FluentValidation;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Validators;

public class DealInputValidator : AbstractValidator<DealInputDto>
{
    public DealInputValidator()
    {
        // Step 1: Required fields
        RuleFor(x => x.PropertyName)
            .NotEmpty().WithMessage("Property name is required.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.");

        RuleFor(x => x.UnitCount)
            .NotNull().WithMessage("Unit count is required.")
            .GreaterThan(0).WithMessage("Unit count must be greater than 0.");

        RuleFor(x => x.PurchasePrice)
            .NotNull().WithMessage("Purchase price is required.")
            .GreaterThan(0).WithMessage("Purchase price must be greater than 0.");

        // Step 2: Preferred fields (optional but validated when provided)
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

        When(x => x.AmortizationYears.HasValue, () =>
        {
            RuleFor(x => x.AmortizationYears)
                .InclusiveBetween(1, 40).WithMessage("Amortization must be between 1 and 40 years.");
        });

        When(x => x.LoanTermYears.HasValue, () =>
        {
            RuleFor(x => x.LoanTermYears)
                .InclusiveBetween(1, 40).WithMessage("Loan term must be between 1 and 40 years.");
        });

        // Step 3: Optional fields (validated when provided)
        When(x => x.HoldPeriodYears.HasValue, () =>
        {
            RuleFor(x => x.HoldPeriodYears)
                .InclusiveBetween(1, 30).WithMessage("Hold period must be between 1 and 30 years.");
        });

        When(x => x.TargetOccupancy.HasValue, () =>
        {
            RuleFor(x => x.TargetOccupancy)
                .InclusiveBetween(0, 100).WithMessage("Target occupancy must be between 0% and 100%.");
        });
    }
}
