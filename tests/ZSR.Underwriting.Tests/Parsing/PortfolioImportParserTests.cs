using System.Text;
using ZSR.Underwriting.Infrastructure.Parsing;

namespace ZSR.Underwriting.Tests.Parsing;

public class PortfolioImportParserTests
{
    private readonly PortfolioImportParser _parser = new();

    [Fact]
    public async Task ParseAsync_Csv_StandardHeaders_ReturnsRows()
    {
        var csv = "Property Name,Address,Units,Purchase Price,Monthly Rent,T12 NOI,LTV (%),Interest Rate (%),CapEx Budget\n" +
                  "Sunset Apartments,123 Main St,24,\"$2,500,000\",\"$18,000\",\"$150,000\",75,5.25,\"$100,000\"\n" +
                  "River Place,456 Oak Ave,12,\"$1,200,000\",\"$9,500\",,65,4.75,\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = await _parser.ParseAsync(stream, "test.csv");

        Assert.Equal(2, rows.Count);

        Assert.Equal("Sunset Apartments", rows[0].PropertyName);
        Assert.Equal("123 Main St", rows[0].Address);
        Assert.Equal(24, rows[0].UnitCount);
        Assert.Equal(2_500_000m, rows[0].PurchasePrice);
        Assert.Equal(18_000m, rows[0].RentRollSummary);
        Assert.Equal(150_000m, rows[0].T12Summary);
        Assert.Equal(75m, rows[0].LoanLtv);
        Assert.Equal(5.25m, rows[0].LoanRate);
        Assert.Equal(100_000m, rows[0].CapexBudget);

        Assert.Equal("River Place", rows[1].PropertyName);
        Assert.Null(rows[1].T12Summary);
        Assert.Null(rows[1].CapexBudget);
    }

    [Fact]
    public async Task ParseAsync_Csv_AliasHeaders_MapsCorrectly()
    {
        var csv = "Asset,Location,Total Units,Price,Gross Rent,NOI,LTV (%),Rate,Rehab Budget\n" +
                  "Park Tower,789 Elm Blvd,48,\"$5,000,000\",\"$35,000\",\"$280,000\",70,5.5,\"$200,000\"\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = await _parser.ParseAsync(stream, "aliases.csv");

        Assert.Single(rows);
        Assert.Equal("Park Tower", rows[0].PropertyName);
        Assert.Equal("789 Elm Blvd", rows[0].Address);
        Assert.Equal(48, rows[0].UnitCount);
        Assert.Equal(5_000_000m, rows[0].PurchasePrice);
    }

    [Fact]
    public async Task ParseAsync_Csv_SkipsEmptyRows()
    {
        var csv = "Property Name,Address\n" +
                  "Valid Property,123 Main St\n" +
                  ",\n" +
                  "  ,  \n" +
                  "Another Property,456 Oak Ave\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = await _parser.ParseAsync(stream, "empty.csv");

        Assert.Equal(2, rows.Count);
        Assert.Equal("Valid Property", rows[0].PropertyName);
        Assert.Equal("Another Property", rows[1].PropertyName);
    }

    [Fact]
    public async Task ParseAsync_Csv_CurrencyParsing_HandlesFormats()
    {
        var csv = "Property Name,Address,Purchase Price\n" +
                  "Deal A,Addr A,\"$1,250,000\"\n" +
                  "Deal B,Addr B,1250000\n" +
                  "Deal C,Addr C,\"$1250000.50\"\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = await _parser.ParseAsync(stream, "currency.csv");

        Assert.Equal(3, rows.Count);
        Assert.Equal(1_250_000m, rows[0].PurchasePrice);
        Assert.Equal(1_250_000m, rows[1].PurchasePrice);
        Assert.Equal(1_250_000.50m, rows[2].PurchasePrice);
    }

    [Fact]
    public async Task ParseAsync_Csv_FormulaInjection_Sanitized()
    {
        var csv = "Property Name,Address\n" +
                  "=CMD('calc'),=HYPERLINK(\"http://evil.com\")\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = await _parser.ParseAsync(stream, "injection.csv");

        Assert.Single(rows);
        Assert.DoesNotContain("=", rows[0].PropertyName);
        Assert.DoesNotContain("=", rows[0].Address);
    }

    [Fact]
    public async Task ParseAsync_Csv_PercentageParsing_StripsPercent()
    {
        var csv = "Property Name,Address,LTV (%),Interest Rate (%)\n" +
                  "Deal A,Addr A,75%,5.25%\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = await _parser.ParseAsync(stream, "percent.csv");

        Assert.Single(rows);
        Assert.Equal(75m, rows[0].LoanLtv);
        Assert.Equal(5.25m, rows[0].LoanRate);
    }

    [Fact]
    public async Task ParseAsync_Csv_RowNumbers_AreCorrect()
    {
        var csv = "Property Name,Address\n" +
                  "Deal A,Addr A\n" +
                  "Deal B,Addr B\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = await _parser.ParseAsync(stream, "rownum.csv");

        Assert.Equal(2, rows[0].RowNumber); // row 1 is header, data starts at row 2
        Assert.Equal(3, rows[1].RowNumber);
    }

    [Fact]
    public void CanParse_AcceptsXlsxAndCsv()
    {
        Assert.True(_parser.CanParse("test.xlsx"));
        Assert.True(_parser.CanParse("test.csv"));
        Assert.True(_parser.CanParse("TEST.CSV"));
        Assert.False(_parser.CanParse("test.pdf"));
        Assert.False(_parser.CanParse("test.docx"));
    }

    [Fact]
    public async Task ParseAsync_UnsupportedExtension_Throws()
    {
        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _parser.ParseAsync(stream, "test.pdf"));
    }
}
