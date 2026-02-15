namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// Stub — full implementation in Task 3.
/// </summary>
public class UnderwritingInput
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
}
