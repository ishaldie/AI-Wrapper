using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Domain;

public class SecuritizationCompTests
{
    [Fact]
    public void Constructor_SetsIdAndSource()
    {
        var comp = new SecuritizationComp(SecuritizationDataSource.CMBS);

        Assert.NotEqual(Guid.Empty, comp.Id);
        Assert.Equal(SecuritizationDataSource.CMBS, comp.Source);
    }

    [Fact]
    public void Constructor_SetsDefaults()
    {
        var comp = new SecuritizationComp(SecuritizationDataSource.FannieMae);

        Assert.Null(comp.PropertyType);
        Assert.Null(comp.State);
        Assert.Null(comp.City);
        Assert.Null(comp.MSA);
        Assert.Null(comp.Units);
        Assert.Null(comp.LoanAmount);
        Assert.Null(comp.InterestRate);
        Assert.Null(comp.DSCR);
        Assert.Null(comp.LTV);
        Assert.Null(comp.NOI);
        Assert.Null(comp.Occupancy);
        Assert.Null(comp.CapRate);
        Assert.Null(comp.MaturityDate);
        Assert.Null(comp.OriginationDate);
        Assert.Null(comp.DealName);
        Assert.Null(comp.SecuritizationId);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var comp = new SecuritizationComp(SecuritizationDataSource.FreddieMac)
        {
            PropertyType = PropertyType.Multifamily,
            State = "GA",
            City = "Atlanta",
            MSA = "Atlanta-Sandy Springs-Roswell",
            Units = 120,
            LoanAmount = 15_000_000m,
            InterestRate = 5.25m,
            DSCR = 1.35m,
            LTV = 72.5m,
            NOI = 1_200_000m,
            Occupancy = 94.5m,
            CapRate = 5.75m,
            MaturityDate = new DateTime(2033, 6, 1),
            OriginationDate = new DateTime(2023, 6, 1),
            DealName = "K-Deal 2023-K150",
            SecuritizationId = "FHMS K-150"
        };

        Assert.Equal(PropertyType.Multifamily, comp.PropertyType);
        Assert.Equal("GA", comp.State);
        Assert.Equal("Atlanta", comp.City);
        Assert.Equal("Atlanta-Sandy Springs-Roswell", comp.MSA);
        Assert.Equal(120, comp.Units);
        Assert.Equal(15_000_000m, comp.LoanAmount);
        Assert.Equal(5.25m, comp.InterestRate);
        Assert.Equal(1.35m, comp.DSCR);
        Assert.Equal(72.5m, comp.LTV);
        Assert.Equal(1_200_000m, comp.NOI);
        Assert.Equal(94.5m, comp.Occupancy);
        Assert.Equal(5.75m, comp.CapRate);
        Assert.Equal(new DateTime(2033, 6, 1), comp.MaturityDate);
        Assert.Equal(new DateTime(2023, 6, 1), comp.OriginationDate);
        Assert.Equal("K-Deal 2023-K150", comp.DealName);
        Assert.Equal("FHMS K-150", comp.SecuritizationId);
    }

    [Theory]
    [InlineData(SecuritizationDataSource.CMBS)]
    [InlineData(SecuritizationDataSource.FannieMae)]
    [InlineData(SecuritizationDataSource.FreddieMac)]
    public void Constructor_AcceptsAllDataSources(SecuritizationDataSource source)
    {
        var comp = new SecuritizationComp(source);
        Assert.Equal(source, comp.Source);
    }

    [Fact]
    public void Each_Instance_HasUniqueId()
    {
        var comp1 = new SecuritizationComp(SecuritizationDataSource.CMBS);
        var comp2 = new SecuritizationComp(SecuritizationDataSource.CMBS);

        Assert.NotEqual(comp1.Id, comp2.Id);
    }
}

public class SecuritizationDataSourceEnumTests
{
    [Fact]
    public void Enum_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(SecuritizationDataSource), SecuritizationDataSource.CMBS));
        Assert.True(Enum.IsDefined(typeof(SecuritizationDataSource), SecuritizationDataSource.FannieMae));
        Assert.True(Enum.IsDefined(typeof(SecuritizationDataSource), SecuritizationDataSource.FreddieMac));
    }

    [Fact]
    public void Enum_HasExactlyThreeValues()
    {
        var values = Enum.GetValues<SecuritizationDataSource>();
        Assert.Equal(3, values.Length);
    }
}
