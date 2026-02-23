using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Domain;

public class CapitalStackItemTests
{
    [Fact]
    public void Constructor_with_valid_args_creates_entity()
    {
        var dealId = Guid.NewGuid();
        var item = new CapitalStackItem(dealId, CapitalSource.SeniorDebt, 3_500_000m);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(dealId, item.DealId);
        Assert.Equal(CapitalSource.SeniorDebt, item.Source);
        Assert.Equal(3_500_000m, item.Amount);
    }

    [Fact]
    public void Constructor_with_empty_dealId_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new CapitalStackItem(Guid.Empty, CapitalSource.SeniorDebt, 1_000_000m));
    }

    [Fact]
    public void Optional_fields_default_correctly()
    {
        var item = new CapitalStackItem(Guid.NewGuid(), CapitalSource.Mezzanine, 500_000m);

        Assert.Null(item.Rate);
        Assert.Null(item.TermYears);
        Assert.Null(item.Notes);
        Assert.Equal(0, item.SortOrder);
    }

    [Fact]
    public void Optional_fields_can_be_set()
    {
        var item = new CapitalStackItem(Guid.NewGuid(), CapitalSource.SeniorDebt, 3_000_000m)
        {
            Rate = 5.5m,
            TermYears = 10,
            Notes = "Agency loan",
            SortOrder = 1
        };

        Assert.Equal(5.5m, item.Rate);
        Assert.Equal(10, item.TermYears);
        Assert.Equal("Agency loan", item.Notes);
        Assert.Equal(1, item.SortOrder);
    }
}
