namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// Stub — full implementation in Task 7.
/// </summary>
public class UnderwritingReport
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
}
