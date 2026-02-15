using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Parsing;

namespace ZSR.Underwriting.Tests.Parsing;

public class T12ParserTests
{
    private readonly T12Parser _sut = new();

    [Fact]
    public void SupportedType_IsT12PAndL()
    {
        Assert.Equal(DocumentType.T12PAndL, _sut.SupportedType);
    }

    [Theory]
    [InlineData("t12.xlsx", true)]
    [InlineData("t12.csv", true)]
    [InlineData("t12.pdf", false)]
    public void CanParse_ReturnsExpected(string fileName, bool expected)
    {
        Assert.Equal(expected, _sut.CanParse(fileName));
    }

    [Fact]
    public async Task ParseAsync_Xlsx_ExtractsRevenueItems()
    {
        using var stream = CreateT12Xlsx(
            revenue: new[] { ("Rental Income", 960000m), ("Other Income", 24000m) },
            expenses: new[] { ("Property Tax", 120000m), ("Insurance", 36000m), ("Maintenance", 48000m) });

        var result = await _sut.ParseAsync(stream, "t12.xlsx");

        Assert.True(result.Success);
        Assert.Equal(2, result.RevenueItems.Count);
        Assert.Equal("Rental Income", result.RevenueItems[0].Category);
        Assert.Equal(960000m, result.RevenueItems[0].AnnualAmount);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_ExtractsExpenseItems()
    {
        using var stream = CreateT12Xlsx(
            revenue: new[] { ("Rental Income", 960000m) },
            expenses: new[] { ("Property Tax", 120000m), ("Insurance", 36000m) });

        var result = await _sut.ParseAsync(stream, "t12.xlsx");

        Assert.Equal(2, result.ExpenseItems.Count);
        Assert.Equal(120000m, result.ExpenseItems[0].AnnualAmount);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_CalculatesGrossRevenue()
    {
        using var stream = CreateT12Xlsx(
            revenue: new[] { ("Rental Income", 960000m), ("Other Income", 24000m) },
            expenses: new[] { ("Maintenance", 48000m) });

        var result = await _sut.ParseAsync(stream, "t12.xlsx");

        Assert.Equal(984000m, result.GrossRevenue);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_CalculatesTotalExpenses()
    {
        using var stream = CreateT12Xlsx(
            revenue: new[] { ("Rental Income", 960000m) },
            expenses: new[] { ("Property Tax", 120000m), ("Insurance", 36000m), ("Maintenance", 48000m) });

        var result = await _sut.ParseAsync(stream, "t12.xlsx");

        Assert.Equal(204000m, result.TotalExpenses);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_CalculatesNOI()
    {
        using var stream = CreateT12Xlsx(
            revenue: new[] { ("Rental Income", 960000m), ("Other Income", 24000m) },
            expenses: new[] { ("Property Tax", 120000m), ("Insurance", 36000m), ("Maintenance", 48000m) });

        var result = await _sut.ParseAsync(stream, "t12.xlsx");

        Assert.Equal(780000m, result.NetOperatingIncome);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_SetsMonthlyAmounts()
    {
        using var stream = CreateT12Xlsx(
            revenue: new[] { ("Rental Income", 120000m) },
            expenses: new[] { ("Insurance", 12000m) });

        var result = await _sut.ParseAsync(stream, "t12.xlsx");

        Assert.Equal(10000m, result.RevenueItems[0].MonthlyAmount);
        Assert.Equal(1000m, result.ExpenseItems[0].MonthlyAmount);
    }

    [Fact]
    public async Task ParseAsync_EmptyStream_ReturnsError()
    {
        using var stream = new MemoryStream();

        var result = await _sut.ParseAsync(stream, "empty.xlsx");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    // --- Helper: create a T12 XLSX with Revenue and Expense sections ---
    private static MemoryStream CreateT12Xlsx(
        (string category, decimal annual)[] revenue,
        (string category, decimal annual)[] expenses)
    {
        var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook();
            var wsPart = wbPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            wsPart.Worksheet = new Worksheet(sheetData);

            var sheets = wbPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = wbPart.GetIdOfPart(wsPart),
                SheetId = 1,
                Name = "T12"
            });

            // Header
            var header = new Row();
            header.Append(MakeCell("Category"), MakeCell("Annual Amount"));
            sheetData.Append(header);

            // Revenue section
            sheetData.Append(MakeRow("REVENUE", ""));
            foreach (var r in revenue)
                sheetData.Append(MakeRow(r.category, r.annual.ToString(CultureInfo.InvariantCulture)));

            // Expense section
            sheetData.Append(MakeRow("EXPENSES", ""));
            foreach (var e in expenses)
                sheetData.Append(MakeRow(e.category, e.annual.ToString(CultureInfo.InvariantCulture)));
        }
        ms.Position = 0;
        return ms;
    }

    private static Row MakeRow(string col1, string col2)
    {
        var row = new Row();
        row.Append(MakeCell(col1), MakeCell(col2));
        return row;
    }

    private static Cell MakeCell(string value) =>
        new() { DataType = CellValues.String, CellValue = new CellValue(value) };
}
