using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Infrastructure.Parsing;

public class LoanTermSheetParser : IDocumentParser
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".csv" };

    public DocumentType SupportedType => DocumentType.LoanTermSheet;

    public bool CanParse(string fileName) =>
        SupportedExtensions.Contains(Path.GetExtension(fileName));

    public async Task<ParsedDocumentResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var result = new ParsedDocumentResult { DocumentType = DocumentType.LoanTermSheet };

        try
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var terms = ext == ".csv"
                ? await ParseCsvAsync(fileStream, ct)
                : ParseXlsx(fileStream);

            if (terms.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No loan terms found in file.";
                return result;
            }

            ApplyTerms(result, terms);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to parse loan term sheet: {ex.Message}";
        }

        return result;
    }

    private static void ApplyTerms(ParsedDocumentResult result, Dictionary<string, string> terms)
    {
        foreach (var (key, value) in terms)
        {
            var normalizedKey = key.Trim().ToLowerInvariant().Replace(" ", "").Replace("(", "").Replace(")", "");

            switch (normalizedKey)
            {
                case "loanamount":
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var loanAmt))
                        result.LoanAmount = loanAmt;
                    break;
                case "interestrate" or "rate":
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                        result.InterestRate = rate;
                    break;
                case "ltv" or "ltvratio" or "loantovalue":
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var ltv))
                        result.LtvRatio = ltv;
                    break;
                case "loanterm" or "term" or "loantermyears":
                    if (int.TryParse(value, out var term))
                        result.LoanTermYears = term;
                    break;
                case "amortization" or "amortizationyears":
                    if (int.TryParse(value, out var amort))
                        result.AmortizationYears = amort;
                    break;
                case "interestonly" or "io":
                    result.IsInterestOnly = value.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase)
                        || value.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
                    break;
                case "ioperiodmonths" or "ioperiod" or "iotermmonths":
                    if (int.TryParse(value, out var ioMonths))
                        result.IoTermMonths = ioMonths;
                    break;
                case "prepayment" or "prepaymentterms" or "prepaymentpenalty":
                    result.PrepaymentTerms = value.Trim();
                    break;
            }
        }
    }

    private static Task<Dictionary<string, string>> ParseCsvAsync(Stream stream, CancellationToken ct)
    {
        var terms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
            var term = csv.GetField("Term")?.Trim() ?? "";
            var value = csv.GetField("Value")?.Trim() ?? "";
            if (!string.IsNullOrEmpty(term))
                terms[term] = value;
        }

        return Task.FromResult(terms);
    }

    private static Dictionary<string, string> ParseXlsx(Stream stream)
    {
        var terms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var doc = SpreadsheetDocument.Open(stream, false);
        var wbPart = doc.WorkbookPart ?? throw new InvalidOperationException("No workbook found.");
        var wsPart = wbPart.WorksheetParts.First();
        var rows = wsPart.Worksheet.Descendants<Row>().ToList();

        if (rows.Count < 2) return terms;

        // Skip header, read key-value pairs
        for (int i = 1; i < rows.Count; i++)
        {
            var cells = rows[i].Elements<Cell>().ToList();
            var term = cells.Count > 0 ? SpreadsheetHelper.GetCellValue(cells[0], wbPart).Trim() : "";
            var value = cells.Count > 1 ? SpreadsheetHelper.GetCellValue(cells[1], wbPart).Trim() : "";

            if (!string.IsNullOrEmpty(term))
                terms[term] = value;
        }

        return terms;
    }

}
