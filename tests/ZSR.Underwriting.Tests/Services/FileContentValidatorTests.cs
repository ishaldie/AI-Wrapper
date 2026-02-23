using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class FileContentValidatorTests
{
    private readonly FileContentValidator _sut = new();

    [Fact]
    public async Task ValidateAsync_PdfWithCorrectMagicBytes_ReturnsValid()
    {
        // %PDF magic bytes followed by some content
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        using var stream = new MemoryStream(content);

        var result = await _sut.ValidateAsync(stream, ".pdf");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_PdfWithWrongMagicBytes_ReturnsInvalid()
    {
        // PNG magic bytes in a .pdf file
        var content = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(content);

        var result = await _sut.ValidateAsync(stream, ".pdf");

        Assert.False(result.IsValid);
        Assert.Contains("magic bytes", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_XlsxWithPkZipSignature_ReturnsValid()
    {
        // PK ZIP header
        var content = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(content);

        var result = await _sut.ValidateAsync(stream, ".xlsx");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_DocxWithPkZipSignature_ReturnsValid()
    {
        var content = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 };
        using var stream = new MemoryStream(content);

        var result = await _sut.ValidateAsync(stream, ".docx");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_CsvAlwaysValid_NoMagicBytes()
    {
        var content = "Unit,Rent,Status\n101,1500,Occupied"u8.ToArray();
        using var stream = new MemoryStream(content);

        var result = await _sut.ValidateAsync(stream, ".csv");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_EmptyStream_ReturnsInvalid()
    {
        using var stream = new MemoryStream([]);

        var result = await _sut.ValidateAsync(stream, ".pdf");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_UnknownExtension_ReturnsInvalid()
    {
        var content = new byte[] { 1, 2, 3, 4 };
        using var stream = new MemoryStream(content);

        var result = await _sut.ValidateAsync(stream, ".exe");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_StreamPositionResetAfterValidation()
    {
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        using var stream = new MemoryStream(content);

        await _sut.ValidateAsync(stream, ".pdf");

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task ValidateAsync_XlsxWithPdfContent_ReturnsInvalid()
    {
        // PDF magic bytes in a .xlsx file
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        using var stream = new MemoryStream(content);

        var result = await _sut.ValidateAsync(stream, ".xlsx");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateMimeTypeAsync_CorrectMime_ReturnsTrue()
    {
        Assert.True(_sut.IsValidMimeType(".pdf", "application/pdf"));
        Assert.True(_sut.IsValidMimeType(".csv", "text/csv"));
        Assert.True(_sut.IsValidMimeType(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        Assert.True(_sut.IsValidMimeType(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
    }

    [Fact]
    public async Task ValidateMimeTypeAsync_WrongMime_ReturnsFalse()
    {
        Assert.False(_sut.IsValidMimeType(".pdf", "text/plain"));
        Assert.False(_sut.IsValidMimeType(".xlsx", "application/pdf"));
    }

    [Fact]
    public async Task ValidateMimeTypeAsync_CsvAcceptsTextPlain()
    {
        // CSV accepts both text/csv and text/plain
        Assert.True(_sut.IsValidMimeType(".csv", "text/plain"));
        Assert.True(_sut.IsValidMimeType(".csv", "text/csv"));
    }
}
