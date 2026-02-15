using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Infrastructure.Parsing;

public class RentRollParser : IDocumentParser
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".csv" };

    public DocumentType SupportedType => DocumentType.RentRoll;

    public bool CanParse(string fileName) =>
        SupportedExtensions.Contains(Path.GetExtension(fileName));

    public async Task<ParsedDocumentResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var result = new ParsedDocumentResult { DocumentType = DocumentType.RentRoll };

        try
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var units = ext == ".csv"
                ? await ParseCsvAsync(fileStream, ct)
                : ParseXlsx(fileStream);

            if (units.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No unit data found in file.";
                return result;
            }

            result.Units = units;
            result.UnitCount = units.Count;

            var occupiedUnits = units.Where(u => u.IsOccupied).ToList();
            result.OccupancyRate = units.Count > 0
                ? Math.Round((decimal)occupiedUnits.Count / units.Count * 100, 2)
                : 0;

            var unitsWithRent = units.Where(u => u.MonthlyRent > 0).ToList();
            result.TotalMonthlyRent = unitsWithRent.Sum(u => u.MonthlyRent);
            result.AverageRent = unitsWithRent.Count > 0
                ? Math.Round(result.TotalMonthlyRent.Value / unitsWithRent.Count, 2)
                : 0;

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to parse rent roll: {ex.Message}";
        }

        return result;
    }

    private static Task<List<RentRollUnit>> ParseCsvAsync(Stream stream, CancellationToken ct)
    {
        var units = new List<RentRollUnit>();
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim().Replace(" ", ""),
        });

        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var unit = new RentRollUnit
            {
                UnitNumber = csv.GetField("Unit") ?? "",
                UnitType = csv.GetField("Type"),
                MonthlyRent = csv.GetField<decimal>("MonthlyRent"),
                IsOccupied = csv.GetField<bool>("Occupied"),
                SquareFeet = ParseIntOrNull(csv.GetField("SquareFeet")),
            };

            var leaseExp = csv.GetField("LeaseExpiration");
            if (DateTime.TryParse(leaseExp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var expDate))
                unit.LeaseExpiration = expDate;

            units.Add(unit);
        }

        return Task.FromResult(units);
    }

    private static List<RentRollUnit> ParseXlsx(Stream stream)
    {
        var units = new List<RentRollUnit>();
        using var doc = SpreadsheetDocument.Open(stream, false);
        var wbPart = doc.WorkbookPart ?? throw new InvalidOperationException("No workbook found.");
        var wsPart = wbPart.WorksheetParts.First();
        var rows = wsPart.Worksheet.Descendants<Row>().ToList();

        if (rows.Count < 2) return units;

        // Read header row to find column indices
        var headerCells = rows[0].Elements<Cell>().ToList();
        var headers = headerCells.Select(c => GetCellValue(c, wbPart).Trim().Replace(" ", "")).ToList();

        int ColIdx(string name) => headers.FindIndex(h => h.Equals(name, StringComparison.OrdinalIgnoreCase));

        var unitIdx = ColIdx("Unit");
        var typeIdx = ColIdx("Type");
        var rentIdx = ColIdx("MonthlyRent");
        var occIdx = ColIdx("Occupied");
        var leaseIdx = ColIdx("LeaseExpiration");
        var sqftIdx = ColIdx("SquareFeet");

        for (int i = 1; i < rows.Count; i++)
        {
            var cells = rows[i].Elements<Cell>().ToList();
            string CellVal(int idx) => idx >= 0 && idx < cells.Count ? GetCellValue(cells[idx], wbPart) : "";

            var unit = new RentRollUnit
            {
                UnitNumber = CellVal(unitIdx),
                UnitType = typeIdx >= 0 ? CellVal(typeIdx) : null,
                MonthlyRent = decimal.TryParse(CellVal(rentIdx), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 0,
                IsOccupied = bool.TryParse(CellVal(occIdx), out var occ) && occ,
                SquareFeet = ParseIntOrNull(CellVal(sqftIdx)),
            };

            var leaseVal = CellVal(leaseIdx);
            if (DateTime.TryParse(leaseVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exp))
                unit.LeaseExpiration = exp;

            units.Add(unit);
        }

        return units;
    }

    private static string GetCellValue(Cell cell, WorkbookPart wbPart)
    {
        var value = cell.CellValue?.Text ?? "";
        if (cell.DataType?.Value == CellValues.SharedString)
        {
            var sst = wbPart.SharedStringTablePart?.SharedStringTable;
            if (sst != null && int.TryParse(value, out var idx))
                value = sst.ElementAt(idx).InnerText;
        }
        return value;
    }

    private static int? ParseIntOrNull(string? value) =>
        int.TryParse(value, out var n) ? n : null;
}
