using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Configuration;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class TokenBudgetService : ITokenBudgetService
{
    private readonly AppDbContext _db;
    private readonly TokenManagementOptions _options;

    public TokenBudgetService(AppDbContext db, IOptions<TokenManagementOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<(bool Allowed, int Used, int Limit)> CheckUserBudgetAsync(string userId)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var used = await _db.TokenUsageRecords
            .Where(r => r.UserId == userId && r.CreatedAt >= todayUtc)
            .SumAsync(r => r.InputTokens + r.OutputTokens);

        var limit = _options.DailyUserTokenBudget;
        return (used < limit, used, limit);
    }

    public async Task<(bool Allowed, int Used, int Limit, bool Warning)> CheckDealBudgetAsync(Guid dealId)
    {
        var used = await _db.TokenUsageRecords
            .Where(r => r.DealId == dealId)
            .SumAsync(r => r.InputTokens + r.OutputTokens);

        var limit = _options.DealTokenBudget;
        var warning = used >= (int)(limit * 0.8);
        return (used < limit, used, limit, warning);
    }
}
