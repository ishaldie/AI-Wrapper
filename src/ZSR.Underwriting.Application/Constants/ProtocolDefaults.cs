using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Constants;

public static class ProtocolDefaults
{
    public const decimal LoanLtv = 65m;
    public const int HoldPeriodYears = 5;
    public const decimal TargetOccupancy = 95m;
    public const int AmortizationYears = 30;
    public const int LoanTermYears = 5;

    public static decimal GetEffectiveLtv(decimal? input) => input ?? LoanLtv;
    public static int GetEffectiveHoldPeriod(int? input) => input ?? HoldPeriodYears;
    public static decimal GetEffectiveOccupancy(decimal? input) => input ?? TargetOccupancy;
    public static int GetEffectiveAmortization(int? input) => input ?? AmortizationYears;
    public static int GetEffectiveLoanTerm(int? input) => input ?? LoanTermYears;

    // Type-aware occupancy defaults
    public static decimal GetEffectiveOccupancy(decimal? input, PropertyType type) => input ?? type switch
    {
        PropertyType.AssistedLiving => 87m,
        PropertyType.SkilledNursing => 82m,
        PropertyType.MemoryCare => 85m,
        PropertyType.CCRC => 90m,
        _ => TargetOccupancy // 95% for Multifamily
    };

    // Type-aware operating expense ratio
    public static decimal GetEffectiveOpExRatio(PropertyType type) => type switch
    {
        PropertyType.AssistedLiving => 0.68m,
        PropertyType.SkilledNursing => 0.75m,
        PropertyType.MemoryCare => 0.70m,
        PropertyType.CCRC => 0.65m,
        _ => 0.5435m // Multifamily
    };

    // Type-aware other income ratio
    public static decimal GetEffectiveOtherIncomeRatio(PropertyType type) => type switch
    {
        PropertyType.AssistedLiving or PropertyType.SkilledNursing
            or PropertyType.MemoryCare or PropertyType.CCRC => 0.05m,
        _ => 0.135m // Multifamily
    };

    public static bool IsSeniorHousing(PropertyType type) =>
        type != PropertyType.Multifamily;
}
