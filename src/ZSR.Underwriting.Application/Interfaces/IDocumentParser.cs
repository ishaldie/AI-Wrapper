using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IDocumentParser
{
    DocumentType SupportedType { get; }
    bool CanParse(string fileName);
    Task<ParsedDocumentResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}
