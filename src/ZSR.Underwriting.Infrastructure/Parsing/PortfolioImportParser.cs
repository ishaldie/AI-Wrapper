using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ZSR.Underwriting.Application.DTOs;

namespace ZSR.Underwriting.Infrastructure.Parsing;

public class PortfolioImportParser
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xlsx", ".csv" };

    // Flexible header aliases — normalized (lowercase, no spaces/punctuation)
    private static readonly Dictionary<string, string[]> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PropertyName"] = ["propertyname", "asset", "assetname", "dealname", "property", "name"],
        ["Address"] = ["address", "location", "propertyaddress", "streetaddress", "street"],
        ["UnitCount"] = ["units", "totalunits", "unitcount", "numberofunits", "numunits", "noofunits"],
        ["PurchasePrice"] = ["purchaseprice", "price", "askingprice", "acquisitionprice", "listprice", "saleprice"],
        ["RentRollSummary"] = ["monthlyrent", "grossrent", "totalrent", "rentrollsummary", "monthlyincome", "grossincome"],
        ["T12Summary"] = ["t12noi", "noi", "netoperatingincome", "t12summary", "annualnoi", "trailingnoi"],
        ["LoanLtv"] = ["ltv", "ltv(%)", "loantovalueratio", "loanltv", "leverageratio"],
        ["LoanRate"] = ["interestrate", "interestrate(%)", "rate", "loanrate", "couponrate", "mortgagerate"],
        ["CapexBudget"] = ["capexbudget", "rehabbudget", "renovationbudget", "capex", "rehab", "capitalexpenditures"],
        ["PropertyType"] = ["propertytype", "assettype", "type", "assetclass", "facilitytype"],
        ["LicensedBeds"] = ["licensedbeds", "beds", "totalbeds", "bedcount"],
        ["AverageDailyRate"] = ["averagedailyrate", "adr", "dailyrate", "rateperday"],
        ["PrivatePayPct"] = ["privatepaypct", "privatepay", "privatepay%", "privpay"],
    };

    public bool CanParse(string fileName) =>
        SupportedExtensions.Contains(Path.GetExtension(fileName));

    public Task<List<BulkImportRowDto>> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        if (!CanParse(fileName))
            throw new ArgumentException($"Unsupported file type: {Path.GetExtension(fileName)}");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var rows = ext == ".csv"
            ? ParseCsv(fileStream)
            : ParseXlsx(fileStream);

        return Task.FromResult(rows);
    }

    private static List<BulkImportRowDto> ParseCsv(Stream stream)
    {
        var rows = new List<BulkImportRowDto>();
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
            PrepareHeaderForMatch = args => SpreadsheetHelper.NormalizeHeader(args.Header),
        });

        csv.Read();
        csv.ReadHeader();

        var headerMap = MapHeaders(csv.HeaderRecord ?? Array.Empty<string>());
        int rowNum = 1;

        while (csv.Read())
        {
            rowNum++;

            string Field(string canonical) =>
                headerMap.TryGetValue(canonical, out var csvHeader) ? csv.GetField(csvHeader) ?? "" : "";

            var row = BuildRow(rowNum, Field);

            // Skip completely empty rows
            if (string.IsNullOrWhiteSpace(row.PropertyName) && string.IsNullOrWhiteSpace(row.Address))
                continue;

            rows.Add(row);
        }

        return rows;
    }

    private static List<BulkImportRowDto> ParseXlsx(Stream stream)
    {
        var rows = new List<BulkImportRowDto>();
        using var doc = SpreadsheetDocument.Open(stream, false);
        var wbPart = doc.WorkbookPart ?? throw new InvalidOperationException("No workbook found.");
        var wsPart = wbPart.WorksheetParts.First();
        var allRows = wsPart.Worksheet.Descendants<Row>().ToList();

        if (allRows.Count < 2) return rows;

        // Build header map from first row
        var headerRow = SpreadsheetHelper.GetCellsByColumn(allRows[0], wbPart);
        var headerMap = MapHeaders(headerRow.Values.ToArray());

        // Map canonical names to column indices
        var colMap = new Dictionary<string, int>();
        foreach (var (canonical, headerName) in headerMap)
        {
            var col = headerRow.FirstOrDefault(kv =>
                SpreadsheetHelper.NormalizeHeader(kv.Value) == SpreadsheetHelper.NormalizeHeader(headerName));
            if (col.Value != null)
                colMap[canonical] = col.Key;
        }

        for (int i = 1; i < allRows.Count; i++)
        {
            var cellsByCol = SpreadsheetHelper.GetCellsByColumn(allRows[i], wbPart);

            string CellVal(string canonical) =>
                colMap.TryGetValue(canonical, out var colIdx) && cellsByCol.TryGetValue(colIdx, out var val) ? val : "";

            var row = BuildRow(i + 1, CellVal);

            // Skip completely empty rows
            if (string.IsNullOrWhiteSpace(row.PropertyName) && string.IsNullOrWhiteSpace(row.Address))
                continue;

            rows.Add(row);
        }

        return rows;
    }

    private static BulkImportRowDto BuildRow(int rowNumber, Func<string, string> field)
    {
        var propertyType = CellSanitizer.SanitizeCellValue(field("PropertyType"));
        return new BulkImportRowDto
        {
            RowNumber = rowNumber,
            PropertyName = CellSanitizer.SanitizeCellValue(field("PropertyName")),
            Address = CellSanitizer.SanitizeCellValue(field("Address")),
            UnitCount = ParseIntOrNull(field("UnitCount")),
            PurchasePrice = ParseDecimalOrNull(field("PurchasePrice")),
            RentRollSummary = ParseDecimalOrNull(field("RentRollSummary")),
            T12Summary = ParseDecimalOrNull(field("T12Summary")),
            LoanLtv = ParseDecimalOrNull(field("LoanLtv")),
            LoanRate = ParseDecimalOrNull(field("LoanRate")),
            CapexBudget = ParseDecimalOrNull(field("CapexBudget")),
            PropertyType = string.IsNullOrWhiteSpace(propertyType) ? null : propertyType,
            LicensedBeds = ParseIntOrNull(field("LicensedBeds")),
            AverageDailyRate = ParseDecimalOrNull(field("AverageDailyRate")),
            PrivatePayPct = ParseDecimalOrNull(field("PrivatePayPct")),
        };
    }

    private static Dictionary<string, string> MapHeaders(string[] headers)
    {
        var result = new Dictionary<string, string>();
        foreach (var (canonical, aliases) in HeaderAliases)
        {
            foreach (var header in headers)
            {
                var normalized = SpreadsheetHelper.NormalizeHeader(header);
                if (aliases.Any(a => a.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    result[canonical] = header;
                    break;
                }
            }
        }
        return result;
    }

    private static decimal? ParseDecimalOrNull(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = Regex.Replace(value, @"[$,\s%]", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static int? ParseIntOrNull(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = Regex.Replace(value, @"[,\s]", "");
        return int.TryParse(cleaned, out var n) ? n : null;
    }
}
