using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IBulkImportService
{
    Task<List<BulkImportRowDto>> ParseFileAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    Task<BulkImportResultDto> ImportAsync(List<BulkImportRowDto> rows, string portfolioName, string userId, IProgress<int>? progress = null, CancellationToken ct = default);
}
