namespace ZSR.Underwriting.Domain.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string subFolder, CancellationToken ct = default);
    Task<Stream> GetFileAsync(string storedPath, CancellationToken ct = default);
    Task DeleteFileAsync(string storedPath, CancellationToken ct = default);
    Task<bool> FileExistsAsync(string storedPath, CancellationToken ct = default);
}
