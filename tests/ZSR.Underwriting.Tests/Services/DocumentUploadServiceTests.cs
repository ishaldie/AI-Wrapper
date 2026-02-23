using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class DocumentUploadServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _tempDir;
    private readonly DocumentUploadService _sut;
    private readonly DocumentUploadService _sutWithValidator;

    public DocumentUploadServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _tempDir = Path.Combine(Path.GetTempPath(), "docupload_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var storage = new LocalFileStorageService(_tempDir);
        _sut = new DocumentUploadService(_db, storage);
        _sutWithValidator = new DocumentUploadService(_db, storage, new FileContentValidator());
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task UploadDocumentAsync_ReturnsResult()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await _sut.UploadDocumentAsync(dealId, stream, "test.pdf", DocumentType.RentRoll, "test-user");

        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.Equal("test.pdf", result.FileName);
        Assert.Equal("RentRoll", result.DocumentType);
    }

    [Fact]
    public async Task UploadDocumentAsync_PersistsToDatabase()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await _sut.UploadDocumentAsync(dealId, stream, "test.pdf", DocumentType.T12PAndL, "test-user");

        Assert.Equal(1, await _db.UploadedDocuments.CountAsync());
    }

    [Fact]
    public async Task UploadDocumentAsync_SavesFileToStorage()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 10, 20, 30 });

        await _sut.UploadDocumentAsync(dealId, stream, "data.xlsx", DocumentType.RentRoll, "test-user");

        // At least one file should exist in the temp dir
        var files = Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories);
        Assert.Single(files);
    }

    [Fact]
    public async Task GetDocumentsForDealAsync_ReturnsList()
    {
        var dealId = await SeedDealAsync();
        using var s1 = new MemoryStream(new byte[] { 1 });
        using var s2 = new MemoryStream(new byte[] { 2 });
        await _sut.UploadDocumentAsync(dealId, s1, "a.pdf", DocumentType.RentRoll, "test-user");
        await _sut.UploadDocumentAsync(dealId, s2, "b.csv", DocumentType.T12PAndL, "test-user");

        var docs = await _sut.GetDocumentsForDealAsync(dealId, "test-user");

        Assert.Equal(2, docs.Count);
    }

    [Fact]
    public async Task GetDocumentsForDealAsync_EmptyWhenNoDocs()
    {
        var dealId = await SeedDealAsync();

        var docs = await _sut.GetDocumentsForDealAsync(dealId, "test-user");

        Assert.Empty(docs);
    }

    [Fact]
    public async Task DeleteDocumentAsync_RemovesFromDatabase()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 1, 2 });
        var result = await _sut.UploadDocumentAsync(dealId, stream, "test.pdf", DocumentType.Appraisal, "test-user");

        await _sut.DeleteDocumentAsync(result.DocumentId, "test-user");

        Assert.Equal(0, await _db.UploadedDocuments.CountAsync());
    }

    [Fact]
    public async Task DeleteDocumentAsync_NonExistentDoesNotThrow()
    {
        await _sut.DeleteDocumentAsync(Guid.NewGuid(), "owner-user");
    }

    // --- Multi-tenant ownership verification ---

    [Fact]
    public async Task UploadDocumentAsync_Throws_When_Deal_Belongs_To_Different_User()
    {
        var dealId = await SeedDealAsync("owner-user");
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UploadDocumentAsync(dealId, stream, "test.pdf", DocumentType.RentRoll, "other-user"));
    }

    [Fact]
    public async Task UploadDocumentAsync_Succeeds_When_UserId_Matches()
    {
        var dealId = await SeedDealAsync("owner-user");
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await _sut.UploadDocumentAsync(dealId, stream, "test.pdf", DocumentType.RentRoll, "owner-user");

        Assert.NotEqual(Guid.Empty, result.DocumentId);
    }

    [Fact]
    public async Task GetDocumentsForDealAsync_Throws_When_User_Not_Owner()
    {
        var dealId = await SeedDealAsync("owner-user");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetDocumentsForDealAsync(dealId, "other-user"));
    }

    [Fact]
    public async Task DeleteDocumentAsync_Throws_When_User_Not_Owner()
    {
        var dealId = await SeedDealAsync("owner-user");
        using var stream = new MemoryStream(new byte[] { 1, 2 });
        var result = await _sut.UploadDocumentAsync(dealId, stream, "test.pdf", DocumentType.Appraisal, "owner-user");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteDocumentAsync(result.DocumentId, "other-user"));
    }

    // --- Filename sanitization ---

    [Fact]
    public async Task UploadDocumentAsync_Sanitizes_PathTraversal_Filename()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await _sut.UploadDocumentAsync(
            dealId, stream, "../../etc/passwd.csv", DocumentType.RentRoll, "test-user");

        // The stored filename should be sanitized — no path traversal
        Assert.Equal("passwd.csv", result.FileName);
    }

    [Fact]
    public async Task UploadDocumentAsync_Sanitizes_Backslash_PathTraversal()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await _sut.UploadDocumentAsync(
            dealId, stream, @"..\..\etc\passwd.csv", DocumentType.RentRoll, "test-user");

        Assert.Equal("passwd.csv", result.FileName);
    }

    // --- Content validation wiring ---

    [Fact]
    public async Task UploadDocumentAsync_WithValidator_Rejects_Mismatched_Content()
    {
        var dealId = await SeedDealAsync();
        // PNG magic bytes in a .pdf file
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sutWithValidator.UploadDocumentAsync(
                dealId, stream, "fake.pdf", DocumentType.RentRoll, "test-user"));
    }

    [Fact]
    public async Task UploadDocumentAsync_WithValidator_Accepts_Valid_Pdf()
    {
        var dealId = await SeedDealAsync();
        // PDF magic bytes
        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 });

        var result = await _sutWithValidator.UploadDocumentAsync(
            dealId, stream, "real.pdf", DocumentType.RentRoll, "test-user");

        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.Equal("real.pdf", result.FileName);
    }

    [Fact]
    public async Task UploadDocumentAsync_Rejects_Disallowed_Extension()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadDocumentAsync(
                dealId, stream, "malware.exe", DocumentType.RentRoll, "test-user"));
    }

    private async Task<Guid> SeedDealAsync(string userId = "test-user")
    {
        var deal = new Deal("Test Property", userId);
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();
        return deal.Id;
    }
}
