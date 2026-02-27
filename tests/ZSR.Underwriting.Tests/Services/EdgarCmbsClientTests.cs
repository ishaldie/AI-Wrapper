using System.Net;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace ZSR.Underwriting.Tests.Services;

public class EdgarCmbsClientTests
{
    private const string SampleEx102Xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <assetData>
          <assets>
            <asset>
              <assetNumber>1</assetNumber>
              <originationDate>2023-06-15</originationDate>
              <originalLoanAmount>15000000</originalLoanAmount>
              <originalInterestRatePercentage>5.25</originalInterestRatePercentage>
              <maturityDate>2033-06-01</maturityDate>
              <propertyTypeCode>MF</propertyTypeCode>
              <propertyCity>Atlanta</propertyCity>
              <propertyState>GA</propertyState>
              <mostRecentDebtServiceCoverageNOI>1.35</mostRecentDebtServiceCoverageNOI>
              <mostRecentNOIAmount>1200000</mostRecentNOIAmount>
              <mostRecentValuationAmount>20000000</mostRecentValuationAmount>
              <mostRecentPhysicalOccupancyPercentage>94.5</mostRecentPhysicalOccupancyPercentage>
              <numberOfUnitsBedsRooms>120</numberOfUnitsBedsRooms>
            </asset>
            <asset>
              <assetNumber>2</assetNumber>
              <originationDate>2024-01-10</originationDate>
              <originalLoanAmount>8500000</originalLoanAmount>
              <originalInterestRatePercentage>6.10</originalInterestRatePercentage>
              <maturityDate>2034-01-10</maturityDate>
              <propertyTypeCode>HC</propertyTypeCode>
              <propertyCity>Dallas</propertyCity>
              <propertyState>TX</propertyState>
              <mostRecentDebtServiceCoverageNOI>1.22</mostRecentDebtServiceCoverageNOI>
              <mostRecentNOIAmount>750000</mostRecentNOIAmount>
              <mostRecentValuationAmount>11000000</mostRecentValuationAmount>
              <mostRecentPhysicalOccupancyPercentage>88.0</mostRecentPhysicalOccupancyPercentage>
              <numberOfUnitsBedsRooms>80</numberOfUnitsBedsRooms>
            </asset>
          </assets>
        </assetData>
        """;

    private const string SampleEx102WithNamespace = """
        <?xml version="1.0" encoding="UTF-8"?>
        <assetData xmlns="http://www.sec.gov/edgar/document/absee/cmbs">
          <assets>
            <asset>
              <assetNumber>1</assetNumber>
              <originationDate>2023-03-01</originationDate>
              <originalLoanAmount>25000000</originalLoanAmount>
              <originalInterestRatePercentage>4.85</originalInterestRatePercentage>
              <maturityDate>2033-03-01</maturityDate>
              <propertyTypeCode>MF</propertyTypeCode>
              <propertyCity>Phoenix</propertyCity>
              <propertyState>AZ</propertyState>
              <mostRecentDebtServiceCoverageNOI>1.50</mostRecentDebtServiceCoverageNOI>
              <mostRecentNOIAmount>2000000</mostRecentNOIAmount>
              <mostRecentValuationAmount>35000000</mostRecentValuationAmount>
              <mostRecentPhysicalOccupancyPercentage>96.0</mostRecentPhysicalOccupancyPercentage>
              <numberOfUnitsBedsRooms>200</numberOfUnitsBedsRooms>
            </asset>
          </assets>
        </assetData>
        """;

    [Fact]
    public void ParseEx102Xml_ExtractsMultipleAssets()
    {
        var client = CreateClient();
        var comps = client.ParseEx102Xml(SampleEx102Xml);

        Assert.Equal(2, comps.Count);
    }

    [Fact]
    public void ParseEx102Xml_ExtractsLoanFields_FirstAsset()
    {
        var client = CreateClient();
        var comps = client.ParseEx102Xml(SampleEx102Xml, "CMBS Trust 2023-C1");
        var comp = comps[0];

        Assert.Equal(SecuritizationDataSource.CMBS, comp.Source);
        Assert.Equal(PropertyType.Multifamily, comp.PropertyType);
        Assert.Equal("GA", comp.State);
        Assert.Equal("Atlanta", comp.City);
        Assert.Equal(120, comp.Units);
        Assert.Equal(15_000_000m, comp.LoanAmount);
        Assert.Equal(5.25m, comp.InterestRate);
        Assert.Equal(1.35m, comp.DSCR);
        Assert.Equal(1_200_000m, comp.NOI);
        Assert.Equal(94.5m, comp.Occupancy);
        Assert.Equal(new DateTime(2023, 6, 15), comp.OriginationDate);
        Assert.Equal(new DateTime(2033, 6, 1), comp.MaturityDate);
        Assert.Equal("CMBS Trust 2023-C1", comp.DealName);
    }

    [Fact]
    public void ParseEx102Xml_CalculatesLTV()
    {
        var client = CreateClient();
        var comps = client.ParseEx102Xml(SampleEx102Xml);
        var comp = comps[0];

        // LTV = originalLoanAmount / mostRecentValuationAmount * 100
        Assert.Equal(75.0m, comp.LTV);
    }

    [Fact]
    public void ParseEx102Xml_CalculatesCapRate()
    {
        var client = CreateClient();
        var comps = client.ParseEx102Xml(SampleEx102Xml);
        var comp = comps[0];

        // CapRate = NOI / Valuation * 100 = 1200000 / 20000000 * 100 = 6.0
        Assert.Equal(6.0m, comp.CapRate);
    }

    [Fact]
    public void ParseEx102Xml_MapsPropertyTypeCodes()
    {
        var client = CreateClient();
        var comps = client.ParseEx102Xml(SampleEx102Xml);

        Assert.Equal(PropertyType.Multifamily, comps[0].PropertyType);
        Assert.Equal(PropertyType.SeniorApartment, comps[1].PropertyType); // HC → senior housing
    }

    [Fact]
    public void ParseEx102Xml_HandlesXmlNamespace()
    {
        var client = CreateClient();
        var comps = client.ParseEx102Xml(SampleEx102WithNamespace, "CMBS 2023-NS");

        Assert.Single(comps);
        Assert.Equal("AZ", comps[0].State);
        Assert.Equal("Phoenix", comps[0].City);
        Assert.Equal(25_000_000m, comps[0].LoanAmount);
    }

    [Fact]
    public void ParseEx102Xml_EmptyXml_ReturnsEmpty()
    {
        var client = CreateClient();
        var comps = client.ParseEx102Xml("<assetData><assets></assets></assetData>");

        Assert.Empty(comps);
    }

    [Fact]
    public void ParseEx102Xml_InvalidXml_ReturnsEmpty()
    {
        var client = CreateClient();
        var comps = client.ParseEx102Xml("not xml at all");

        Assert.Empty(comps);
    }

    [Fact]
    public void ParseEx102Xml_MissingFields_DefaultsToNull()
    {
        var xml = """
            <assetData>
              <assets>
                <asset>
                  <assetNumber>1</assetNumber>
                  <propertyTypeCode>MF</propertyTypeCode>
                  <propertyState>CA</propertyState>
                </asset>
              </assets>
            </assetData>
            """;

        var client = CreateClient();
        var comps = client.ParseEx102Xml(xml);

        Assert.Single(comps);
        Assert.Equal("CA", comps[0].State);
        Assert.Equal(PropertyType.Multifamily, comps[0].PropertyType);
        Assert.Null(comps[0].LoanAmount);
        Assert.Null(comps[0].DSCR);
        Assert.Null(comps[0].Occupancy);
        Assert.Null(comps[0].Units);
    }

    [Theory]
    [InlineData("MF", PropertyType.Multifamily)]
    [InlineData("HC", PropertyType.SeniorApartment)]
    [InlineData("MH", PropertyType.Multifamily)] // manufactured housing → multifamily
    public void ParseEx102Xml_PropertyTypeMapping(string cmbsCode, PropertyType expected)
    {
        var xml = $"""
            <assetData>
              <assets>
                <asset>
                  <assetNumber>1</assetNumber>
                  <propertyTypeCode>{cmbsCode}</propertyTypeCode>
                  <propertyState>NY</propertyState>
                </asset>
              </assets>
            </assetData>
            """;

        var client = CreateClient();
        var comps = client.ParseEx102Xml(xml);

        Assert.Single(comps);
        Assert.Equal(expected, comps[0].PropertyType);
    }

    [Fact]
    public void ParseEx102Xml_UnmappedPropertyType_SkipsAsset()
    {
        var xml = """
            <assetData>
              <assets>
                <asset>
                  <assetNumber>1</assetNumber>
                  <propertyTypeCode>OF</propertyTypeCode>
                  <propertyState>NY</propertyState>
                </asset>
                <asset>
                  <assetNumber>2</assetNumber>
                  <propertyTypeCode>MF</propertyTypeCode>
                  <propertyState>CA</propertyState>
                </asset>
              </assets>
            </assetData>
            """;

        var client = CreateClient();
        var comps = client.ParseEx102Xml(xml);

        // Only MF asset should be included (OF=Office not in our property type scope)
        Assert.Single(comps);
        Assert.Equal("CA", comps[0].State);
    }

    private static EdgarCmbsClient CreateClient()
    {
        var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://efts.sec.gov/") };
        return new EdgarCmbsClient(httpClient, NullLogger<EdgarCmbsClient>.Instance);
    }
}
