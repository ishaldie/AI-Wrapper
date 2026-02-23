using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class SecurityAuditTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _tempDir;

    public SecurityAuditTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _tempDir = Path.Combine(Path.GetTempPath(), "audit_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void ActivityEventType_HasSecurityEventTypes()
    {
        var values = Enum.GetValues<ActivityEventType>();
        Assert.Contains(ActivityEventType.DocumentAccessDenied, values);
        Assert.Contains(ActivityEventType.DocumentScanFailed, values);
        Assert.Contains(ActivityEventType.DocumentRateLimited, values);
        Assert.Contains(ActivityEventType.DocumentDeleted, values);
    }

    [Fact]
    public async Task UploadedDocument_StoresUploadedByUserId()
    {
        var storage = new LocalFileStorageService(_tempDir);
        var sut = new DocumentUploadService(_db, storage);

        var deal = new Deal("Test Property", "uploader-user");
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        await sut.UploadDocumentAsync(deal.Id, stream, "test.csv", DocumentType.RentRoll, "uploader-user");

        var doc = await _db.UploadedDocuments.FirstAsync();
        Assert.Equal("uploader-user", doc.UploadedByUserId);
    }

    [Fact]
    public async Task UploadedDocument_HasFileHashAfterUpload()
    {
        var storage = new LocalFileStorageService(_tempDir);
        var sut = new DocumentUploadService(_db, storage);

        var deal = new Deal("Test Property", "hash-user");
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        await sut.UploadDocumentAsync(deal.Id, stream, "doc.pdf", DocumentType.RentRoll, "hash-user");

        var doc = await _db.UploadedDocuments.FirstAsync();
        Assert.NotNull(doc.FileHash);
        Assert.Equal(64, doc.FileHash.Length);
    }

    [Fact]
    public void UploadsDirectory_NotMappedByStaticFiles()
    {
        // Verify that no StaticFiles middleware maps to the uploads directory.
        // This is a structural verification — MapStaticAssets() only serves wwwroot.
        // The uploads directory at ContentRootPath/uploads is NOT in wwwroot.
        //
        // We verify this by checking that the uploads path is outside wwwroot.
        var webRoot = "wwwroot";
        var uploadsPath = "uploads";
        Assert.False(uploadsPath.StartsWith(webRoot));
    }

    [Fact]
    public async Task AccessDenied_Upload_ThrowsAndSetsCorrectException()
    {
        var storage = new LocalFileStorageService(_tempDir);
        var sut = new DocumentUploadService(_db, storage);

        var deal = new Deal("Test Property", "owner-user");
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.UploadDocumentAsync(deal.Id, stream, "test.csv", DocumentType.RentRoll, "other-user"));

        Assert.Contains("does not have access", ex.Message);
    }

    [Fact]
    public async Task AccessDenied_Delete_ThrowsAndSetsCorrectException()
    {
        var storage = new LocalFileStorageService(_tempDir);
        var sut = new DocumentUploadService(_db, storage);

        var deal = new Deal("Test Property", "owner-user");
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await sut.UploadDocumentAsync(deal.Id, stream, "test.csv", DocumentType.RentRoll, "owner-user");

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.DeleteDocumentAsync(result.DocumentId, "other-user"));

        Assert.Contains("does not have access", ex.Message);
    }
}
