using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Domain.Entities;

public class ChecklistTemplate
{
    public Guid Id { get; private set; }
    public string Section { get; set; }
    public int SectionOrder { get; set; }
    public string ItemName { get; set; }
    public int SortOrder { get; set; }
    public ExecutionType ExecutionType { get; set; }
    public string TransactionType { get; set; }

    private ChecklistTemplate()
    {
        Section = string.Empty;
        ItemName = string.Empty;
        TransactionType = "All";
    }

    public ChecklistTemplate(
        string section, int sectionOrder,
        string itemName, int sortOrder,
        ExecutionType executionType, string transactionType)
    {
        if (string.IsNullOrWhiteSpace(section))
            throw new ArgumentException("Section cannot be empty.", nameof(section));
        if (string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("ItemName cannot be empty.", nameof(itemName));

        Id = Guid.NewGuid();
        Section = section;
        SectionOrder = sectionOrder;
        ItemName = itemName;
        SortOrder = sortOrder;
        ExecutionType = executionType;
        TransactionType = transactionType;
    }
}
