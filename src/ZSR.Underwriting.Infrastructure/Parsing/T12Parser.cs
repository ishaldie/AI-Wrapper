using System.Globalization;
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
            result.EffectiveGrossIncome = result.GrossRevenue; // Simplified: no vacancy deduction
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

        while (csv.Read())
        {
            var category = csv.GetField("Category")?.Trim() ?? "";
            var amountStr = csv.GetField("Annual Amount")?.Trim() ?? csv.GetField("AnnualAmount")?.Trim() ?? "";

            if (category.Equals("REVENUE", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "revenue";
                continue;
            }
            if (category.Equals("EXPENSES", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "expenses";
                continue;
            }

            if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
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

        // Skip header row
        for (int i = 1; i < rows.Count; i++)
        {
            var cells = rows[i].Elements<Cell>().ToList();
            var category = cells.Count > 0 ? GetCellValue(cells[0], wbPart).Trim() : "";
            var amountStr = cells.Count > 1 ? GetCellValue(cells[1], wbPart).Trim() : "";

            if (category.Equals("REVENUE", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "revenue";
                continue;
            }
            if (category.Equals("EXPENSES", StringComparison.OrdinalIgnoreCase))
            {
                currentSection = "expenses";
                continue;
            }

            if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
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
}
