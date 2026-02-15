using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Domain;

public class UploadedDocumentTests
{
    [Fact]
    public void Constructor_Sets_Properties()
    {
        var dealId = Guid.NewGuid();
        var doc = new UploadedDocument(
            dealId, "rent_roll.xlsx", "deals/abc123.xlsx",
            DocumentType.RentRoll, 1024);

        Assert.NotEqual(Guid.Empty, doc.Id);
        Assert.Equal(dealId, doc.DealId);
        Assert.Equal("rent_roll.xlsx", doc.FileName);
        Assert.Equal("deals/abc123.xlsx", doc.StoredPath);
        Assert.Equal(DocumentType.RentRoll, doc.DocumentType);
        Assert.Equal(1024, doc.FileSize);
    }

    [Fact]
    public void Constructor_Sets_UploadedAt()
    {
        var before = DateTime.UtcNow;
        var doc = new UploadedDocument(
            Guid.NewGuid(), "file.pdf", "path/file.pdf",
            DocumentType.T12PAndL, 500);
        var after = DateTime.UtcNow;

        Assert.InRange(doc.UploadedAt, before, after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_When_FileName_Empty(string? fileName)
    {
        Assert.Throws<ArgumentException>(() =>
            new UploadedDocument(Guid.NewGuid(), fileName!, "path", DocumentType.RentRoll, 100));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_When_StoredPath_Empty(string? storedPath)
    {
        Assert.Throws<ArgumentException>(() =>
            new UploadedDocument(Guid.NewGuid(), "file.pdf", storedPath!, DocumentType.RentRoll, 100));
    }

    [Fact]
    public void Constructor_Throws_When_FileSize_Zero_Or_Negative()
    {
        Assert.Throws<ArgumentException>(() =>
            new UploadedDocument(Guid.NewGuid(), "file.pdf", "path", DocumentType.RentRoll, 0));
        Assert.Throws<ArgumentException>(() =>
            new UploadedDocument(Guid.NewGuid(), "file.pdf", "path", DocumentType.RentRoll, -1));
    }

    [Fact]
    public void DocumentType_Has_All_Expected_Values()
    {
        Assert.True(Enum.IsDefined(typeof(DocumentType), DocumentType.RentRoll));
        Assert.True(Enum.IsDefined(typeof(DocumentType), DocumentType.T12PAndL));
        Assert.True(Enum.IsDefined(typeof(DocumentType), DocumentType.OfferingMemorandum));
        Assert.True(Enum.IsDefined(typeof(DocumentType), DocumentType.Appraisal));
        Assert.True(Enum.IsDefined(typeof(DocumentType), DocumentType.PhaseIPCA));
        Assert.True(Enum.IsDefined(typeof(DocumentType), DocumentType.LoanTermSheet));
    }
}
