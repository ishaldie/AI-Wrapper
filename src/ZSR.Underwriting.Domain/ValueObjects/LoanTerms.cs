namespace ZSR.Underwriting.Domain.ValueObjects;

/// <summary>
/// Value object representing loan parameters. Immutable, equality by value.
/// </summary>
public record LoanTerms
{
    public decimal LtvPercent { get; init; }
    public decimal InterestRate { get; init; }
    public bool IsInterestOnly { get; init; }
    public int AmortizationYears { get; init; }
    public int LoanTermYears { get; init; }
    public decimal LoanAmount { get; init; }

    public LoanTerms(decimal ltvPercent, decimal interestRate, bool isInterestOnly,
        int amortizationYears, int loanTermYears, decimal loanAmount)
    {
        if (ltvPercent < 0 || ltvPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(ltvPercent), "LTV must be between 0 and 100.");
        if (interestRate < 0 || interestRate > 30)
            throw new ArgumentOutOfRangeException(nameof(interestRate), "Interest rate must be between 0 and 30.");
        if (amortizationYears <= 0)
            throw new ArgumentOutOfRangeException(nameof(amortizationYears), "Amortization must be positive.");
        if (loanTermYears <= 0)
            throw new ArgumentOutOfRangeException(nameof(loanTermYears), "Loan term must be positive.");
        if (loanAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(loanAmount), "Loan amount cannot be negative.");

        LtvPercent = ltvPercent;
        InterestRate = interestRate;
        IsInterestOnly = isInterestOnly;
        AmortizationYears = amortizationYears;
        LoanTermYears = loanTermYears;
        LoanAmount = loanAmount;
    }

    /// <summary>
    /// Calculate loan amount from purchase price and LTV.
    /// </summary>
    public static LoanTerms Create(decimal purchasePrice, decimal ltvPercent, decimal interestRate,
        bool isInterestOnly, int amortizationYears, int loanTermYears)
    {
        var loanAmount = Math.Round(purchasePrice * ltvPercent / 100m, 2);
        return new LoanTerms(ltvPercent, interestRate, isInterestOnly, amortizationYears, loanTermYears, loanAmount);
    }
}
