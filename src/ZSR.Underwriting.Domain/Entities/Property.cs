namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// Stub — full implementation in Task 2.
/// </summary>
public class Property
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
}
