using Microsoft.EntityFrameworkCore;
using Xunit;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class AuthorizedSenderServiceTests : IAsyncLifetime
{
    private readonly AppDbContext _db;
    private readonly AuthorizedSenderService _svc;
    private const string UserId = "user-123";

    public AuthorizedSenderServiceTests()
    {
        var dbName = $"AuthSenderTests_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _db = new AppDbContext(options);
        _svc = new AuthorizedSenderService(_db);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task AddAsync_CreatesNewSender()
    {
        var result = await _svc.AddAsync(UserId, "broker@example.com", "My Broker");

        Assert.NotNull(result);
        Assert.Equal("broker@example.com", result.Email);
        Assert.Equal("My Broker", result.Label);
        Assert.Equal(UserId, result.UserId);

        var saved = await _db.AuthorizedSenders.FirstOrDefaultAsync(s => s.Email == "broker@example.com");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task AddAsync_NormalizesEmail()
    {
        var result = await _svc.AddAsync(UserId, "  Broker@EXAMPLE.com  ", "Broker");

        Assert.Equal("broker@example.com", result.Email);
    }

    [Fact]
    public async Task AddAsync_DuplicateEmail_ReturnsNull()
    {
        await _svc.AddAsync(UserId, "broker@example.com", "Broker");
        var duplicate = await _svc.AddAsync(UserId, "broker@example.com", "Same Broker");

        Assert.Null(duplicate);
    }

    [Fact]
    public async Task AddAsync_SameEmailDifferentUser_Succeeds()
    {
        await _svc.AddAsync(UserId, "broker@example.com", "Broker A");
        var result = await _svc.AddAsync("user-456", "broker@example.com", "Broker B");

        Assert.NotNull(result);
        Assert.Equal("user-456", result.UserId);
    }

    [Fact]
    public async Task RemoveAsync_DeletesExistingSender()
    {
        var sender = await _svc.AddAsync(UserId, "broker@example.com", "Broker");
        Assert.NotNull(sender);

        var removed = await _svc.RemoveAsync(UserId, sender.Id);
        Assert.True(removed);

        var remaining = await _db.AuthorizedSenders.CountAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task RemoveAsync_WrongUser_ReturnsFalse()
    {
        var sender = await _svc.AddAsync(UserId, "broker@example.com", "Broker");
        Assert.NotNull(sender);

        var removed = await _svc.RemoveAsync("wrong-user", sender.Id);
        Assert.False(removed);

        // Sender should still exist
        var count = await _db.AuthorizedSenders.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RemoveAsync_NonExistentId_ReturnsFalse()
    {
        var removed = await _svc.RemoveAsync(UserId, Guid.NewGuid());
        Assert.False(removed);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyUsersSenders()
    {
        await _svc.AddAsync(UserId, "a@example.com", "A");
        await _svc.AddAsync(UserId, "b@example.com", "B");
        await _svc.AddAsync("other-user", "c@example.com", "C");

        var list = await _svc.ListAsync(UserId);

        Assert.Equal(2, list.Count);
        Assert.All(list, s => Assert.Equal(UserId, s.UserId));
    }

    [Fact]
    public async Task ListAsync_OrdersByCreatedAt()
    {
        await _svc.AddAsync(UserId, "first@example.com", "First");
        await _svc.AddAsync(UserId, "second@example.com", "Second");

        var list = await _svc.ListAsync(UserId);

        Assert.Equal("first@example.com", list[0].Email);
        Assert.Equal("second@example.com", list[1].Email);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrueForExistingSender()
    {
        await _svc.AddAsync(UserId, "broker@example.com", "Broker");

        var authorized = await _svc.IsAuthorizedAsync(UserId, "broker@example.com");
        Assert.True(authorized);
    }

    [Fact]
    public async Task IsAuthorizedAsync_CaseInsensitive()
    {
        await _svc.AddAsync(UserId, "broker@example.com", "Broker");

        var authorized = await _svc.IsAuthorizedAsync(UserId, "BROKER@EXAMPLE.COM");
        Assert.True(authorized);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalseForUnknownEmail()
    {
        await _svc.AddAsync(UserId, "broker@example.com", "Broker");

        var authorized = await _svc.IsAuthorizedAsync(UserId, "stranger@evil.com");
        Assert.False(authorized);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalseForDifferentUser()
    {
        await _svc.AddAsync(UserId, "broker@example.com", "Broker");

        var authorized = await _svc.IsAuthorizedAsync("other-user", "broker@example.com");
        Assert.False(authorized);
    }
}
