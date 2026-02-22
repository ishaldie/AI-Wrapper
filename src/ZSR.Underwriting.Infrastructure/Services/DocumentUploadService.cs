using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class DocumentUploadService : IDocumentUploadService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;

    public DocumentUploadService(AppDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<FileUploadResultDto> UploadDocumentAsync(
        Guid dealId, Stream fileStream, string fileName,
        DocumentType documentType, string userId, CancellationToken ct = default)
    {
        await VerifyDealOwnershipAsync(dealId, userId, ct);

        var storedPath = await _storage.SaveFileAsync(fileStream, fileName, $"deals/{dealId}", ct);

        var doc = new UploadedDocument(dealId, fileName, storedPath, documentType, fileStream.Length);
        _db.UploadedDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);

        return new FileUploadResultDto
        {
            DocumentId = doc.Id,
            FileName = doc.FileName,
            DocumentType = doc.DocumentType.ToString(),
            FileSize = doc.FileSize,
            UploadedAt = doc.UploadedAt
        };
    }

    public async Task<IReadOnlyList<FileUploadResultDto>> GetDocumentsForDealAsync(
        Guid dealId, string userId, CancellationToken ct = default)
    {
        await VerifyDealOwnershipAsync(dealId, userId, ct);

        return await _db.UploadedDocuments
            .Where(d => d.DealId == dealId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new FileUploadResultDto
            {
                DocumentId = d.Id,
                FileName = d.FileName,
                DocumentType = d.DocumentType.ToString(),
                FileSize = d.FileSize,
                UploadedAt = d.UploadedAt
            })
            .ToListAsync(ct);
    }

    public async Task DeleteDocumentAsync(Guid documentId, string userId, CancellationToken ct = default)
    {
        var doc = await _db.UploadedDocuments.FindAsync(new object[] { documentId }, ct);
        if (doc is null) return;

        await VerifyDealOwnershipAsync(doc.DealId, userId, ct);

        await _storage.DeleteFileAsync(doc.StoredPath, ct);
        _db.UploadedDocuments.Remove(doc);
        await _db.SaveChangesAsync(ct);
    }

    private async Task VerifyDealOwnershipAsync(Guid dealId, string userId, CancellationToken ct)
    {
        var deal = await _db.Deals.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null || deal.UserId != userId)
            throw new UnauthorizedAccessException($"User does not have access to deal {dealId}.");
    }
}
