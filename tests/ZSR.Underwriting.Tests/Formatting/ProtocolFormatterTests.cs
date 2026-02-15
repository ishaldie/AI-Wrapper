using ZSR.Underwriting.Application.Formatting;

namespace ZSR.Underwriting.Tests.Formatting;

public class ProtocolFormatterTests
{
    // === Currency formatting: $X,XXX ===

    [Theory]
    [InlineData(0, "$0")]
    [InlineData(1000, "$1,000")]
    [InlineData(1234567, "$1,234,567")]
    [InlineData(1234567.89, "$1,234,568")]
    [InlineData(999.50, "$1,000")]
    [InlineData(-50000, "-$50,000")]
    public void FormatCurrency_FormatsWithCommasAndDollarSign(decimal value, string expected)
    {
        var result = ProtocolFormatter.Currency(value);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, "$0.00")]
    [InlineData(1234.56, "$1,234.56")]
    [InlineData(1234.5, "$1,234.50")]
    [InlineData(1000000, "$1,000,000.00")]
    [InlineData(-999.99, "-$999.99")]
    public void FormatCurrencyExact_FormatsWithTwoDecimals(decimal value, string expected)
    {
        var result = ProtocolFormatter.CurrencyExact(value);
        Assert.Equal(expected, result);
    }

    // === Percentage formatting: XX.X% ===

    [Theory]
    [InlineData(0, "0.0%")]
    [InlineData(95, "95.0%")]
    [InlineData(65.5, "65.5%")]
    [InlineData(99.99, "100.0%")]
    [InlineData(5.25, "5.3%")]
    [InlineData(-2.5, "-2.5%")]
    public void FormatPercent_FormatsWithOneDecimal(decimal value, string expected)
    {
        var result = ProtocolFormatter.Percent(value);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, "0.00%")]
    [InlineData(6.75, "6.75%")]
    [InlineData(5.5, "5.50%")]
    [InlineData(12.125, "12.13%")]
    public void FormatPercentExact_FormatsWithTwoDecimals(decimal value, string expected)
    {
        var result = ProtocolFormatter.PercentExact(value);
        Assert.Equal(expected, result);
    }

    // === Multiple formatting: X.XXx ===

    [Theory]
    [InlineData(0, "0.00x")]
    [InlineData(1.25, "1.25x")]
    [InlineData(2, "2.00x")]
    [InlineData(1.333, "1.33x")]
    [InlineData(0.85, "0.85x")]
    [InlineData(-1.5, "-1.50x")]
    public void FormatMultiple_FormatsWithTwoDecimalsAndX(decimal value, string expected)
    {
        var result = ProtocolFormatter.Multiple(value);
        Assert.Equal(expected, result);
    }

    // === Per-unit formatting ===

    [Theory]
    [InlineData(1200000, 100, "$12,000/unit")]
    [InlineData(500000, 50, "$10,000/unit")]
    [InlineData(0, 10, "$0/unit")]
    public void FormatPerUnit_FormatsAsCurrencyPerUnit(decimal total, int units, string expected)
    {
        var result = ProtocolFormatter.PerUnit(total, units);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatPerUnit_ZeroUnits_ReturnsNA()
    {
        var result = ProtocolFormatter.PerUnit(100000, 0);
        Assert.Equal("N/A", result);
    }

    // === Per-SF formatting ===

    [Theory]
    [InlineData(1200000, 60000, "$20.00/SF")]
    [InlineData(500000, 25000, "$20.00/SF")]
    [InlineData(750000, 50000, "$15.00/SF")]
    public void FormatPerSf_FormatsAsCurrencyPerSquareFoot(decimal total, int sqft, string expected)
    {
        var result = ProtocolFormatter.PerSf(total, sqft);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatPerSf_ZeroSqft_ReturnsNA()
    {
        var result = ProtocolFormatter.PerSf(100000, 0);
        Assert.Equal("N/A", result);
    }

    // === Year formatting ===

    [Theory]
    [InlineData(1, "1 year")]
    [InlineData(5, "5 years")]
    [InlineData(30, "30 years")]
    public void FormatYears_FormatsWithPluralHandling(int years, string expected)
    {
        var result = ProtocolFormatter.Years(years);
        Assert.Equal(expected, result);
    }

    // === Integer formatting ===

    [Theory]
    [InlineData(0, "0")]
    [InlineData(100, "100")]
    [InlineData(1234, "1,234")]
    [InlineData(1000000, "1,000,000")]
    public void FormatInteger_FormatsWithCommas(int value, string expected)
    {
        var result = ProtocolFormatter.Integer(value);
        Assert.Equal(expected, result);
    }
}
