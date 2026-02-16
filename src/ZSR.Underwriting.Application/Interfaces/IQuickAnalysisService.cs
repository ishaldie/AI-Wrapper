using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IQuickAnalysisService
{
    Task<QuickAnalysisProgress> StartAnalysisAsync(string searchQuery, CancellationToken ct = default);
}
