using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ZSR.Underwriting.Infrastructure.Parsing;

internal static class SpreadsheetHelper
{
    /// <summary>
    /// Resolves a cell's display value, handling SharedString references.
    /// </summary>
    internal static string GetCellValue(Cell cell, WorkbookPart wbPart)
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

    /// <summary>
    /// Extracts cells from a row keyed by 0-based column index (derived from cell reference).
    /// Handles sparse rows where OpenXML omits empty cells.
    /// </summary>
    internal static Dictionary<int, string> GetCellsByColumn(Row row, WorkbookPart wbPart)
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
    internal static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrEmpty(cellReference)) return -1;
        var match = Regex.Match(cellReference, @"^([A-Z]+)");
        if (!match.Success) return -1;

        var letters = match.Value;
        int index = 0;
        foreach (var ch in letters)
            index = index * 26 + (ch - 'A' + 1);
        return index - 1;
    }

    /// <summary>
    /// Normalizes a header string for flexible matching (strips whitespace, punctuation, lowercases).
    /// </summary>
    internal static string NormalizeHeader(string header) =>
        Regex.Replace(header.Trim(), @"[\s\-_./\\#]+", "").ToLowerInvariant();
}
