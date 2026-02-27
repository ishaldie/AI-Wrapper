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

    // === Type-aware occupancy defaults ===
    public static decimal GetEffectiveOccupancy(decimal? input, PropertyType type) => input ?? type switch
    {
        PropertyType.AssistedLiving => 87m,
        PropertyType.SkilledNursing => 82m,
        PropertyType.MemoryCare => 85m,
        PropertyType.CCRC => 90m,
        PropertyType.Bridge => 92m,
        PropertyType.Hospitality => 65m,
        PropertyType.Commercial => 93m,
        PropertyType.LIHTC => 97m,
        PropertyType.BoardAndCare => 85m,
        PropertyType.IndependentLiving => 90m,
        PropertyType.SeniorApartment => 95m,
        _ => TargetOccupancy // 95% for Multifamily
    };

    // === Type-aware operating expense ratio ===
    public static decimal GetEffectiveOpExRatio(PropertyType type) => type switch
    {
        PropertyType.AssistedLiving => 0.68m,
        PropertyType.SkilledNursing => 0.75m,
        PropertyType.MemoryCare => 0.70m,
        PropertyType.CCRC => 0.65m,
        PropertyType.Bridge => 0.50m,
        PropertyType.Hospitality => 0.62m,
        PropertyType.Commercial => 0.45m,
        PropertyType.LIHTC => 0.58m,
        PropertyType.BoardAndCare => 0.70m,
        PropertyType.IndependentLiving => 0.60m,
        PropertyType.SeniorApartment => 0.52m,
        _ => 0.5435m // Multifamily
    };

    // === Type-aware other income ratio ===
    public static decimal GetEffectiveOtherIncomeRatio(PropertyType type) => type switch
    {
        PropertyType.AssistedLiving or PropertyType.SkilledNursing
            or PropertyType.MemoryCare or PropertyType.CCRC
            or PropertyType.BoardAndCare => 0.05m,
        PropertyType.LIHTC => 0.05m,
        PropertyType.IndependentLiving => 0.08m,
        PropertyType.Bridge or PropertyType.SeniorApartment => 0.10m,
        PropertyType.Hospitality => 0.15m,
        PropertyType.Commercial => 0.03m,
        _ => 0.135m // Multifamily
    };

    // === Management fee % of EGI ===
    public static decimal GetManagementFeePct(PropertyType type) => type switch
    {
        PropertyType.Multifamily or PropertyType.Bridge => 3.5m,
        PropertyType.Hospitality => 3.0m,
        PropertyType.Commercial => 4.0m,
        PropertyType.LIHTC => 6.0m,
        PropertyType.SkilledNursing or PropertyType.AssistedLiving
            or PropertyType.MemoryCare or PropertyType.CCRC
            or PropertyType.BoardAndCare or PropertyType.IndependentLiving => 5.0m,
        PropertyType.SeniorApartment => 4.0m,
        _ => 3.5m
    };

    // === Replacement reserve per unit/bed per annum ===
    public static decimal GetReservesPupa(PropertyType type) => type switch
    {
        // HUD healthcare: $350/bed (historical guidance)
        PropertyType.SkilledNursing or PropertyType.AssistedLiving
            or PropertyType.MemoryCare or PropertyType.CCRC
            or PropertyType.BoardAndCare => 350m,
        // LIHTC and Independent Living: $300/unit
        PropertyType.LIHTC or PropertyType.IndependentLiving => 300m,
        // Standard: $250/unit
        _ => 250m
    };

    // === Minimum DSCR threshold per type ===
    public static decimal GetMinDscr(PropertyType type) => type switch
    {
        PropertyType.Bridge => 1.20m,
        PropertyType.LIHTC => 1.15m,
        PropertyType.Multifamily or PropertyType.SeniorApartment => 1.25m,
        PropertyType.Commercial => 1.30m,
        PropertyType.IndependentLiving => 1.35m,
        PropertyType.Hospitality or PropertyType.CCRC => 1.40m,
        PropertyType.SkilledNursing or PropertyType.AssistedLiving
            or PropertyType.MemoryCare or PropertyType.BoardAndCare => 1.45m,
        _ => 1.25m
    };

    // === Maximum LTV per type ===
    public static decimal GetMaxLtv(PropertyType type) => type switch
    {
        PropertyType.Hospitality => 65m,
        PropertyType.Bridge or PropertyType.Commercial => 75m,
        PropertyType.Multifamily or PropertyType.MemoryCare
            or PropertyType.CCRC or PropertyType.BoardAndCare
            or PropertyType.IndependentLiving or PropertyType.SeniorApartment => 80m,
        PropertyType.LIHTC or PropertyType.SkilledNursing
            or PropertyType.AssistedLiving => 85m,
        _ => 80m
    };

    // === Revenue growth rate default ===
    public static decimal GetRevenueGrowthRate(PropertyType type) => type switch
    {
        PropertyType.LIHTC => 0.02m, // 2% for restricted rents
        _ => 0.03m // 3% standard
    };

    // === Expense growth rates (fixed vs controllable) ===
    public const decimal FixedExpenseGrowthRate = 0.03m;     // RE Tax, Insurance
    public const decimal ControllableExpenseGrowthRate = 0.02m; // R&M, Payroll, Marketing, G&A

    // === Expense PUPA minimums (from NREC Bridge training) ===
    public static readonly IReadOnlyDictionary<string, decimal> ExpensePupaMinimums =
        new Dictionary<string, decimal>
        {
            ["RepairsAndMaintenance"] = 600m,
            ["Payroll"] = 1_000m,
            ["Marketing"] = 50m,
            ["GeneralAndAdmin"] = 250m
        };

    // === Classification helpers ===
    public static bool IsSeniorHousing(PropertyType type) => type is
        PropertyType.AssistedLiving or PropertyType.SkilledNursing
        or PropertyType.MemoryCare or PropertyType.CCRC
        or PropertyType.BoardAndCare or PropertyType.IndependentLiving
        or PropertyType.SeniorApartment;

    /// <summary>
    /// Healthcare facilities regulated by CMS / state licensing.
    /// Board & Care included (Keys Amendment regulated).
    /// </summary>
    public static bool IsHealthcare(PropertyType type) => type is
        PropertyType.SkilledNursing or PropertyType.AssistedLiving
        or PropertyType.MemoryCare or PropertyType.CCRC
        or PropertyType.BoardAndCare;
}
