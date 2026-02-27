using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.DTOs;

public class ComparisonResult
{
    public required IReadOnlyList<SecuritizationComp> Comps { get; init; }
    public int TotalCompsFound { get; init; }

    // User's deal metrics for side-by-side comparison
    public decimal? UserDSCR { get; init; }
    public decimal? UserLTV { get; init; }
    public decimal? UserCapRate { get; init; }
    public decimal? UserOccupancy { get; init; }
    public decimal? UserInterestRate { get; init; }

    // Market aggregates
    public decimal? MedianDSCR { get; init; }
    public decimal? MedianLTV { get; init; }
    public decimal? MedianCapRate { get; init; }
    public decimal? MedianOccupancy { get; init; }
    public decimal? MedianInterestRate { get; init; }

    public decimal? MinDSCR { get; init; }
    public decimal? MaxDSCR { get; init; }
    public decimal? MinLTV { get; init; }
    public decimal? MaxLTV { get; init; }
    public decimal? MinCapRate { get; init; }
    public decimal? MaxCapRate { get; init; }
    public decimal? MinOccupancy { get; init; }
    public decimal? MaxOccupancy { get; init; }
    public decimal? MinInterestRate { get; init; }
    public decimal? MaxInterestRate { get; init; }
}
