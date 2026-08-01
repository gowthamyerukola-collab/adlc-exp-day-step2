using System.Net;
using System.Text;
using OuterloopLabApi.Services;
using Xunit;

namespace Tests;

public sealed class FrankfurterRateProviderTests
{
    [Fact]
    public async Task ParsesRate_From_Rates_Property()
    {
        var json = "{\"date\":\"2026-08-01\",\"rates\":{\"EUR\":1.0,\"USD\":1.2345}}";
        var httpClient = CreateHttpClient(json, HttpStatusCode.OK);

        var provider = new FrankfurterRateProvider(httpClient, "https://frankfurter.dev");

        var result = await provider.GetRateAsync("EUR", "USD", CancellationToken.None);
        Assert.Equal(1.2345m, result.Rate);
        Assert.Equal("2026-08-01", result.ProviderDate);
    }

    [Fact]
    public async Task ParsesRate_From_Conversion_Rates_Property()
    {
        var json = "{\"date\":\"2026-08-01\",\"conversion_rates\":{\"USD\":0.9876}}";
        var httpClient = CreateHttpClient(json, HttpStatusCode.OK);

        var provider = new FrankfurterRateProvider(httpClient, "https://frankfurter.dev");

        var result = await provider.GetRateAsync("EUR", "USD", CancellationToken.None);
        Assert.Equal(0.9876m, result.Rate);
        Assert.Equal("2026-08-01", result.ProviderDate);
    }

    private static HttpClient CreateHttpClient(string json, HttpStatusCode statusCode)
    {
        var handler = new StubMessageHandler(statusCode, json);
        return new HttpClient(handler);
    }

    private sealed class StubMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _json;

        public StubMessageHandler(HttpStatusCode statusCode, string json)
        {
            _statusCode = statusCode;
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
