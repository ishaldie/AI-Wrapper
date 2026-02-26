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

public class T12Parser : IDocumentParser
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".csv" };

    // Flexible header aliases for category column
    private static readonly string[] CategoryAliases =
        ["category", "description", "lineitem", "item", "account", "accountname", "name", "glcode", "glname"];

    // Flexible header aliases for amount column
    private static readonly string[] AmountAliases =
        ["annualamount", "annual", "total", "totalamount", "amount", "annualtotal", "ytd", "t12", "trailing12", "12month", "12mo"];

    // Section header detection
    private static readonly string[] RevenueSectionHeaders =
        ["revenue", "revenues", "income", "grossincome", "grossrevenue", "rentalincome", "totalrevenue"];
    private static readonly string[] ExpenseSectionHeaders =
        ["expenses", "expense", "operatingexpenses", "opex", "totalexpenses"];

    public DocumentType SupportedType => DocumentType.T12PAndL;

    public bool CanParse(string fileName) =>
        SupportedExtensions.Contains(Path.GetExtension(fileName));

    public async Task<ParsedDocumentResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var result = new ParsedDocumentResult { DocumentType = DocumentType.T12PAndL };

        try
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var (revenue, expenses) = ext == ".csv"
                ? await ParseCsvAsync(fileStream, ct)
                : ParseXlsx(fileStream);

            if (revenue.Count == 0 && expenses.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No revenue or expense data found in file.";
                return result;
            }

            result.RevenueItems = revenue;
            result.ExpenseItems = expenses;
            result.GrossRevenue = revenue.Sum(r => r.AnnualAmount);
            result.TotalExpenses = expenses.Sum(e => e.AnnualAmount);
            result.NetOperatingIncome = result.GrossRevenue - result.TotalExpenses;
            result.EffectiveGrossIncome = result.GrossRevenue;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to parse T12: {ex.Message}";
        }

        return result;
    }

    private static Task<(List<T12LineItem> revenue, List<T12LineItem> expenses)> ParseCsvAsync(Stream stream, CancellationToken ct)
    {
        var revenue = new List<T12LineItem>();
        var expenses = new List<T12LineItem>();
        var currentSection = "";

        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
        });

        csv.Read();
        csv.ReadHeader();

        // Find the actual header names using flexible matching
        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        var categoryHeader = FindHeader(headers, CategoryAliases);
        var amountHeader = FindHeader(headers, AmountAliases);

        while (csv.Read())
        {
            var rawCategory = (categoryHeader != null ? csv.GetField(categoryHeader) : csv.GetField(0))?.Trim() ?? "";
            var category = CellSanitizer.SanitizeCellValue(rawCategory);
            var amountStr = (amountHeader != null ? csv.GetField(amountHeader) : csv.GetField(1))?.Trim() ?? "";

            var normalized = SpreadsheetHelper.NormalizeHeader(category);

            if (RevenueSectionHeaders.Contains(normalized))
            {
                currentSection = "revenue";
                continue;
            }
            if (ExpenseSectionHeaders.Contains(normalized))
            {
                currentSection = "expenses";
                continue;
            }

            // Skip total/subtotal rows
            if (normalized.Contains("total") || normalized.Contains("subtotal") || normalized.Contains("noi") || normalized.Contains("netoperating"))
                continue;

            if (ParseAmount(amountStr, out var amount))
            {
                var item = new T12LineItem
                {
                    Category = category,
                    AnnualAmount = amount,
                    MonthlyAmount = Math.Round(amount / 12, 2)
                };

                if (currentSection == "expenses")
                    expenses.Add(item);
                else
                    revenue.Add(item);
            }
        }

        return Task.FromResult((revenue, expenses));
    }

    private static (List<T12LineItem> revenue, List<T12LineItem> expenses) ParseXlsx(Stream stream)
    {
        var revenue = new List<T12LineItem>();
        var expenses = new List<T12LineItem>();
        var currentSection = "";

        using var doc = SpreadsheetDocument.Open(stream, false);
        var wbPart = doc.WorkbookPart ?? throw new InvalidOperationException("No workbook found.");
        var wsPart = wbPart.WorksheetParts.First();
        var rows = wsPart.Worksheet.Descendants<Row>().ToList();

        if (rows.Count < 2) return (revenue, expenses);

        // Read header row using cell references (not indices)
        var headerCells = SpreadsheetHelper.GetCellsByColumn(rows[0], wbPart);
        var headerValues = headerCells.Values.ToArray();

        // Find which column holds category and which holds amount
        int categoryCol = FindHeaderColumn(headerCells, CategoryAliases);
        int amountCol = FindHeaderColumn(headerCells, AmountAliases);

        // Fallback: if no matching headers found, use first two columns
        if (categoryCol < 0) categoryCol = headerCells.Keys.OrderBy(k => k).FirstOrDefault();
        if (amountCol < 0) amountCol = headerCells.Keys.OrderBy(k => k).Skip(1).FirstOrDefault();

        for (int i = 1; i < rows.Count; i++)
        {
            var cellsByCol = SpreadsheetHelper.GetCellsByColumn(rows[i], wbPart);

            var category = cellsByCol.TryGetValue(categoryCol, out var catVal) ? CellSanitizer.SanitizeCellValue(catVal.Trim()) : "";
            var amountStr = cellsByCol.TryGetValue(amountCol, out var amtVal) ? amtVal.Trim() : "";

            var normalized = SpreadsheetHelper.NormalizeHeader(category);

            if (RevenueSectionHeaders.Contains(normalized))
            {
                currentSection = "revenue";
                continue;
            }
            if (ExpenseSectionHeaders.Contains(normalized))
            {
                currentSection = "expenses";
                continue;
            }

            // Skip empty rows, total/subtotal rows
            if (string.IsNullOrWhiteSpace(category)) continue;
            if (normalized.Contains("total") || normalized.Contains("subtotal") || normalized.Contains("noi") || normalized.Contains("netoperating"))
                continue;

            if (ParseAmount(amountStr, out var amount))
            {
                var item = new T12LineItem
                {
                    Category = category,
                    AnnualAmount = amount,
                    MonthlyAmount = Math.Round(amount / 12, 2)
                };

                if (currentSection == "expenses")
                    expenses.Add(item);
                else
                    revenue.Add(item);
            }
        }

        return (revenue, expenses);
    }

    private static string? FindHeader(string[] headers, string[] aliases)
    {
        foreach (var header in headers)
        {
            var normalized = SpreadsheetHelper.NormalizeHeader(header);
            if (aliases.Any(a => a.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                return header;
        }
        return null;
    }

    private static int FindHeaderColumn(Dictionary<int, string> headerCells, string[] aliases)
    {
        foreach (var (colIdx, headerValue) in headerCells)
        {
            var normalized = SpreadsheetHelper.NormalizeHeader(headerValue);
            if (aliases.Any(a => a.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                return colIdx;
        }
        return -1;
    }

    private static bool ParseAmount(string value, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        // Strip currency symbols, commas, whitespace, parentheses (negative)
        var cleaned = value.Trim();
        bool negative = cleaned.StartsWith('(') && cleaned.EndsWith(')');
        cleaned = Regex.Replace(cleaned, @"[$,\s()]+", "");
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
        {
            if (negative) amount = -amount;
            return true;
        }
        return false;
    }
}
