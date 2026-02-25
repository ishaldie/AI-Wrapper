using Microsoft.Extensions.Logging;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class TokenUsageTracker : ITokenUsageTracker
{
    private readonly AppDbContext _db;
    private readonly ILogger<TokenUsageTracker> _logger;

    public TokenUsageTracker(AppDbContext db, ILogger<TokenUsageTracker> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RecordUsageAsync(
        string userId,
        Guid? dealId,
        OperationType operationType,
        int inputTokens,
        int outputTokens,
        string model)
    {
        try
        {
            var record = new TokenUsageRecord(userId, dealId, operationType, inputTokens, outputTokens, model);
            _db.TokenUsageRecords.Add(record);
            await _db.SaveChangesAsync();

            _logger.LogDebug(
                "Token usage recorded: user={UserId}, deal={DealId}, op={OperationType}, in={Input}, out={Output}",
                userId, dealId, operationType, inputTokens, outputTokens);
        }
        catch (Exception ex)
        {
            // Fire-and-forget: never block the caller for tracking failures
            _logger.LogWarning(ex, "Failed to record token usage for user {UserId}", userId);
        }
    }
}
