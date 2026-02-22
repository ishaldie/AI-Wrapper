using Microsoft.EntityFrameworkCore;
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

    private async Task<Guid> SeedDealAsync(string userId = "test-user")
    {
        var deal = new Deal("Test Property", userId);
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();
        return deal.Id;
    }
}
