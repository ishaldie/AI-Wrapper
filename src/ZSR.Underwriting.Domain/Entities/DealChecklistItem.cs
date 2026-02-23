using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class DealChecklistItem
{
    public Guid Id { get; private set; }
    public Guid DealId { get; set; }
    public Deal Deal { get; set; } = null!;

    public Guid ChecklistTemplateId { get; set; }
    public ChecklistTemplate Template { get; set; } = null!;

    public ChecklistStatus Status { get; private set; }
    public Guid? DocumentId { get; set; }
    public UploadedDocument? Document { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; private set; }

    private DealChecklistItem() { }

    public DealChecklistItem(Guid dealId, Guid checklistTemplateId)
    {
        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId cannot be empty.", nameof(dealId));
        if (checklistTemplateId == Guid.Empty)
            throw new ArgumentException("ChecklistTemplateId cannot be empty.", nameof(checklistTemplateId));

        Id = Guid.NewGuid();
        DealId = dealId;
        ChecklistTemplateId = checklistTemplateId;
        Status = ChecklistStatus.Outstanding;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(ChecklistStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSatisfied(Guid documentId)
    {
        Status = ChecklistStatus.Satisfied;
        DocumentId = documentId;
        UpdatedAt = DateTime.UtcNow;
    }
}
