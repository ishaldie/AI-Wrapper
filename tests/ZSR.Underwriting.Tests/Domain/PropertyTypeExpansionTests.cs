using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Entities;
using System.Text.Json;

namespace ZSR.Underwriting.Tests.Domain;

public class PropertyTypeExpansionTests
{
    [Fact]
    public void PropertyType_Has12Values()
    {
        var values = Enum.GetValues<PropertyType>();
        Assert.Equal(12, values.Length);
    }

    [Theory]
    [InlineData("Multifamily", PropertyType.Multifamily)]
    [InlineData("AssistedLiving", PropertyType.AssistedLiving)]
    [InlineData("SkilledNursing", PropertyType.SkilledNursing)]
    [InlineData("MemoryCare", PropertyType.MemoryCare)]
    [InlineData("CCRC", PropertyType.CCRC)]
    [InlineData("Bridge", PropertyType.Bridge)]
    [InlineData("Hospitality", PropertyType.Hospitality)]
    [InlineData("Commercial", PropertyType.Commercial)]
    [InlineData("LIHTC", PropertyType.LIHTC)]
    [InlineData("BoardAndCare", PropertyType.BoardAndCare)]
    [InlineData("IndependentLiving", PropertyType.IndependentLiving)]
    [InlineData("SeniorApartment", PropertyType.SeniorApartment)]
    public void PropertyType_ParsesFromString(string name, PropertyType expected)
    {
        Assert.True(Enum.TryParse<PropertyType>(name, true, out var result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetailedExpenses_HasAnyValues_FalseWhenEmpty()
    {
        var expenses = new DetailedExpenses();
        Assert.False(expenses.HasAnyValues);
    }

    [Fact]
    public void DetailedExpenses_HasAnyValues_TrueWhenOneSet()
    {
        var expenses = new DetailedExpenses { RealEstateTaxes = 50_000m };
        Assert.True(expenses.HasAnyValues);
    }

    [Fact]
    public void DetailedExpenses_Total_SumsAllFields()
    {
        var expenses = new DetailedExpenses
        {
            RealEstateTaxes = 100_000m,
            Insurance = 25_000m,
            Utilities = 40_000m,
            RepairsAndMaintenance = 30_000m,
            Payroll = 80_000m,
            Marketing = 5_000m,
            GeneralAndAdmin = 15_000m,
            ManagementFee = 35_000m,
            ReplacementReserves = 12_500m,
            OtherExpenses = 7_500m
        };
        Assert.Equal(350_000m, expenses.Total);
    }

    [Fact]
    public void DetailedExpenses_Total_HandlesNulls()
    {
        var expenses = new DetailedExpenses
        {
            RealEstateTaxes = 100_000m,
            Insurance = 25_000m
        };
        Assert.Equal(125_000m, expenses.Total);
    }

    [Fact]
    public void DetailedExpenses_SerializesRoundTrip()
    {
        var original = new DetailedExpenses
        {
            RealEstateTaxes = 100_000m,
            Insurance = 25_000m,
            ManagementFeePct = 3.5m
        };
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DetailedExpenses>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(100_000m, deserialized!.RealEstateTaxes);
        Assert.Equal(25_000m, deserialized.Insurance);
        Assert.Equal(3.5m, deserialized.ManagementFeePct);
        Assert.Null(deserialized.Utilities);
    }

    [Fact]
    public void Deal_DetailedExpensesJson_NullByDefault()
    {
        var deal = new Deal("Test Deal");
        Assert.Null(deal.DetailedExpensesJson);
    }

    [Fact]
    public void Deal_DetailedExpensesJson_StoresAndRetrieves()
    {
        var deal = new Deal("Test Deal");
        var expenses = new DetailedExpenses { RealEstateTaxes = 50_000m };
        deal.DetailedExpensesJson = JsonSerializer.Serialize(expenses);

        var restored = JsonSerializer.Deserialize<DetailedExpenses>(deal.DetailedExpensesJson);
        Assert.NotNull(restored);
        Assert.Equal(50_000m, restored!.RealEstateTaxes);
    }

    [Theory]
    [InlineData(PropertyType.Bridge)]
    [InlineData(PropertyType.Hospitality)]
    [InlineData(PropertyType.Commercial)]
    [InlineData(PropertyType.LIHTC)]
    [InlineData(PropertyType.BoardAndCare)]
    [InlineData(PropertyType.IndependentLiving)]
    [InlineData(PropertyType.SeniorApartment)]
    public void Deal_AcceptsNewPropertyTypes(PropertyType type)
    {
        var deal = new Deal("Test Deal");
        deal.PropertyType = type;
        Assert.Equal(type, deal.PropertyType);
    }
}
