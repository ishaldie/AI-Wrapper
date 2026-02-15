namespace ZSR.Underwriting.Domain.Entities;

/// <summary>
/// Tracks that a Deal field was overridden by data parsed from an uploaded document.
/// </summary>
public class FieldOverride
{
    public Guid Id { get; private set; }
    public Guid DealId { get; private set; }
    public Guid DocumentId { get; private set; }
    public string FieldName { get; private set; }
    public string OriginalValue { get; private set; }
    public string NewValue { get; private set; }
    public string Source { get; private set; }
    public DateTime AppliedAt { get; private set; }

    public Deal Deal { get; set; } = null!;
    public UploadedDocument Document { get; set; } = null!;

    private FieldOverride()
    {
        FieldName = string.Empty;
        OriginalValue = string.Empty;
        NewValue = string.Empty;
        Source = string.Empty;
    }

    public FieldOverride(Guid dealId, Guid documentId, string fieldName, string originalValue, string newValue, string source)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Field name cannot be empty.", nameof(fieldName));

        Id = Guid.NewGuid();
        DealId = dealId;
        DocumentId = documentId;
        FieldName = fieldName;
        OriginalValue = originalValue;
        NewValue = newValue;
        Source = source;
        AppliedAt = DateTime.UtcNow;
    }
}
