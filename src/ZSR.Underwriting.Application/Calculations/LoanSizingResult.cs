namespace ZSR.Underwriting.Application.Calculations;

/// <summary>
/// Result of dual-constraint (LTV + DSCR) loan sizing.
/// MaxLoan = MIN(LtvBasedLoan, DscrBasedLoan).
/// </summary>
public record LoanSizingResult(
    decimal MaxLoan,
    decimal LtvBasedLoan,
    decimal DscrBasedLoan,
    string ConstrainingTest);
