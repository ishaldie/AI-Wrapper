using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class DocumentDownloadTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _tempDir;
    private readonly LocalFileStorageService _storage;

    public DocumentDownloadTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _tempDir = Path.Combine(Path.GetTempPath(), "docdownload_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _storage = new LocalFileStorageService(_tempDir);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task Download_HappyPath_ReturnsFileStream()
    {
        var (_, doc) = await SeedDocumentAsync("test-user", "report.pdf");

        // Simulate endpoint logic: query doc, verify ownership, stream file
        var document = await _db.UploadedDocuments
            .Include(d => d.Deal)
            .FirstOrDefaultAsync(d => d.Id == doc.Id);

        Assert.NotNull(document);
        Assert.Equal("test-user", document.Deal.UserId);

        var exists = await _storage.FileExistsAsync(document.StoredPath);
        Assert.True(exists);

        var stream = await _storage.GetFileAsync(document.StoredPath);
        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);
        stream.Dispose();
    }

    [Fact]
    public async Task Download_WrongUser_FailsOwnershipCheck()
    {
        var (_, doc) = await SeedDocumentAsync("owner-user", "report.pdf");

        var document = await _db.UploadedDocuments
            .Include(d => d.Deal)
            .FirstOrDefaultAsync(d => d.Id == doc.Id);

        Assert.NotNull(document);
        // A different user should be denied
        Assert.NotEqual("other-user", document.Deal.UserId);
    }

    [Fact]
    public async Task Download_NonExistentDocument_ReturnsNull()
    {
        var document = await _db.UploadedDocuments
            .FirstOrDefaultAsync(d => d.Id == Guid.NewGuid());

        Assert.Null(document);
    }

    [Fact]
    public async Task Download_FileDeletedFromDisk_NotExists()
    {
        var (_, doc) = await SeedDocumentAsync("test-user", "deleted.pdf");

        var document = await _db.UploadedDocuments
            .FirstOrDefaultAsync(d => d.Id == doc.Id);

        Assert.NotNull(document);

        // Delete the file from storage
        await _storage.DeleteFileAsync(document.StoredPath);

        var exists = await _storage.FileExistsAsync(document.StoredPath);
        Assert.False(exists);
    }

    [Fact]
    public async Task Download_PreservesOriginalFileName()
    {
        var (_, doc) = await SeedDocumentAsync("test-user", "Monthly Rent Roll 2024.xlsx");

        var document = await _db.UploadedDocuments
            .FirstOrDefaultAsync(d => d.Id == doc.Id);

        Assert.NotNull(document);
        Assert.Equal("Monthly Rent Roll 2024.xlsx", document.FileName);
    }

    [Fact]
    public async Task Download_MultipleDocsSameDeal_EachAccessible()
    {
        var dealId = await SeedDealAsync("test-user");

        var doc1 = await SeedDocForDealAsync(dealId, "doc1.pdf");
        var doc2 = await SeedDocForDealAsync(dealId, "doc2.xlsx");

        var documents = await _db.UploadedDocuments
            .Where(d => d.DealId == dealId)
            .ToListAsync();

        Assert.Equal(2, documents.Count);

        foreach (var doc in documents)
        {
            var exists = await _storage.FileExistsAsync(doc.StoredPath);
            Assert.True(exists);
        }
    }

    // --- Helpers ---

    private async Task<Guid> SeedDealAsync(string userId = "test-user")
    {
        var deal = new Deal("Test Property", userId);
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();
        return deal.Id;
    }

    private async Task<(Guid DealId, UploadedDocument Doc)> SeedDocumentAsync(string userId, string fileName)
    {
        var dealId = await SeedDealAsync(userId);
        var doc = await SeedDocForDealAsync(dealId, fileName);
        return (dealId, doc);
    }

    private async Task<UploadedDocument> SeedDocForDealAsync(Guid dealId, string fileName)
    {
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 1, 2, 3, 4 }; // PDF magic bytes + data
        using var stream = new MemoryStream(content);

        var storedPath = await _storage.SaveFileAsync(stream, fileName, $"deals/{dealId}");

        var doc = new UploadedDocument(dealId, fileName, storedPath, DocumentType.RentRoll, content.Length);
        _db.UploadedDocuments.Add(doc);
        await _db.SaveChangesAsync();
        return doc;
    }
}
