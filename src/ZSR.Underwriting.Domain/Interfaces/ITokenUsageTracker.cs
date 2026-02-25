using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Interfaces;

public interface ITokenUsageTracker
{
    Task RecordUsageAsync(
        string userId,
        Guid? dealId,
        OperationType operationType,
        int inputTokens,
        int outputTokens,
        string model);
}
