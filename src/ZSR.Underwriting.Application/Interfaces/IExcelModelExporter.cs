namespace ZSR.Underwriting.Application.Interfaces;

public interface IExcelModelExporter
{
    Task<byte[]> ExportAsync(Guid dealId, CancellationToken ct = default);
}
