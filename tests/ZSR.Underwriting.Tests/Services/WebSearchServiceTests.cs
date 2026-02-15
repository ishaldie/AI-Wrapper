using System.Net;
using System.Text.Json;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Domain.Interfaces;
using ZSR.Underwriting.Domain.Models;
using ZSR.Underwriting.Infrastructure.Services;

namespace ZSR.Underwriting.Tests.Services;

public class WebSearchServiceTests
{
    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
    {
        var handler = new MockHttpMessageHandler(statusCode, content);
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com") };
    }

    [Fact]
    public async Task SearchAsync_Returns_Results_On_Success()
    {
        var json = JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new { title = "Top Employers in Dallas", snippet = "Major employers include...", link = "https://example.com/employers" },
                new { title = "Dallas Economy", snippet = "The Dallas economy...", link = "https://example.com/economy" }
            }
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = new WebSearchService(httpClient);

        var results = await service.SearchAsync("major employers Dallas TX", MarketSearchCategory.MajorEmployers);

        Assert.Equal(2, results.Count);
        Assert.Equal("Top Employers in Dallas", results[0].Title);
        Assert.Equal("Major employers include...", results[0].Snippet);
        Assert.Equal("https://example.com/employers", results[0].SourceUrl);
        Assert.Equal(MarketSearchCategory.MajorEmployers, results[0].Category);
        Assert.True(results[0].RetrievedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_HttpError()
    {
        var httpClient = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
        var service = new WebSearchService(httpClient);

        var results = await service.SearchAsync("test query", MarketSearchCategory.EconomicDrivers);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_InvalidJson()
    {
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, "not json");
        var service = new WebSearchService(httpClient);

        var results = await service.SearchAsync("test query", MarketSearchCategory.Infrastructure);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_Respects_MaxResults()
    {
        var json = JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new { title = "Result 1", snippet = "Snippet 1", link = "https://example.com/1" },
                new { title = "Result 2", snippet = "Snippet 2", link = "https://example.com/2" },
                new { title = "Result 3", snippet = "Snippet 3", link = "https://example.com/3" }
            }
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = new WebSearchService(httpClient);

        var results = await service.SearchAsync("test", MarketSearchCategory.FannieMaeRates, maxResults: 2);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchAsync_Sets_Category_On_All_Results()
    {
        var json = JsonSerializer.Serialize(new
        {
            items = new[]
            {
                new { title = "R1", snippet = "S1", link = "https://example.com/1" },
                new { title = "R2", snippet = "S2", link = "https://example.com/2" }
            }
        });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = new WebSearchService(httpClient);

        var results = await service.SearchAsync("test", MarketSearchCategory.ConstructionPipeline);

        Assert.All(results, r => Assert.Equal(MarketSearchCategory.ConstructionPipeline, r.Category));
    }

    [Fact]
    public async Task SearchAsync_Handles_Empty_Items_Array()
    {
        var json = JsonSerializer.Serialize(new { items = Array.Empty<object>() });

        var httpClient = CreateMockHttpClient(HttpStatusCode.OK, json);
        var service = new WebSearchService(httpClient);

        var results = await service.SearchAsync("obscure query", MarketSearchCategory.ComparableTransactions);

        Assert.Empty(results);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            };
            return Task.FromResult(response);
        }
    }
}
