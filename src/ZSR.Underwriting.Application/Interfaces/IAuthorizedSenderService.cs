using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IAuthorizedSenderService
{
    Task<AuthorizedSender?> AddAsync(string userId, string email, string? label = null);
    Task<bool> RemoveAsync(string userId, Guid senderId);
    Task<IReadOnlyList<AuthorizedSender>> ListAsync(string userId);
    Task<bool> IsAuthorizedAsync(string userId, string senderEmail);
}
