using FluentValidation;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Enums;

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

        // UnitCount required for non-senior, non-commercial types
        When(x => !x.IsSeniorHousing && x.PropertyType != PropertyType.Commercial, () =>
        {
            RuleFor(x => x.UnitCount)
                .NotNull().WithMessage("Unit count is required.")
                .GreaterThan(0).WithMessage("Unit count must be greater than 0.");
        });

        RuleFor(x => x.PurchasePrice)
            .NotNull().WithMessage("Purchase price is required.")
            .GreaterThan(0).WithMessage("Purchase price must be greater than 0.");

        // Senior housing conditional rules
        When(x => x.IsSeniorHousing, () =>
        {
            RuleFor(x => x.LicensedBeds)
                .NotNull().WithMessage("Licensed beds is required for senior housing.")
                .GreaterThan(0).WithMessage("Licensed beds must be greater than 0.");

            When(x => x.PrivatePayPct.HasValue || x.MedicaidPct.HasValue || x.MedicarePct.HasValue, () =>
            {
                RuleFor(x => (x.PrivatePayPct ?? 0) + (x.MedicaidPct ?? 0) + (x.MedicarePct ?? 0))
                    .InclusiveBetween(95, 105)
                    .WithMessage("Payer mix percentages must sum to approximately 100%.");
            });

            When(x => x.StaffingRatio.HasValue, () =>
            {
                RuleFor(x => x.StaffingRatio)
                    .InclusiveBetween(0, 5).WithMessage("Staffing ratio must be between 0 and 5.");
            });

            When(x => x.AverageDailyRate.HasValue, () =>
            {
                RuleFor(x => x.AverageDailyRate)
                    .GreaterThan(0).WithMessage("Average daily rate must be greater than 0.");
            });
        });

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
