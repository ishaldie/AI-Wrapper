using System.Net;

namespace ZSR.Underwriting.Tests.Helpers;

/// <summary>
/// Simple mock HttpMessageHandler for unit testing HTTP clients.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _response;
    private readonly HttpStatusCode _statusCode;
    private readonly Dictionary<string, string> _responseHeaders = new();

    public HttpRequestMessage? LastRequest { get; private set; }

    public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
    {
        _response = response;
        _statusCode = statusCode;
    }

    public MockHttpMessageHandler WithResponseHeader(string name, string value)
    {
        _responseHeaders[name] = value;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
        };

        foreach (var header in _responseHeaders)
        {
            responseMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return Task.FromResult(responseMessage);
    }
}
