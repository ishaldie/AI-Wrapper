using System.Globalization;
using System.Text.RegularExpressions;
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

    // Flexible header aliases — normalized (lowercase, no spaces/punctuation)
    private static readonly Dictionary<string, string[]> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Unit"] = ["unit", "unitnumber", "unitno", "unit#", "unitnum", "unitid", "apt", "suite"],
        ["Type"] = ["type", "unittype", "bedrooms", "beds", "br", "floorplan", "plan", "style"],
        ["MonthlyRent"] = ["monthlyrent", "rent", "rentmo", "rent/mo", "currentrent", "contractrent", "marketrent", "scheduledrent", "rentamount"],
        ["Occupied"] = ["occupied", "status", "occupancy", "vacant", "leased", "occupancystatus"],
        ["LeaseExpiration"] = ["leaseexpiration", "leaseexp", "leaseend", "leaseenddate", "expiration", "expires", "moveout"],
        ["SquareFeet"] = ["squarefeet", "sqft", "sf", "sq.ft.", "sqfeet", "area", "size", "unitsqft", "unitsf"],
    };

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
            PrepareHeaderForMatch = args => NormalizeHeader(args.Header),
        });

        csv.Read();
        csv.ReadHeader();

        // Map CSV headers to our canonical names
        var headerMap = MapHeaders(csv.HeaderRecord ?? Array.Empty<string>());

        while (csv.Read())
        {
            string Field(string canonical) =>
                headerMap.TryGetValue(canonical, out var csvHeader) ? csv.GetField(csvHeader) ?? "" : "";

            var unit = new RentRollUnit
            {
                UnitNumber = CellSanitizer.SanitizeCellValue(Field("Unit")),
                UnitType = CellSanitizer.SanitizeCellValue(Field("Type")),
                MonthlyRent = ParseDecimal(Field("MonthlyRent")),
                IsOccupied = ParseOccupied(Field("Occupied")),
                SquareFeet = ParseIntOrNull(Field("SquareFeet")),
            };

            var leaseVal = Field("LeaseExpiration");
            if (DateTime.TryParse(leaseVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out var expDate))
                unit.LeaseExpiration = expDate;

            // Skip rows with no unit identifier and no rent
            if (string.IsNullOrWhiteSpace(unit.UnitNumber) && unit.MonthlyRent == 0)
                continue;

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

        // Build header map from first row using cell references (not list indices)
        var headerRow = GetCellsByColumn(rows[0], wbPart);
        var headerMap = MapHeaders(headerRow.Values.ToArray());

        // Map canonical names to column indices
        var colMap = new Dictionary<string, int>();
        foreach (var (canonical, headerName) in headerMap)
        {
            var col = headerRow.FirstOrDefault(kv =>
                NormalizeHeader(kv.Value) == NormalizeHeader(headerName));
            if (col.Value != null)
                colMap[canonical] = col.Key;
        }

        for (int i = 1; i < rows.Count; i++)
        {
            var cellsByCol = GetCellsByColumn(rows[i], wbPart);

            string CellVal(string canonical) =>
                colMap.TryGetValue(canonical, out var colIdx) && cellsByCol.TryGetValue(colIdx, out var val) ? val : "";

            var unit = new RentRollUnit
            {
                UnitNumber = CellSanitizer.SanitizeCellValue(CellVal("Unit")),
                UnitType = colMap.ContainsKey("Type") ? CellSanitizer.SanitizeCellValue(CellVal("Type")) : null,
                MonthlyRent = ParseDecimal(CellVal("MonthlyRent")),
                IsOccupied = ParseOccupied(CellVal("Occupied")),
                SquareFeet = ParseIntOrNull(CellVal("SquareFeet")),
            };

            var leaseVal = CellVal("LeaseExpiration");
            if (DateTime.TryParse(leaseVal, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exp))
                unit.LeaseExpiration = exp;

            // Skip rows with no unit identifier and no rent
            if (string.IsNullOrWhiteSpace(unit.UnitNumber) && unit.MonthlyRent == 0)
                continue;

            units.Add(unit);
        }

        return units;
    }

    /// <summary>
    /// Maps canonical field names to actual header strings found in the file.
    /// </summary>
    private static Dictionary<string, string> MapHeaders(string[] headers)
    {
        var result = new Dictionary<string, string>();
        foreach (var (canonical, aliases) in HeaderAliases)
        {
            foreach (var header in headers)
            {
                var normalized = NormalizeHeader(header);
                if (aliases.Any(a => a.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    result[canonical] = header;
                    break;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Extracts cells from a row keyed by 0-based column index (derived from cell reference).
    /// Handles sparse rows where OpenXML omits empty cells.
    /// </summary>
    private static Dictionary<int, string> GetCellsByColumn(Row row, WorkbookPart wbPart)
    {
        var result = new Dictionary<int, string>();
        foreach (var cell in row.Elements<Cell>())
        {
            var colIndex = GetColumnIndex(cell.CellReference?.Value);
            if (colIndex >= 0)
                result[colIndex] = GetCellValue(cell, wbPart);
        }
        return result;
    }

    /// <summary>
    /// Converts a cell reference like "B3" to a 0-based column index (1).
    /// </summary>
    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrEmpty(cellReference)) return -1;
        var match = Regex.Match(cellReference, @"^([A-Z]+)");
        if (!match.Success) return -1;

        var letters = match.Value;
        int index = 0;
        foreach (var ch in letters)
            index = index * 26 + (ch - 'A' + 1);
        return index - 1; // 0-based
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

    private static string NormalizeHeader(string header) =>
        Regex.Replace(header.Trim(), @"[\s\-_./\\#]+", "").ToLowerInvariant();

    private static decimal ParseDecimal(string value)
    {
        // Strip currency symbols, commas, whitespace
        var cleaned = Regex.Replace(value, @"[$,\s]", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    private static bool ParseOccupied(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim().ToLowerInvariant();
        // "occupied", "leased", "yes", "true", "1" → true
        // "vacant", "no", "false", "0", "available" → false
        return v is "true" or "1" or "yes" or "occupied" or "leased" or "current";
    }

    private static int? ParseIntOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = Regex.Replace(value, @"[,\s]", "");
        return int.TryParse(cleaned, out var n) ? n : null;
    }
}
