using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Parsing;

namespace ZSR.Underwriting.Tests.Parsing;

public class RentRollParserTests
{
    private readonly RentRollParser _sut = new();

    [Fact]
    public void SupportedType_IsRentRoll()
    {
        Assert.Equal(DocumentType.RentRoll, _sut.SupportedType);
    }

    [Theory]
    [InlineData("rentroll.xlsx", true)]
    [InlineData("rentroll.csv", true)]
    [InlineData("rentroll.XLSX", true)]
    [InlineData("rentroll.pdf", false)]
    [InlineData("rentroll.docx", false)]
    public void CanParse_ReturnsExpected(string fileName, bool expected)
    {
        Assert.Equal(expected, _sut.CanParse(fileName));
    }

    [Fact]
    public async Task ParseAsync_Csv_ExtractsUnitCount()
    {
        using var stream = CreateCsvRentRoll(new[]
        {
            ("101", "1BR", 1200m, true, "2026-06-30", 750),
            ("102", "2BR", 1500m, true, "2026-09-15", 950),
            ("103", "1BR", 0m, false, "", 750),
        });

        var result = await _sut.ParseAsync(stream, "test.csv");

        Assert.True(result.Success);
        Assert.Equal(3, result.UnitCount);
    }

    [Fact]
    public async Task ParseAsync_Csv_CalculatesOccupancyRate()
    {
        using var stream = CreateCsvRentRoll(new[]
        {
            ("101", "1BR", 1200m, true, "2026-06-30", 750),
            ("102", "2BR", 1500m, true, "2026-09-15", 950),
            ("103", "1BR", 0m, false, "", 750),
        });

        var result = await _sut.ParseAsync(stream, "test.csv");

        Assert.Equal(66.67m, Math.Round(result.OccupancyRate!.Value, 2));
    }

    [Fact]
    public async Task ParseAsync_Csv_CalculatesAverageRent()
    {
        using var stream = CreateCsvRentRoll(new[]
        {
            ("101", "1BR", 1200m, true, "2026-06-30", 750),
            ("102", "2BR", 1500m, true, "2026-09-15", 950),
        });

        var result = await _sut.ParseAsync(stream, "test.csv");

        Assert.Equal(1350m, result.AverageRent);
    }

    [Fact]
    public async Task ParseAsync_Csv_CalculatesTotalMonthlyRent()
    {
        using var stream = CreateCsvRentRoll(new[]
        {
            ("101", "1BR", 1200m, true, "2026-06-30", 750),
            ("102", "2BR", 1500m, true, "2026-09-15", 950),
        });

        var result = await _sut.ParseAsync(stream, "test.csv");

        Assert.Equal(2700m, result.TotalMonthlyRent);
    }

    [Fact]
    public async Task ParseAsync_Csv_PopulatesUnits()
    {
        using var stream = CreateCsvRentRoll(new[]
        {
            ("101", "1BR", 1200m, true, "2026-06-30", 750),
        });

        var result = await _sut.ParseAsync(stream, "test.csv");

        Assert.Single(result.Units);
        var unit = result.Units[0];
        Assert.Equal("101", unit.UnitNumber);
        Assert.Equal("1BR", unit.UnitType);
        Assert.Equal(1200m, unit.MonthlyRent);
        Assert.True(unit.IsOccupied);
        Assert.Equal(750, unit.SquareFeet);
    }

    [Fact]
    public async Task ParseAsync_Xlsx_ExtractsData()
    {
        using var stream = CreateXlsxRentRoll(new[]
        {
            ("201", "Studio", 900m, true, "2026-12-31", 500),
            ("202", "1BR", 1100m, true, "2026-08-15", 700),
            ("203", "2BR", 1400m, false, "", 950),
        });

        var result = await _sut.ParseAsync(stream, "test.xlsx");

        Assert.True(result.Success);
        Assert.Equal(3, result.UnitCount);
        Assert.Equal(66.67m, Math.Round(result.OccupancyRate!.Value, 2));
        Assert.Equal(3, result.Units.Count);
    }

    [Fact]
    public async Task ParseAsync_EmptyStream_ReturnsError()
    {
        using var stream = new MemoryStream();

        var result = await _sut.ParseAsync(stream, "empty.csv");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    // --- Helper: create a CSV rent roll stream ---
    private static MemoryStream CreateCsvRentRoll(
        (string unit, string type, decimal rent, bool occupied, string leaseExp, int sqft)[] rows)
    {
        var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, leaveOpen: true))
        {
            writer.WriteLine("Unit,Type,MonthlyRent,Occupied,LeaseExpiration,SquareFeet");
            foreach (var r in rows)
                writer.WriteLine($"{r.unit},{r.type},{r.rent},{r.occupied},{r.leaseExp},{r.sqft}");
            writer.Flush();
        }
        ms.Position = 0;
        return ms;
    }

    // --- Helper: create an XLSX rent roll stream ---
    private static MemoryStream CreateXlsxRentRoll(
        (string unit, string type, decimal rent, bool occupied, string leaseExp, int sqft)[] rows)
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
                Name = "Rent Roll"
            });

            // Header row
            var headerRow = new Row();
            headerRow.Append(
                MakeCell("Unit"), MakeCell("Type"), MakeCell("MonthlyRent"),
                MakeCell("Occupied"), MakeCell("LeaseExpiration"), MakeCell("SquareFeet"));
            sheetData.Append(headerRow);

            foreach (var r in rows)
            {
                var row = new Row();
                row.Append(
                    MakeCell(r.unit), MakeCell(r.type), MakeCell(r.rent.ToString(CultureInfo.InvariantCulture)),
                    MakeCell(r.occupied.ToString()), MakeCell(r.leaseExp), MakeCell(r.sqft.ToString()));
                sheetData.Append(row);
            }
        }
        ms.Position = 0;
        return ms;
    }

    private static Cell MakeCell(string value) =>
        new() { DataType = CellValues.String, CellValue = new CellValue(value) };
}
