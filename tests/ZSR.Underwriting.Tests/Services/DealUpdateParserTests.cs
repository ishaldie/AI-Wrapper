using ZSR.Underwriting.Application.Services;

namespace ZSR.Underwriting.Tests.Services;

public class DealUpdateParserTests
{
    [Fact]
    public void Parse_returns_null_when_no_block()
    {
        var response = "Here is my analysis of the property. It looks great!";
        var result = DealUpdateParser.Parse(response);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_extracts_general_data()
    {
        var response = """
            Here is my analysis.

            ```deal-update
            {
              "general": {
                "propertyName": "Sunrise Apartments",
                "address": "123 Main St, Austin, TX 78701",
                "unitCount": 50,
                "yearBuilt": 1985,
                "buildingType": "Garden",
                "squareFootage": 45000
              }
            }
            ```

            The property looks promising.
            """;

        var result = DealUpdateParser.Parse(response);

        Assert.NotNull(result);
        Assert.NotNull(result.General);
        Assert.Equal("Sunrise Apartments", result.General.PropertyName);
        Assert.Equal("123 Main St, Austin, TX 78701", result.General.Address);
        Assert.Equal(50, result.General.UnitCount);
        Assert.Equal(1985, result.General.YearBuilt);
        Assert.Equal("Garden", result.General.BuildingType);
        Assert.Equal(45000, result.General.SquareFootage);
    }

    [Fact]
    public void Parse_extracts_underwriting_data()
    {
        var response = """
            Analysis complete.

            ```deal-update
            {
              "underwriting": {
                "grossPotentialRent": 600000,
                "vacancyLoss": 30000,
                "effectiveGrossIncome": 570000,
                "operatingExpenses": 228000,
                "netOperatingIncome": 342000,
                "goingInCapRate": 6.84,
                "pricePerUnit": 100000
              }
            }
            ```
            """;

        var result = DealUpdateParser.Parse(response);

        Assert.NotNull(result);
        Assert.NotNull(result.Underwriting);
        Assert.Equal(600000m, result.Underwriting.GrossPotentialRent);
        Assert.Equal(342000m, result.Underwriting.NetOperatingIncome);
        Assert.Equal(6.84m, result.Underwriting.GoingInCapRate);
    }

    [Fact]
    public void Parse_extracts_checklist_items()
    {
        var response = """
            ```deal-update
            {
              "checklist": [
                {"item": "Current Months Rent Roll", "status": "Outstanding"},
                {"item": "Trailing 12 Month Operating Statement", "status": "Satisfied"}
              ]
            }
            ```
            """;

        var result = DealUpdateParser.Parse(response);

        Assert.NotNull(result);
        Assert.NotNull(result.Checklist);
        Assert.Equal(2, result.Checklist.Count);
        Assert.Equal("Current Months Rent Roll", result.Checklist[0].Item);
        Assert.Equal("Outstanding", result.Checklist[0].Status);
        Assert.Equal("Satisfied", result.Checklist[1].Status);
    }

    [Fact]
    public void Parse_extracts_capital_stack()
    {
        var response = """
            ```deal-update
            {
              "underwriting": {
                "netOperatingIncome": 342000,
                "capitalStack": [
                  {"source": "SeniorDebt", "amount": 3500000, "rate": 5.5, "termYears": 10},
                  {"source": "SponsorEquity", "amount": 1500000}
                ]
              }
            }
            ```
            """;

        var result = DealUpdateParser.Parse(response);

        Assert.NotNull(result?.Underwriting?.CapitalStack);
        Assert.Equal(2, result.Underwriting.CapitalStack.Count);
        Assert.Equal("SeniorDebt", result.Underwriting.CapitalStack[0].Source);
        Assert.Equal(3500000m, result.Underwriting.CapitalStack[0].Amount);
        Assert.Equal(5.5m, result.Underwriting.CapitalStack[0].Rate);
        Assert.Equal(10, result.Underwriting.CapitalStack[0].TermYears);
    }

    [Fact]
    public void Parse_handles_all_sections_together()
    {
        var response = """
            ```deal-update
            {
              "general": {"propertyName": "Test", "unitCount": 10},
              "underwriting": {"netOperatingIncome": 100000},
              "checklist": [{"item": "Rent Roll", "status": "Outstanding"}]
            }
            ```
            """;

        var result = DealUpdateParser.Parse(response);

        Assert.NotNull(result);
        Assert.NotNull(result.General);
        Assert.NotNull(result.Underwriting);
        Assert.NotNull(result.Checklist);
        Assert.Single(result.Checklist);
    }

    [Fact]
    public void Parse_handles_malformed_json_gracefully()
    {
        var response = """
            ```deal-update
            { invalid json here
            ```
            """;

        var result = DealUpdateParser.Parse(response);
        Assert.Null(result);
    }

    [Fact]
    public void StripBlocks_removes_deal_update_block()
    {
        var response = """
            Here is my analysis of the property.

            ```deal-update
            {"general": {"propertyName": "Test"}}
            ```

            The property looks promising.
            """;

        var stripped = DealUpdateParser.StripBlocks(response);

        Assert.DoesNotContain("deal-update", stripped);
        Assert.DoesNotContain("propertyName", stripped);
        Assert.Contains("Here is my analysis", stripped);
        Assert.Contains("The property looks promising", stripped);
    }

    [Fact]
    public void StripBlocks_returns_original_when_no_block()
    {
        var response = "No structured data here.";
        var stripped = DealUpdateParser.StripBlocks(response);
        Assert.Equal(response, stripped);
    }

    [Fact]
    public void StripBlocks_removes_multiple_blocks()
    {
        var response = """
            First part.

            ```deal-update
            {"general": {"propertyName": "A"}}
            ```

            Middle text.

            ```deal-update
            {"underwriting": {"netOperatingIncome": 100}}
            ```

            End.
            """;

        var stripped = DealUpdateParser.StripBlocks(response);

        Assert.DoesNotContain("deal-update", stripped);
        Assert.Contains("First part", stripped);
        Assert.Contains("Middle text", stripped);
        Assert.Contains("End", stripped);
    }

    [Fact]
    public void Parse_handles_partial_data_gracefully()
    {
        var response = """
            ```deal-update
            {
              "general": {"yearBuilt": 2001}
            }
            ```
            """;

        var result = DealUpdateParser.Parse(response);

        Assert.NotNull(result?.General);
        Assert.Null(result.General.PropertyName);
        Assert.Null(result.General.Address);
        Assert.Equal(2001, result.General.YearBuilt);
    }
}
