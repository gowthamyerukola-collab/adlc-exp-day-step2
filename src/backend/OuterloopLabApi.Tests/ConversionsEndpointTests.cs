using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OuterloopLabApi.Contracts;
using OuterloopLabApi.Domain;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Tests;

public sealed class ConversionsEndpointTests : IClassFixture<ConversionsEndpointTests.CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConversionsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostConversions_Returns400ProblemDetails_WhenAmountIsInvalid()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/conversions", new ConversionRequest(0m, "USD", "EUR"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Amount must be greater than zero.", body);
    }

    [Fact]
    public async Task PostConversions_Returns503ProblemDetails_WhenProviderFails()
    {
        _factory.Provider = new ThrowingCurrencyRateProvider();
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/conversions", new ConversionRequest(100m, "USD", "EUR"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Currency rate provider unavailable", body);
        Assert.DoesNotContain("socket", body, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public ICurrencyRateProvider Provider { get; set; } = new StubCurrencyRateProvider();
        public IConversionAuditRepository Repository { get; set; } = new InMemoryRepository();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICurrencyRateProvider>();
                services.RemoveAll<IConversionAuditRepository>();
                services.AddSingleton(Provider);
                services.AddSingleton(Repository);
            });
        }
    }

    private sealed class StubCurrencyRateProvider : ICurrencyRateProvider
    {
        public Task<CurrencyConversionQuote> GetQuoteAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
            => Task.FromResult(new CurrencyConversionQuote(0.92m, "2026-08-01"));
    }

    private sealed class ThrowingCurrencyRateProvider : ICurrencyRateProvider
    {
        public Task<CurrencyConversionQuote> GetQuoteAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
            => throw new CurrencyRateProviderUnavailableException("socket timeout details should not leak");
    }

    private sealed class InMemoryRepository : IConversionAuditRepository
    {
        public Task<ConversionAuditRecord> CreateAsync(ConversionAuditRecord record, CancellationToken cancellationToken)
            => Task.FromResult(record);

        public Task<ConversionAuditRecord?> GetByIdAsync(string id, CancellationToken cancellationToken)
            => Task.FromResult<ConversionAuditRecord?>(null);

        public Task<IReadOnlyList<ConversionAuditRecord>> ListRecentAsync(int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConversionAuditRecord>>([]);
    }
}
