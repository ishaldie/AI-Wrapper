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

        var result = await _sut.UploadDocumentAsync(dealId, stream, "test.pdf", DocumentType.RentRoll);

        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.Equal("test.pdf", result.FileName);
        Assert.Equal("RentRoll", result.DocumentType);
    }

    [Fact]
    public async Task UploadDocumentAsync_PersistsToDatabase()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await _sut.UploadDocumentAsync(dealId, stream, "test.pdf", DocumentType.T12PAndL);

        Assert.Equal(1, await _db.UploadedDocuments.CountAsync());
    }

    [Fact]
    public async Task UploadDocumentAsync_SavesFileToStorage()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 10, 20, 30 });

        await _sut.UploadDocumentAsync(dealId, stream, "data.xlsx", DocumentType.RentRoll);

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
        await _sut.UploadDocumentAsync(dealId, s1, "a.pdf", DocumentType.RentRoll);
        await _sut.UploadDocumentAsync(dealId, s2, "b.csv", DocumentType.T12PAndL);

        var docs = await _sut.GetDocumentsForDealAsync(dealId);

        Assert.Equal(2, docs.Count);
    }

    [Fact]
    public async Task GetDocumentsForDealAsync_EmptyWhenNoDocs()
    {
        var dealId = await SeedDealAsync();

        var docs = await _sut.GetDocumentsForDealAsync(dealId);

        Assert.Empty(docs);
    }

    [Fact]
    public async Task DeleteDocumentAsync_RemovesFromDatabase()
    {
        var dealId = await SeedDealAsync();
        using var stream = new MemoryStream(new byte[] { 1, 2 });
        var result = await _sut.UploadDocumentAsync(dealId, stream, "test.pdf", DocumentType.Appraisal);

        await _sut.DeleteDocumentAsync(result.DocumentId);

        Assert.Equal(0, await _db.UploadedDocuments.CountAsync());
    }

    [Fact]
    public async Task DeleteDocumentAsync_NonExistentDoesNotThrow()
    {
        await _sut.DeleteDocumentAsync(Guid.NewGuid());
    }

    private async Task<Guid> SeedDealAsync()
    {
        var deal = new Deal("Test Property");
        _db.Deals.Add(deal);
        await _db.SaveChangesAsync();
        return deal.Id;
    }
}
