using ZSR.Underwriting.Domain.Interfaces;

namespace ZSR.Underwriting.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(string rootPath)
    {
        _rootPath = rootPath;
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string subFolder, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(subFolder, storedName);
        var fullPath = Path.Combine(_rootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var fileOnDisk = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await fileStream.CopyToAsync(fileOnDisk, ct);

        return relativePath;
    }

    public Task<Stream> GetFileAsync(string storedPath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, storedPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Stored file not found.", fullPath);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteFileAsync(string storedPath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, storedPath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string storedPath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, storedPath);
        return Task.FromResult(File.Exists(fullPath));
    }
}
