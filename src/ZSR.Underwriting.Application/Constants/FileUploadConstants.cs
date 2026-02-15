namespace ZSR.Underwriting.Application.Constants;

public static class FileUploadConstants
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".xlsx", ".csv", ".docx"
    };

    public static bool IsValidFileSize(long fileSize) => fileSize > 0 && fileSize <= MaxFileSizeBytes;

    public static bool IsValidExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
    }
}
