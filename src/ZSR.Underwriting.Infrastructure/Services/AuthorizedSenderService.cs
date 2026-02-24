using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class AuthorizedSenderService : IAuthorizedSenderService
{
    private readonly AppDbContext _db;

    public AuthorizedSenderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuthorizedSender?> AddAsync(string userId, string email, string? label = null)
    {
        var normalized = email.Trim().ToLowerInvariant();

        var exists = await _db.AuthorizedSenders
            .AnyAsync(s => s.UserId == userId && s.Email == normalized);

        if (exists)
            return null;

        var sender = new AuthorizedSender(userId, normalized, label);
        _db.AuthorizedSenders.Add(sender);
        await _db.SaveChangesAsync();
        return sender;
    }

    public async Task<bool> RemoveAsync(string userId, Guid senderId)
    {
        var sender = await _db.AuthorizedSenders
            .FirstOrDefaultAsync(s => s.Id == senderId && s.UserId == userId);

        if (sender is null)
            return false;

        _db.AuthorizedSenders.Remove(sender);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<AuthorizedSender>> ListAsync(string userId)
    {
        return await _db.AuthorizedSenders
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsAuthorizedAsync(string userId, string senderEmail)
    {
        var normalized = senderEmail.Trim().ToLowerInvariant();
        return await _db.AuthorizedSenders
            .AnyAsync(s => s.UserId == userId && s.Email == normalized);
    }
}
