using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

/// <summary>
/// Orchestrates Claude API calls to generate all prose sections for the underwriting report.
/// </summary>
public interface IReportProseGenerator
{
    Task<GeneratedProse> GenerateAllProseAsync(ProseGenerationContext context, CancellationToken ct = default);
}
