using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Parsing;

public class DocumentParsingService : IDocumentParsingService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IEnumerable<IDocumentParser> _parsers;

    public DocumentParsingService(AppDbContext db, IFileStorageService storage, IEnumerable<IDocumentParser> parsers)
    {
        _db = db;
        _storage = storage;
        _parsers = parsers;
    }

    public async Task<ParsedDocumentResult> ParseDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.UploadedDocuments.FindAsync(new object[] { documentId }, ct);
        if (doc is null)
            return new ParsedDocumentResult { Success = false, ErrorMessage = "Document not found." };

        var parser = _parsers.FirstOrDefault(p => p.SupportedType == doc.DocumentType && p.CanParse(doc.FileName));
        if (parser is null)
            return new ParsedDocumentResult
            {
                DocumentId = documentId,
                DocumentType = doc.DocumentType,
                Success = false,
                ErrorMessage = $"No parser available for {doc.DocumentType} files with extension {Path.GetExtension(doc.FileName)}."
            };

        using var stream = await _storage.GetFileAsync(doc.StoredPath, ct);
        var result = await parser.ParseAsync(stream, doc.FileName, ct);
        result.DocumentId = documentId;
        return result;
    }
}
