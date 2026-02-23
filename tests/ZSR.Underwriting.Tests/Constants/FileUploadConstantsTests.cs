using ZSR.Underwriting.Application.Constants;

namespace ZSR.Underwriting.Tests.Constants;

public class FileUploadConstantsTests
{
    [Fact]
    public void MaxFileSizeBytes_Is25MB()
    {
        Assert.Equal(25 * 1024 * 1024, FileUploadConstants.MaxFileSizeBytes);
    }

    [Theory]
    [InlineData(".pdf", true)]
    [InlineData(".xlsx", true)]
    [InlineData(".csv", true)]
    [InlineData(".docx", true)]
    [InlineData(".PDF", true)]
    [InlineData(".txt", false)]
    [InlineData(".exe", false)]
    [InlineData(".zip", false)]
    [InlineData("", false)]
    public void IsValidExtension_ReturnsExpected(string fileName, bool expected)
    {
        // Prepend a name if extension only
        var fullName = string.IsNullOrEmpty(fileName) ? "" : "test" + fileName;
        Assert.Equal(expected, FileUploadConstants.IsValidExtension(fullName));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    [InlineData(25 * 1024 * 1024, true)]
    [InlineData(25 * 1024 * 1024 + 1, false)]
    public void IsValidFileSize_ReturnsExpected(long fileSize, bool expected)
    {
        Assert.Equal(expected, FileUploadConstants.IsValidFileSize(fileSize));
    }

    [Fact]
    public void AllowedExtensions_ContainsFourTypes()
    {
        Assert.Equal(4, FileUploadConstants.AllowedExtensions.Count);
    }

    [Fact]
    public void MagicBytes_ContainsAllAllowedExtensions()
    {
        foreach (var ext in FileUploadConstants.AllowedExtensions)
        {
            Assert.True(
                FileUploadConstants.MagicBytes.ContainsKey(ext),
                $"MagicBytes should contain entry for {ext}");
        }
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".xlsx")]
    [InlineData(".docx")]
    public void MagicBytes_HasNonEmptySignatures(string extension)
    {
        var signatures = FileUploadConstants.MagicBytes[extension];
        Assert.NotEmpty(signatures);
        foreach (var sig in signatures)
        {
            Assert.NotEmpty(sig);
        }
    }

    [Fact]
    public void MagicBytes_CsvHasEmptySignature()
    {
        // CSV is plain text — no magic bytes, but entry should exist
        var signatures = FileUploadConstants.MagicBytes[".csv"];
        Assert.NotNull(signatures);
        // CSV signatures list can be empty (text-based validation only)
        Assert.Empty(signatures);
    }

    [Fact]
    public void MagicBytes_PdfStartsWith25504446()
    {
        // %PDF in hex
        var signatures = FileUploadConstants.MagicBytes[".pdf"];
        Assert.Contains(signatures, sig =>
            sig.Length >= 4 &&
            sig[0] == 0x25 && sig[1] == 0x50 && sig[2] == 0x44 && sig[3] == 0x46);
    }

    [Fact]
    public void MagicBytes_XlsxAndDocxHavePkZipSignature()
    {
        // Both are ZIP-based: PK\x03\x04
        byte[] pkSig = [0x50, 0x4B, 0x03, 0x04];
        foreach (var ext in new[] { ".xlsx", ".docx" })
        {
            var signatures = FileUploadConstants.MagicBytes[ext];
            Assert.Contains(signatures, sig =>
                sig.Length >= 4 &&
                sig[0] == pkSig[0] && sig[1] == pkSig[1] &&
                sig[2] == pkSig[2] && sig[3] == pkSig[3]);
        }
    }

    [Fact]
    public void AllowedMimeTypes_ContainsAllAllowedExtensions()
    {
        foreach (var ext in FileUploadConstants.AllowedExtensions)
        {
            Assert.True(
                FileUploadConstants.AllowedMimeTypes.ContainsKey(ext),
                $"AllowedMimeTypes should contain entry for {ext}");
        }
    }

    [Theory]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".csv", "text/csv")]
    [InlineData(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void AllowedMimeTypes_MapsCorrectly(string extension, string expectedMime)
    {
        var mimeTypes = FileUploadConstants.AllowedMimeTypes[extension];
        Assert.Contains(expectedMime, mimeTypes);
    }
}
