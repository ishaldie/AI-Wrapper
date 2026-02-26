using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IAssetReportService
{
    Task<IReadOnlyList<AssetReport>> GetReportsAsync(Guid dealId);
    Task<AssetReport?> GetReportAsync(Guid reportId);
    Task<AssetReport> GenerateMonthlyReportAsync(Guid dealId, int year, int month);
    Task<AssetReport> GenerateQuarterlyReportAsync(Guid dealId, int year, int quarter);
    Task<AssetReport> GenerateAnnualReportAsync(Guid dealId, int year);
    Task DeleteReportAsync(Guid reportId);
}
