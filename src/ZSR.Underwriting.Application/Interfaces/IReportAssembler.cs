using ZSR.Underwriting.Application.DTOs.Report;

namespace ZSR.Underwriting.Application.Interfaces;

/// <summary>
/// Assembles all 10 sections of the underwriting report from deal data,
/// calculated metrics, RealAI enrichment, and AI-generated prose.
/// </summary>
public interface IReportAssembler
{
    /// <summary>
    /// Assembles a complete underwriting report for the specified deal.
    /// </summary>
    Task<UnderwritingReportDto> AssembleReportAsync(Guid dealId, CancellationToken cancellationToken = default);
}
