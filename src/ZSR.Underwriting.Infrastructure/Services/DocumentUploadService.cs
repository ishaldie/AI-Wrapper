using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Application.Constants;
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
    private readonly IFileContentValidator? _contentValidator;
    private readonly IVirusScanService? _virusScanner;
    private readonly ILogger<DocumentUploadService>? _logger;

    public DocumentUploadService(
        AppDbContext db,
        IFileStorageService storage,
        IFileContentValidator? contentValidator = null,
        IVirusScanService? virusScanner = null,
        ILogger<DocumentUploadService>? logger = null)
    {
        _db = db;
        _storage = storage;
        _contentValidator = contentValidator;
        _virusScanner = virusScanner;
        _logger = logger;
    }

    public async Task<FileUploadResultDto> UploadDocumentAsync(
        Guid dealId, Stream fileStream, string fileName,
        DocumentType documentType, string userId, CancellationToken ct = default)
    {
        await VerifyDealOwnershipAsync(dealId, userId, ct);

        // Sanitize filename to prevent path traversal
        fileName = Path.GetFileName(fileName);

        // Validate extension
        if (!FileUploadConstants.IsValidExtension(fileName))
        {
            _logger?.LogWarning("Upload rejected: disallowed extension {Extension} by user {UserId} for deal {DealId}",
                Path.GetExtension(fileName), userId, dealId);
            throw new InvalidOperationException($"File extension is not allowed: {Path.GetExtension(fileName)}");
        }

        // Validate file content matches extension (magic bytes)
        if (_contentValidator != null)
        {
            var extension = Path.GetExtension(fileName);
            var validation = await _contentValidator.ValidateAsync(fileStream, extension, ct);
            if (!validation.IsValid)
            {
                _logger?.LogWarning("Upload rejected: content mismatch for {FileName} by user {UserId} for deal {DealId}: {Error}",
                    fileName, userId, dealId, validation.ErrorMessage);
                throw new InvalidOperationException($"File content validation failed: {validation.ErrorMessage}");
            }
        }

        // Compute SHA-256 hash before saving
        var fileHash = await ComputeSha256Async(fileStream, ct);

        // Virus scan before persisting
        var scanStatus = VirusScanStatus.Pending;
        if (_virusScanner != null)
        {
            var scanResult = await _virusScanner.ScanAsync(fileStream, ct);
            scanStatus = scanResult.Status;

            if (scanResult.Status == VirusScanStatus.Infected)
            {
                _logger?.LogWarning("Upload rejected: malware detected in {FileName} by user {UserId} for deal {DealId}, threat: {ThreatName}",
                    fileName, userId, dealId, scanResult.ThreatName);
                throw new InvalidOperationException($"File rejected: malware detected ({scanResult.ThreatName}).");
            }

            if (scanResult.Status == VirusScanStatus.ScanFailed)
            {
                _logger?.LogWarning("Virus scan failed for {FileName} by user {UserId} for deal {DealId}: {Error}",
                    fileName, userId, dealId, scanResult.ThreatName);
            }

            _logger?.LogInformation("Virus scan complete for {FileName}: {ScanStatus}", fileName, scanStatus);
        }

        var storedPath = await _storage.SaveFileAsync(fileStream, fileName, $"deals/{dealId}", ct);

        var doc = new UploadedDocument(dealId, fileName, storedPath, documentType, fileStream.Length);
        doc.FileHash = fileHash;
        doc.VirusScanStatus = scanStatus;
        doc.UploadedByUserId = userId;

        _db.UploadedDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);

        _logger?.LogInformation("Document uploaded: {DocumentId} ({FileName}, {FileSize} bytes) by user {UserId} for deal {DealId}, hash: {FileHash}",
            doc.Id, fileName, fileStream.Length, userId, dealId, fileHash);

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

        _logger?.LogInformation("Document deleted: {DocumentId} ({FileName}) by user {UserId}", documentId, doc.FileName, userId);
    }

    private async Task VerifyDealOwnershipAsync(Guid dealId, string userId, CancellationToken ct)
    {
        var deal = await _db.Deals.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dealId, ct);

        if (deal is null || deal.UserId != userId)
        {
            _logger?.LogWarning("Access denied: user {UserId} attempted to access deal {DealId}", userId, dealId);
            throw new UnauthorizedAccessException($"User does not have access to deal {dealId}.");
        }
    }

    private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
    {
        var originalPosition = stream.Position;
        stream.Position = 0;
        var hashBytes = await SHA256.HashDataAsync(stream, ct);
        stream.Position = originalPosition;
        return Convert.ToHexStringLower(hashBytes);
    }
}
