using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Infrastructure;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _testRoot;
    private readonly LocalFileStorageService _svc;

    public LocalFileStorageServiceTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "zsr_test_" + Guid.NewGuid().ToString("N"));
        _svc = new LocalFileStorageService(_testRoot);
    }

    [Fact]
    public async Task SaveFileAsync_ReturnsStoredPath()
    {
        using var stream = new MemoryStream("hello"u8.ToArray());

        var path = await _svc.SaveFileAsync(stream, "test.pdf", "deals");

        Assert.False(string.IsNullOrWhiteSpace(path));
    }

    [Fact]
    public async Task SaveFileAsync_CreatesFileOnDisk()
    {
        using var stream = new MemoryStream("hello"u8.ToArray());

        var path = await _svc.SaveFileAsync(stream, "test.pdf", "deals");

        var fullPath = Path.Combine(_testRoot, path);
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task SaveFileAsync_PreservesContent()
    {
        var content = "file content here"u8.ToArray();
        using var stream = new MemoryStream(content);

        var path = await _svc.SaveFileAsync(stream, "test.pdf", "deals");

        var fullPath = Path.Combine(_testRoot, path);
        var saved = await File.ReadAllBytesAsync(fullPath);
        Assert.Equal(content, saved);
    }

    [Fact]
    public async Task SaveFileAsync_GeneratesUniqueNames()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());

        var path1 = await _svc.SaveFileAsync(s1, "test.pdf", "deals");
        var path2 = await _svc.SaveFileAsync(s2, "test.pdf", "deals");

        Assert.NotEqual(path1, path2);
    }

    [Fact]
    public async Task GetFileAsync_ReturnsContent()
    {
        var content = "test data"u8.ToArray();
        using var stream = new MemoryStream(content);
        var path = await _svc.SaveFileAsync(stream, "test.pdf", "deals");

        using var result = await _svc.GetFileAsync(path);
        using var ms = new MemoryStream();
        await result.CopyToAsync(ms);

        Assert.Equal(content, ms.ToArray());
    }

    [Fact]
    public async Task GetFileAsync_ThrowsWhenNotFound()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _svc.GetFileAsync("nonexistent/file.pdf"));
    }

    [Fact]
    public async Task DeleteFileAsync_RemovesFile()
    {
        using var stream = new MemoryStream("hello"u8.ToArray());
        var path = await _svc.SaveFileAsync(stream, "test.pdf", "deals");

        await _svc.DeleteFileAsync(path);

        var fullPath = Path.Combine(_testRoot, path);
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task DeleteFileAsync_DoesNotThrowWhenNotFound()
    {
        var ex = await Record.ExceptionAsync(
            () => _svc.DeleteFileAsync("nonexistent/file.pdf"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task FileExistsAsync_ReturnsTrueWhenExists()
    {
        using var stream = new MemoryStream("hello"u8.ToArray());
        var path = await _svc.SaveFileAsync(stream, "test.pdf", "deals");

        var exists = await _svc.FileExistsAsync(path);

        Assert.True(exists);
    }

    [Fact]
    public async Task FileExistsAsync_ReturnsFalseWhenNotExists()
    {
        var exists = await _svc.FileExistsAsync("nonexistent/file.pdf");

        Assert.False(exists);
    }

    [Fact]
    public async Task SaveFileAsync_PreservesFileExtension()
    {
        using var stream = new MemoryStream("hello"u8.ToArray());

        var path = await _svc.SaveFileAsync(stream, "rent_roll.xlsx", "deals");

        Assert.EndsWith(".xlsx", path);
    }

    [Fact]
    public void Implements_IFileStorageService()
    {
        Assert.IsAssignableFrom<IFileStorageService>(_svc);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }
}
