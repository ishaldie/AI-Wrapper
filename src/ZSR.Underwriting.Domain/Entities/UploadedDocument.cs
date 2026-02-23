using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class UploadedDocument
{
    public Guid Id { get; private set; }
    public Guid DealId { get; private set; }
    public string FileName { get; private set; }
    public string StoredPath { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public long FileSize { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public VirusScanStatus VirusScanStatus { get; set; }
    public string? FileHash { get; set; }

    public Deal Deal { get; set; } = null!;

    // EF Core parameterless constructor
    private UploadedDocument()
    {
        FileName = string.Empty;
        StoredPath = string.Empty;
    }

    public UploadedDocument(Guid dealId, string fileName, string storedPath, DocumentType documentType, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(storedPath))
            throw new ArgumentException("Stored path cannot be empty.", nameof(storedPath));
        if (fileSize <= 0)
            throw new ArgumentException("File size must be positive.", nameof(fileSize));

        Id = Guid.NewGuid();
        DealId = dealId;
        FileName = fileName;
        StoredPath = storedPath;
        DocumentType = documentType;
        FileSize = fileSize;
        UploadedAt = DateTime.UtcNow;
        VirusScanStatus = VirusScanStatus.Pending;
    }
}
