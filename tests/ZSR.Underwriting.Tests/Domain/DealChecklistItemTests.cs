using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Domain;

public class DealChecklistItemTests
{
    [Fact]
    public void Constructor_with_valid_args_creates_entity()
    {
        var dealId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var item = new DealChecklistItem(dealId, templateId);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(dealId, item.DealId);
        Assert.Equal(templateId, item.ChecklistTemplateId);
        Assert.Equal(ChecklistStatus.Outstanding, item.Status);
    }

    [Fact]
    public void Constructor_with_empty_dealId_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new DealChecklistItem(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_with_empty_templateId_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new DealChecklistItem(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Status_defaults_to_Outstanding()
    {
        var item = new DealChecklistItem(Guid.NewGuid(), Guid.NewGuid());
        Assert.Equal(ChecklistStatus.Outstanding, item.Status);
    }

    [Fact]
    public void DocumentId_defaults_to_null()
    {
        var item = new DealChecklistItem(Guid.NewGuid(), Guid.NewGuid());
        Assert.Null(item.DocumentId);
    }

    [Fact]
    public void UpdateStatus_changes_status_and_sets_updatedAt()
    {
        var item = new DealChecklistItem(Guid.NewGuid(), Guid.NewGuid());
        var before = item.UpdatedAt;

        item.UpdateStatus(ChecklistStatus.Satisfied);

        Assert.Equal(ChecklistStatus.Satisfied, item.Status);
        Assert.True(item.UpdatedAt >= before);
    }

    [Fact]
    public void MarkSatisfied_sets_status_and_links_document()
    {
        var item = new DealChecklistItem(Guid.NewGuid(), Guid.NewGuid());
        var docId = Guid.NewGuid();

        item.MarkSatisfied(docId);

        Assert.Equal(ChecklistStatus.Satisfied, item.Status);
        Assert.Equal(docId, item.DocumentId);
    }
}
