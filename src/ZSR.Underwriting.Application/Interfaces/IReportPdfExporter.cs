using ZSR.Underwriting.Application.DTOs.Report;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IReportPdfExporter
{
    byte[] GeneratePdf(UnderwritingReportDto report);
}
