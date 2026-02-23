namespace ZSR.Underwriting.Application.Constants;

public static class FileUploadConstants
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".xlsx", ".csv", ".docx"
    };

    /// <summary>
    /// Magic byte signatures for each allowed file type.
    /// CSV has no magic bytes (text-based), so its list is empty.
    /// XLSX and DOCX are ZIP-based (PK\x03\x04).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, byte[][]> MagicBytes =
        new Dictionary<string, byte[][]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = [new byte[] { 0x25, 0x50, 0x44, 0x46 }],           // %PDF
            [".xlsx"] = [new byte[] { 0x50, 0x4B, 0x03, 0x04 }],          // PK ZIP
            [".docx"] = [new byte[] { 0x50, 0x4B, 0x03, 0x04 }],          // PK ZIP
            [".csv"] = [],                                                  // plain text
        };

    /// <summary>
    /// Allowed MIME types per extension. Some extensions accept multiple valid MIME types.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> AllowedMimeTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".xlsx"] = [
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/zip"
            ],
            [".docx"] = [
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/zip"
            ],
            [".csv"] = ["text/csv", "text/plain", "application/csv"],
        };

    public static bool IsValidFileSize(long fileSize) => fileSize > 0 && fileSize <= MaxFileSizeBytes;

    public static bool IsValidExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
    }
}
