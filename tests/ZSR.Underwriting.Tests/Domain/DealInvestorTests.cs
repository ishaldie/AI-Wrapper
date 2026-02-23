using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Tests.Domain;

public class DealInvestorTests
{
    [Fact]
    public void Constructor_with_valid_args_creates_entity()
    {
        var dealId = Guid.NewGuid();
        var investor = new DealInvestor(dealId, "John Doe");

        Assert.NotEqual(Guid.Empty, investor.Id);
        Assert.Equal(dealId, investor.DealId);
        Assert.Equal("John Doe", investor.Name);
    }

    [Fact]
    public void Constructor_with_empty_dealId_throws()
    {
        Assert.Throws<ArgumentException>(() => new DealInvestor(Guid.Empty, "John"));
    }

    [Fact]
    public void Constructor_with_empty_name_throws()
    {
        Assert.Throws<ArgumentException>(() => new DealInvestor(Guid.NewGuid(), ""));
    }

    [Fact]
    public void Optional_fields_default_to_null_or_empty()
    {
        var investor = new DealInvestor(Guid.NewGuid(), "Jane");

        Assert.Null(investor.Company);
        Assert.Null(investor.Role);
        Assert.Null(investor.Address);
        Assert.Null(investor.City);
        Assert.Null(investor.State);
        Assert.Null(investor.Zip);
        Assert.Null(investor.Phone);
        Assert.Null(investor.Email);
        Assert.Null(investor.NetWorth);
        Assert.Null(investor.Liquidity);
        Assert.Null(investor.Notes);
    }

    [Fact]
    public void Optional_fields_can_be_set()
    {
        var investor = new DealInvestor(Guid.NewGuid(), "Jane")
        {
            Company = "Acme",
            Role = "LP",
            Email = "jane@acme.com",
            NetWorth = 5_000_000m,
            Liquidity = 1_000_000m
        };

        Assert.Equal("Acme", investor.Company);
        Assert.Equal("LP", investor.Role);
        Assert.Equal("jane@acme.com", investor.Email);
        Assert.Equal(5_000_000m, investor.NetWorth);
        Assert.Equal(1_000_000m, investor.Liquidity);
    }
}
