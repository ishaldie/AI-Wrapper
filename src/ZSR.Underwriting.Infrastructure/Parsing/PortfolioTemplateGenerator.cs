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
        "Property Type",
        "Units",
        "Licensed Beds",
        "Purchase Price",
        "Monthly Rent",
        "T12 NOI",
        "LTV (%)",
        "Interest Rate (%)",
        "CapEx Budget",
        "Average Daily Rate",
        "Private Pay %",
    ];

    private static readonly string[][] SampleRows =
    [
        ["Sunset Apartments", "123 Main St, Austin TX 78701", "Multifamily", "24", "", "$2,500,000", "$18,000", "$150,000", "75", "5.25", "$100,000", "", ""],
        ["River Place", "456 Oak Ave, Denver CO 80202", "Bridge", "48", "", "$4,800,000", "$28,000", "", "65", "7.25", "$350,000", "", ""],
        ["Sunrise Senior Living", "789 Elm Blvd, Phoenix AZ 85001", "AssistedLiving", "", "120", "$18,000,000", "", "$1,200,000", "60", "5.75", "$500,000", "$250", "65"],
        ["Harbor Inn", "321 Coast Hwy, San Diego CA 92101", "Hospitality", "85", "", "$12,000,000", "", "$800,000", "60", "6.50", "$200,000", "$175", ""],
        ["Oak Street Office", "555 Oak St, Chicago IL 60601", "Commercial", "", "", "$8,500,000", "", "$550,000", "70", "5.50", "", "", ""],
        ["Pinewood LIHTC", "100 Pine Rd, Portland OR 97201", "LIHTC", "60", "", "$9,000,000", "$7,500", "$420,000", "80", "4.25", "", "", ""],
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
