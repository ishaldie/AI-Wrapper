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
}
