using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Infrastructure.Services;
using ZSR.Underwriting.Tests.Helpers;

namespace ZSR.Underwriting.Tests.Services;

public class CensusDemographicsTests
{
    // ACS variables: B19013_001E (HHI), B25064_001E (median rent), B25010_001E (HH size),
    // B25070_010E (rent burden >=30%), B25003_003E (renter units), zip
    private const string ValidDemographicsResponse = """
    [
      ["B19013_001E","B25064_001E","B25010_001E","B25070_010E","B25003_003E","zip code tabulation area"],
      ["65000","1450","2.35","4200","12000","75201"]
    ]
    """;

    [Fact]
    public async Task GetTenantDemographicsAsync_ParsesValidResponse()
    {
        var handler = new MockHttpMessageHandler(ValidDemographicsResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.census.gov/") };
        var service = new CensusApiClient(client, NullLogger<CensusApiClient>.Instance);

        var result = await service.GetTenantDemographicsAsync("75201");

        Assert.NotNull(result);
        Assert.Equal(65_000m, result!.MedianHouseholdIncome);
        Assert.Equal(1_450m, result.MedianGrossRent);
        Assert.Equal(2.35m, result.AverageHouseholdSize);
        Assert.Equal(12_000, result.RenterOccupiedUnits);
        Assert.Equal("75201", result.ZipCode);
    }

    [Fact]
    public async Task GetTenantDemographicsAsync_CalculatesRentBurden()
    {
        var handler = new MockHttpMessageHandler(ValidDemographicsResponse, HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.census.gov/") };
        var service = new CensusApiClient(client, NullLogger<CensusApiClient>.Instance);

        var result = await service.GetTenantDemographicsAsync("75201");

        Assert.NotNull(result);
        // 4200 households with >=30% rent burden out of 12000 renters = 35%
        Assert.True(result!.RentBurdenPercent > 0, "Rent burden should be calculated");
        Assert.Equal(35m, result.RentBurdenPercent);
    }

    [Fact]
    public async Task GetTenantDemographicsAsync_HttpFailure_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler("error", HttpStatusCode.InternalServerError);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.census.gov/") };
        var service = new CensusApiClient(client, NullLogger<CensusApiClient>.Instance);

        var result = await service.GetTenantDemographicsAsync("00000");

        Assert.Null(result);
    }
}

public class DemographicsEnrichmentTests
{
    [Fact]
    public void EnrichTenantMarket_WithDemographics_PopulatesBenchmarks()
    {
        var marketContext = new MarketContextDto
        {
            MajorEmployers = [new() { Name = "Acme Corp", Description = "Tech employer" }]
        };
        var demographics = new TenantDemographicsDto
        {
            MedianHouseholdIncome = 65_000m,
            MedianGrossRent = 1_450m,
            AverageHouseholdSize = 2.35m,
            RentBurdenPercent = 35m,
            RenterOccupiedUnits = 12_000,
            ZipCode = "75201"
        };

        var section = MarketDataEnricher.EnrichTenantMarket(
            marketContext, 1_200m, 95m, demographics);

        Assert.True(section.Benchmarks.Count > 0, "Should have benchmark rows");
        Assert.Contains(section.Benchmarks, b => b.Metric == "Median Household Income");
        Assert.Contains(section.Benchmarks, b => b.Metric == "Median Gross Rent");
        Assert.Contains(section.Benchmarks, b => b.Metric == "Rent Burden (>=30% HHI)");
    }

    [Fact]
    public void EnrichTenantMarket_WithDemographics_IncludesSourceAttribution()
    {
        var marketContext = new MarketContextDto
        {
            MajorEmployers = [new() { Name = "Acme Corp", Description = "Tech employer" }]
        };
        var demographics = new TenantDemographicsDto
        {
            MedianHouseholdIncome = 65_000m,
            MedianGrossRent = 1_450m
        };

        var section = MarketDataEnricher.EnrichTenantMarket(
            marketContext, 1_200m, 95m, demographics);

        Assert.Contains("Census", section.Narrative);
    }

    [Fact]
    public void EnrichTenantMarket_NullDemographics_NoBenchmarks()
    {
        var marketContext = new MarketContextDto
        {
            MajorEmployers = [new() { Name = "Acme Corp", Description = "Tech employer" }]
        };

        var section = MarketDataEnricher.EnrichTenantMarket(
            marketContext, 1_200m, 95m, null);

        Assert.Empty(section.Benchmarks);
    }

    [Fact]
    public void EnrichTenantMarket_WithDemographics_SetsMarketRentPerUnit()
    {
        var marketContext = new MarketContextDto
        {
            MajorEmployers = [new() { Name = "Acme Corp", Description = "Tech employer" }]
        };
        var demographics = new TenantDemographicsDto
        {
            MedianGrossRent = 1_450m
        };

        var section = MarketDataEnricher.EnrichTenantMarket(
            marketContext, 1_200m, 95m, demographics);

        Assert.Equal(1_450m, section.MarketRentPerUnit);
    }
}
