using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.DTOs;

/// <summary>
/// Bundles all data needed for Claude AI prose generation prompts.
/// </summary>
public class ProseGenerationContext
{
    public required Deal Deal { get; init; }
    public CalculationResult? Calculations { get; init; }
    public MarketContextDto? MarketContext { get; init; }
}
