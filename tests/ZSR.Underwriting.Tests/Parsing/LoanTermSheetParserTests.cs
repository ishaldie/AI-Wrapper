using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Parsing;

namespace ZSR.Underwriting.Tests.Parsing;

public class LoanTermSheetParserTests
{
    private readonly LoanTermSheetParser _sut = new();

    [Fact]
    public void SupportedType_IsLoanTermSheet()
    {
        Assert.Equal(DocumentType.LoanTermSheet, _sut.SupportedType);
    }

    [Theory]
    [InlineData("loan.xlsx", true)]
    [InlineData("loan.csv", true)]
    [InlineData("loan.pdf", false)]
    public void CanParse_ReturnsExpected(string fileName, bool expected)
    {
        Assert.Equal(expected, _sut.CanParse(fileName));
    }

    [Fact]
    public async Task ParseAsync_Xlsx_ExtractsLoanAmount()
    {
        using var stream = CreateLoanTermSheetXlsx(new Dictionary<string, string>
        {
            ["Loan Amount"] = "15000000",
            ["Interest Rate"] = "5.25",
            ["LTV"] = "75",
            ["Loan Term"] = "10",
            ["Amortization"] = "30",
            ["Interest Only"] = "Yes",
            ["IO Period (Months)"] = "24",
            ["Prepayment"] = "Yield Maintenance"
        });

        var result = await _sut.ParseAsync(stream, "loan.xlsx");

        Assert.True(result.Success);
        Assert.Equal(15000000m, result.LoanAmount);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_ExtractsInterestRate()
    {
        using var stream = CreateLoanTermSheetXlsx(new Dictionary<string, string>
        {
            ["Interest Rate"] = "5.25",
        });

        var result = await _sut.ParseAsync(stream, "loan.xlsx");

        Assert.Equal(5.25m, result.InterestRate);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_ExtractsLtvRatio()
    {
        using var stream = CreateLoanTermSheetXlsx(new Dictionary<string, string>
        {
            ["LTV"] = "75",
        });

        var result = await _sut.ParseAsync(stream, "loan.xlsx");

        Assert.Equal(75m, result.LtvRatio);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_ExtractsTermAndAmortization()
    {
        using var stream = CreateLoanTermSheetXlsx(new Dictionary<string, string>
        {
            ["Loan Term"] = "10",
            ["Amortization"] = "30",
        });

        var result = await _sut.ParseAsync(stream, "loan.xlsx");

        Assert.Equal(10, result.LoanTermYears);
        Assert.Equal(30, result.AmortizationYears);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_ExtractsInterestOnly()
    {
        using var stream = CreateLoanTermSheetXlsx(new Dictionary<string, string>
        {
            ["Interest Only"] = "Yes",
            ["IO Period (Months)"] = "24",
        });

        var result = await _sut.ParseAsync(stream, "loan.xlsx");

        Assert.True(result.IsInterestOnly);
        Assert.Equal(24, result.IoTermMonths);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_ExtractsPrepaymentTerms()
    {
        using var stream = CreateLoanTermSheetXlsx(new Dictionary<string, string>
        {
            ["Prepayment"] = "Yield Maintenance",
        });

        var result = await _sut.ParseAsync(stream, "loan.xlsx");

        Assert.Equal("Yield Maintenance", result.PrepaymentTerms);
    }

    [Fact]
    public async Task ParseAsync_Csv_ExtractsFields()
    {
        using var stream = CreateLoanTermSheetCsv(new Dictionary<string, string>
        {
            ["Loan Amount"] = "10000000",
            ["Interest Rate"] = "4.75",
            ["LTV"] = "70",
        });

        var result = await _sut.ParseAsync(stream, "loan.csv");

        Assert.True(result.Success);
        Assert.Equal(10000000m, result.LoanAmount);
        Assert.Equal(4.75m, result.InterestRate);
        Assert.Equal(70m, result.LtvRatio);
    }

    [Fact]
    public async Task ParseAsync_EmptyStream_ReturnsError()
    {
        using var stream = new MemoryStream();

        var result = await _sut.ParseAsync(stream, "empty.xlsx");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    // --- Helper: create key-value XLSX ---
    private static MemoryStream CreateLoanTermSheetXlsx(Dictionary<string, string> terms)
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
                Name = "Terms"
            });

            // Header
            var header = new Row();
            header.Append(MakeCell("Term"), MakeCell("Value"));
            sheetData.Append(header);

            foreach (var kvp in terms)
            {
                var row = new Row();
                row.Append(MakeCell(kvp.Key), MakeCell(kvp.Value));
                sheetData.Append(row);
            }
        }
        ms.Position = 0;
        return ms;
    }

    // --- Helper: create key-value CSV ---
    private static MemoryStream CreateLoanTermSheetCsv(Dictionary<string, string> terms)
    {
        var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, leaveOpen: true))
        {
            writer.WriteLine("Term,Value");
            foreach (var kvp in terms)
                writer.WriteLine($"{kvp.Key},{kvp.Value}");
            writer.Flush();
        }
        ms.Position = 0;
        return ms;
    }

    private static Cell MakeCell(string value) =>
        new() { DataType = CellValues.String, CellValue = new CellValue(value) };
}
