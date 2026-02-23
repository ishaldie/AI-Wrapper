using System.Globalization;
using System.Text;
using ZSR.Underwriting.Infrastructure.Parsing;

namespace ZSR.Underwriting.Tests.Parsing;

public class FormulaInjectionTests
{
    [Theory]
    [InlineData("=CMD|'/C calc.exe'!A0", "CMD|'/C calc.exe'!A0")]
    [InlineData("+CMD|'/C calc.exe'!A0", "CMD|'/C calc.exe'!A0")]
    [InlineData("-CMD|'/C calc.exe'!A0", "CMD|'/C calc.exe'!A0")]
    [InlineData("@SUM(1+1)*cmd|' /C calc'!A0", "SUM(1+1)*cmd|' /C calc'!A0")]
    [InlineData("Normal text", "Normal text")]
    [InlineData("", "")]
    [InlineData("  =leading spaces", "leading spaces")]
    public void SanitizeCellValue_StripsFormulaPrefix(string input, string expected)
    {
        Assert.Equal(expected, CellSanitizer.SanitizeCellValue(input));
    }

    [Fact]
    public async Task RentRollParser_SanitizesFormulaInCsv()
    {
        var csv = "Unit,Rent,Status\n=CMD|hack,1500,Occupied\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var parser = new RentRollParser();
        var result = await parser.ParseAsync(stream, "test.csv");

        Assert.True(result.Success);
        Assert.DoesNotContain(result.Units, u => u.UnitNumber.StartsWith("="));
        var unit = result.Units.First();
        Assert.Equal("CMD|hack", unit.UnitNumber);
    }

    [Fact]
    public async Task T12Parser_SanitizesFormulaInCsv()
    {
        var csv = "Category,Annual Amount\nRevenue,0\n=CMD|hack,50000\nExpenses,0\nInsurance,10000\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var parser = new T12Parser();
        var result = await parser.ParseAsync(stream, "test.csv");

        Assert.True(result.Success);
        // Revenue items should have sanitized category
        Assert.DoesNotContain(result.RevenueItems, item => item.Category.StartsWith("="));
    }
}
