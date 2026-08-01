using System.Net;
using System.Text;
using OuterloopLabApi.Domain;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Tests;

public sealed class ExternalCurrencyRateProviderTests
{
    private sealed class RecordingHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task GetQuoteAsync_UsesLatestEndpoint_WithFromAndToParameters()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """
            {
              "amount": 1.0,
              "base": "USD",
              "date": "2026-08-01",
              "rates": {
                "EUR": 0.92
              }
            }
            """);

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app") };
        var provider = new ExternalCurrencyRateProvider(httpClient, new FlexibleCurrencyProviderResponseMapper());

        var quote = await provider.GetQuoteAsync("USD", "EUR", CancellationToken.None);

        Assert.NotNull(handler.RequestUri);
        Assert.Equal("/latest", handler.RequestUri!.AbsolutePath);
        Assert.Contains("from=USD", handler.RequestUri.Query);
        Assert.Contains("to=EUR", handler.RequestUri.Query);
        Assert.Equal(0.92m, quote.Rate);
        Assert.Equal("2026-08-01", quote.ProviderMarker);
    }

    [Fact]
    public async Task GetQuoteAsync_ThrowsDomainException_WhenUpstreamIsUnavailable()
    {
        var handler = new RecordingHandler(HttpStatusCode.ServiceUnavailable, """{"message":"unavailable"}""");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app") };
        var provider = new ExternalCurrencyRateProvider(httpClient, new FlexibleCurrencyProviderResponseMapper());

        await Assert.ThrowsAsync<CurrencyRateProviderUnavailableException>(
            () => provider.GetQuoteAsync("USD", "EUR", CancellationToken.None));
    }
}
