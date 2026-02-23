namespace ZSR.Underwriting.Application.Interfaces;

public record FileValidationResult(bool IsValid, string? ErrorMessage = null);

public interface IFileContentValidator
{
    Task<FileValidationResult> ValidateAsync(Stream fileStream, string extension, CancellationToken ct = default);
    bool IsValidMimeType(string extension, string contentType);
}
