using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IDocumentUploadService
{
    Task<FileUploadResultDto> UploadDocumentAsync(Guid dealId, Stream fileStream, string fileName, DocumentType documentType, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<FileUploadResultDto>> GetDocumentsForDealAsync(Guid dealId, string userId, CancellationToken ct = default);
    Task DeleteDocumentAsync(Guid documentId, string userId, CancellationToken ct = default);
}
