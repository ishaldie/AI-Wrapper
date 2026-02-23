using ZSR.Underwriting.Application.Constants;
using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Infrastructure.Services;

public class FileContentValidator : IFileContentValidator
{
    public async Task<FileValidationResult> ValidateAsync(Stream fileStream, string extension, CancellationToken ct = default)
    {
        if (!FileUploadConstants.AllowedExtensions.Contains(extension))
            return new FileValidationResult(false, $"Extension '{extension}' is not allowed.");

        var signatures = FileUploadConstants.MagicBytes[extension];

        // No magic bytes to check (e.g., CSV) — accept
        if (signatures.Length == 0)
            return new FileValidationResult(true);

        // Need at least some bytes to check
        int maxSigLen = signatures.Max(s => s.Length);
        if (fileStream.Length < maxSigLen)
            return new FileValidationResult(false, "File is too small to validate magic bytes.");

        var buffer = new byte[maxSigLen];
        var originalPosition = fileStream.Position;
        fileStream.Position = 0;
        var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, maxSigLen), ct);
        fileStream.Position = originalPosition;

        if (bytesRead < maxSigLen)
            return new FileValidationResult(false, "File is too small to validate magic bytes.");

        // Check if any signature matches
        foreach (var sig in signatures)
        {
            if (buffer.AsSpan(0, sig.Length).SequenceEqual(sig))
                return new FileValidationResult(true);
        }

        return new FileValidationResult(false, $"File content does not match expected magic bytes for '{extension}'.");
    }

    public bool IsValidMimeType(string extension, string contentType)
    {
        if (!FileUploadConstants.AllowedMimeTypes.TryGetValue(extension, out var allowedMimes))
            return false;

        return allowedMimes.Any(m => m.Equals(contentType, StringComparison.OrdinalIgnoreCase));
    }
}
