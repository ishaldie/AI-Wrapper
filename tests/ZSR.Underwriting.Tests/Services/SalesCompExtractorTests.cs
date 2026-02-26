using Microsoft.Extensions.Logging.Abstractions;
using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.DTOs.Report;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;

namespace ZSR.Underwriting.Tests.Services;

public class SalesCompExtractorTests
{
    private static MarketContextDto CreateMarketContextWithComps()
    {
        return new MarketContextDto
        {
            ComparableTransactions =
            [
                new() { Name = "Oak Park Apartments", Description = "120 units sold for $11.4M ($95K/unit) at 5.5% cap in Jan 2025, 2.3 miles away", SourceUrl = "https://example.com/1" },
                new() { Name = "Pine Ridge Multifamily", Description = "80 units sold for $8.8M ($110K/unit) at 5.2% cap in Mar 2025, 1.1 miles away", SourceUrl = "https://example.com/2" }
            ],
            RetrievedAt = DateTime.UtcNow
        };
    }

    private const string ValidCompsJson = """
    {
      "comps": [
        {
          "address": "100 Oak Park Dr",
          "salePrice": 11400000,
          "units": 120,
          "pricePerUnit": 95000,
          "capRate": 5.5,
          "saleDate": "2025-01-15",
          "distanceMiles": 2.3
        },
        {
          "address": "200 Pine Ridge Ln",
          "salePrice": 8800000,
          "units": 80,
          "pricePerUnit": 110000,
          "capRate": 5.2,
          "saleDate": "2025-03-10",
          "distanceMiles": 1.1
        }
      ],
      "adjustments": [
        {
          "factor": "Unit Count",
          "adjustment": "+5%",
          "rationale": "Subject has fewer units, reducing operational efficiency"
        },
        {
          "factor": "Condition",
          "adjustment": "-3%",
          "rationale": "Subject property is in better condition"
        }
      ]
    }
    """;

    [Fact]
    public async Task ExtractCompsAsync_ParsesCompsFromClaudeResponse()
    {
        var claude = new StubClaudeClient(ValidCompsJson);
        var extractor = new SalesCompExtractor(claude, NullLogger<SalesCompExtractor>.Instance);
        var context = CreateMarketContextWithComps();

        var result = await extractor.ExtractCompsAsync(context, "123 Main St, Dallas, TX", 100_000m, 100);

        Assert.Equal(2, result.Comps.Count);
        Assert.Equal("100 Oak Park Dr", result.Comps[0].Address);
        Assert.Equal(11_400_000m, result.Comps[0].SalePrice);
        Assert.Equal(120, result.Comps[0].Units);
        Assert.Equal(95_000m, result.Comps[0].PricePerUnit);
    }

    [Fact]
    public async Task ExtractCompsAsync_ParsesAdjustmentsFromClaudeResponse()
    {
        var claude = new StubClaudeClient(ValidCompsJson);
        var extractor = new SalesCompExtractor(claude, NullLogger<SalesCompExtractor>.Instance);
        var context = CreateMarketContextWithComps();

        var result = await extractor.ExtractCompsAsync(context, "123 Main St, Dallas, TX", 100_000m, 100);

        Assert.Equal(2, result.Adjustments.Count);
        Assert.Equal("Unit Count", result.Adjustments[0].Factor);
        Assert.Equal("+5%", result.Adjustments[0].Adjustment);
    }

    [Fact]
    public async Task ExtractCompsAsync_EmptyComparableTransactions_ReturnsEmptyResult()
    {
        var claude = new StubClaudeClient("{}");
        var extractor = new SalesCompExtractor(claude, NullLogger<SalesCompExtractor>.Instance);
        var context = new MarketContextDto(); // No comparable transactions

        var result = await extractor.ExtractCompsAsync(context, "123 Main St", 100_000m, 100);

        Assert.Empty(result.Comps);
        Assert.Empty(result.Adjustments);
    }

    [Fact]
    public async Task ExtractCompsAsync_MalformedClaudeResponse_ReturnsEmptyResult()
    {
        var claude = new StubClaudeClient("This is not JSON at all");
        var extractor = new SalesCompExtractor(claude, NullLogger<SalesCompExtractor>.Instance);
        var context = CreateMarketContextWithComps();

        var result = await extractor.ExtractCompsAsync(context, "123 Main St", 100_000m, 100);

        Assert.Empty(result.Comps);
        Assert.Empty(result.Adjustments);
    }

    [Fact]
    public async Task ExtractCompsAsync_CapRateIsParsedCorrectly()
    {
        var claude = new StubClaudeClient(ValidCompsJson);
        var extractor = new SalesCompExtractor(claude, NullLogger<SalesCompExtractor>.Instance);
        var context = CreateMarketContextWithComps();

        var result = await extractor.ExtractCompsAsync(context, "123 Main St", 100_000m, 100);

        Assert.Equal(5.5m, result.Comps[0].CapRate);
        Assert.Equal(5.2m, result.Comps[1].CapRate);
    }
}

internal class StubClaudeClient : IClaudeClient
{
    private readonly string _response;

    public StubClaudeClient(string response)
    {
        _response = response;
    }

    public Task<ClaudeResponse> SendMessageAsync(ClaudeRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(new ClaudeResponse
        {
            Content = _response,
            InputTokens = 100,
            OutputTokens = 200,
            Model = "test-model"
        });
    }
}
