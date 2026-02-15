namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// Stub — full implementation in Task 8.
/// </summary>
public class UploadedDocument
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;
}
