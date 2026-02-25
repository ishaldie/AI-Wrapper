using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class TokenUsageRecord
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public Guid? DealId { get; private set; }
    public OperationType OperationType { get; private set; }
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public string Model { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TokenUsageRecord()
    {
        UserId = string.Empty;
        Model = string.Empty;
    }

    public TokenUsageRecord(
        string userId,
        Guid? dealId,
        OperationType operationType,
        int inputTokens,
        int outputTokens,
        string model)
    {
        Id = Guid.NewGuid();
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        DealId = dealId;
        OperationType = operationType;
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        Model = model ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }
}
