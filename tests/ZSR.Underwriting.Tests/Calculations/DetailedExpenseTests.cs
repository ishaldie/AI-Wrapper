using ZSR.Underwriting.Application.Calculations;
using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Calculations;

public class DetailedExpenseTests
{
    private readonly UnderwritingCalculator _calc = new();

    // === CalculateDetailedExpenses ===

    [Fact]
    public void DetailedExpenses_SumsAllLineItems()
    {
        var expenses = new DetailedExpenses
        {
            RealEstateTaxes = 100_000m,
            Insurance = 50_000m,
            Utilities = 30_000m,
            RepairsAndMaintenance = 60_000m,
            Payroll = 80_000m,
            Marketing = 10_000m,
            GeneralAndAdmin = 25_000m,
            ManagementFee = 40_000m,
            ReplacementReserves = 20_000m,
            OtherExpenses = 5_000m,
        };

        var result = _calc.CalculateDetailedExpenses(expenses, 100, 1_000_000m, PropertyType.Multifamily);
        // Payroll $80k floored to $100k (PUPA min $1000/unit × 100), so total = 440k
        Assert.Equal(440_000m, result);
    }

    [Fact]
    public void DetailedExpenses_AppliesPupaMinimums_RepairsAndMaintenance()
    {
        // R&M minimum is $600/unit. 100 units → min $60,000.
        // If user provides $40,000 (only $400/unit), it should be floored to $60,000.
        var expenses = new DetailedExpenses
        {
            RepairsAndMaintenance = 40_000m,  // $400/unit < $600 minimum
        };

        var result = _calc.CalculateDetailedExpenses(expenses, 100, 1_000_000m, PropertyType.Multifamily);
        Assert.Equal(60_000m, result); // Floored to PUPA minimum
    }

    [Fact]
    public void DetailedExpenses_AbovePupaMinimum_NotCapped()
    {
        // R&M = $80,000 for 100 units = $800/unit > $600 minimum → use actual
        var expenses = new DetailedExpenses
        {
            RepairsAndMaintenance = 80_000m,
        };

        var result = _calc.CalculateDetailedExpenses(expenses, 100, 1_000_000m, PropertyType.Multifamily);
        Assert.Equal(80_000m, result);
    }

    [Fact]
    public void DetailedExpenses_ManagementFeePct_CalculatesFromEgi()
    {
        // ManagementFeePct = 3.5% of EGI $1,000,000 = $35,000
        var expenses = new DetailedExpenses
        {
            ManagementFeePct = 3.5m,
        };

        var result = _calc.CalculateDetailedExpenses(expenses, 100, 1_000_000m, PropertyType.Multifamily);
        Assert.Equal(35_000m, result);
    }

    [Fact]
    public void DetailedExpenses_ManagementFeePct_OverridesDollarAmount()
    {
        // When both ManagementFeePct and ManagementFee are set, % takes precedence
        var expenses = new DetailedExpenses
        {
            ManagementFeePct = 5.0m,
            ManagementFee = 99_999m, // Should be ignored
        };

        var result = _calc.CalculateDetailedExpenses(expenses, 100, 1_000_000m, PropertyType.Multifamily);
        Assert.Equal(50_000m, result); // 5% of $1M
    }

    [Fact]
    public void DetailedExpenses_NullExpenses_ReturnsZero()
    {
        var result = _calc.CalculateDetailedExpenses(null, 100, 1_000_000m, PropertyType.Multifamily);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void DetailedExpenses_EmptyExpenses_ReturnsZero()
    {
        var result = _calc.CalculateDetailedExpenses(new DetailedExpenses(), 100, 1_000_000m, PropertyType.Multifamily);
        Assert.Equal(0m, result);
    }

    // === CalculateOperatingExpenses prefers detailed when available ===

    [Fact]
    public void OperatingExpenses_WithDetailedExpenses_UsesDetailedTotal()
    {
        var expenses = new DetailedExpenses
        {
            RealEstateTaxes = 100_000m,
            Insurance = 50_000m,
            ManagementFee = 35_000m,
        };

        var detailed = _calc.CalculateDetailedExpenses(expenses, 100, 1_000_000m, PropertyType.Multifamily);
        // Should use detailed total (185,000) instead of ratio
        Assert.Equal(185_000m, detailed);

        // Verify ratio-based would give different answer
        var ratioBased = _calc.CalculateOperatingExpenses(1_000_000m, null, 0.5435m);
        Assert.NotEqual(detailed, ratioBased);
    }

    // === PUPA minimums for multiple categories ===

    [Fact]
    public void DetailedExpenses_MultipleCategories_AllPupaMinimums()
    {
        // All categories below PUPA minimum for 50 units:
        // R&M: min $600 × 50 = $30,000
        // Payroll: min $1,000 × 50 = $50,000
        // Marketing: min $50 × 50 = $2,500
        // G&A: min $250 × 50 = $12,500
        var expenses = new DetailedExpenses
        {
            RepairsAndMaintenance = 1_000m, // $20/unit < $600
            Payroll = 2_000m,               // $40/unit < $1000
            Marketing = 500m,               // $10/unit < $50
            GeneralAndAdmin = 1_000m,       // $20/unit < $250
        };

        var result = _calc.CalculateDetailedExpenses(expenses, 50, 500_000m, PropertyType.Multifamily);
        // All floored to PUPA minimums
        Assert.Equal(30_000m + 50_000m + 2_500m + 12_500m, result);
    }

    // === Type-specific management fee default ===

    [Fact]
    public void DetailedExpenses_HealthcareDefaultMgmtFee()
    {
        // ALF management fee default is 5% per ProtocolDefaults
        var expenses = new DetailedExpenses
        {
            RealEstateTaxes = 200_000m,
        };

        // When ManagementFeePct is not set, no automatic management fee is added
        // (User must explicitly provide it)
        var result = _calc.CalculateDetailedExpenses(expenses, 80, 2_000_000m, PropertyType.AssistedLiving);
        Assert.Equal(200_000m, result);
    }
}
