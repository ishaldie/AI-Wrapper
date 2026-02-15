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
}
