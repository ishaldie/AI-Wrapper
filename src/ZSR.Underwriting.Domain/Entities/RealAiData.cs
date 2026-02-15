namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// Stub — full implementation in Task 5.
/// </summary>
public class RealAiData
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
}
