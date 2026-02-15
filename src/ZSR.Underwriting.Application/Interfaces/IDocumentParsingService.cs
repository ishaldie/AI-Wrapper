using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IDocumentParsingService
{
    Task<ParsedDocumentResult> ParseDocumentAsync(Guid documentId, CancellationToken ct = default);
}
