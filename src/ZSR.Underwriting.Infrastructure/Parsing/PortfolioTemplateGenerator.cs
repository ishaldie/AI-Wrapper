using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ZSR.Underwriting.Infrastructure.Parsing;

public static class PortfolioTemplateGenerator
{
    private static readonly string[] Headers =
    [
        "Property Name",
        "Address",
        "Units",
        "Purchase Price",
        "Monthly Rent",
        "T12 NOI",
        "LTV (%)",
        "Interest Rate (%)",
        "CapEx Budget",
    ];

    private static readonly string[][] SampleRows =
    [
        ["Sunset Apartments", "123 Main St, Austin TX 78701", "24", "$2,500,000", "$18,000", "$150,000", "75", "5.25", "$100,000"],
        ["River Place", "456 Oak Ave, Denver CO 80202", "12", "$1,200,000", "$9,500", "", "65", "4.75", ""],
    ];

    public static MemoryStream Generate()
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
                Name = "Portfolio Import"
            });

            // Header row
            var headerRow = new Row();
            foreach (var h in Headers)
                headerRow.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(h) });
            sheetData.Append(headerRow);

            // Sample rows
            foreach (var sampleData in SampleRows)
            {
                var row = new Row();
                foreach (var val in sampleData)
                    row.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(val) });
                sheetData.Append(row);
            }
        }

        ms.Position = 0;
        return ms;
    }
}
