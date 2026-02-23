using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Domain;

public class ChecklistTemplateTests
{
    [Fact]
    public void Constructor_with_valid_args_creates_entity()
    {
        var template = new ChecklistTemplate(
            "Historical & Proforma Property Operations",
            1,
            "Current Months Rent Roll",
            1,
            ExecutionType.All,
            "All");

        Assert.NotEqual(Guid.Empty, template.Id);
        Assert.Equal("Historical & Proforma Property Operations", template.Section);
        Assert.Equal(1, template.SectionOrder);
        Assert.Equal("Current Months Rent Roll", template.ItemName);
        Assert.Equal(1, template.SortOrder);
        Assert.Equal(ExecutionType.All, template.ExecutionType);
        Assert.Equal("All", template.TransactionType);
    }

    [Fact]
    public void Constructor_with_empty_section_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ChecklistTemplate("", 1, "Item", 1, ExecutionType.All, "All"));
    }

    [Fact]
    public void Constructor_with_empty_itemName_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ChecklistTemplate("Section", 1, "", 1, ExecutionType.All, "All"));
    }
}
