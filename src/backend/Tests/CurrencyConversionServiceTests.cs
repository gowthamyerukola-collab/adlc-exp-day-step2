using OuterloopLabApi.Models;
using OuterloopLabApi.Services;
using Tests.TestDoubles;
using Xunit;

namespace Tests;

public sealed class CurrencyConversionServiceTests
{
    [Fact]
    public async Task Converts_And_Persists_Audit_Record()
    {
        var rateProvider = new FakeRateProvider(new ProviderRateResult(1.2345m, "2026-08-01"));
        var repo = new InMemoryAuditRepository();
        var service = new CurrencyConversionService(rateProvider, repo);

        var res = await service.ConvertAsync(100m, "usd", "eur", CancellationToken.None);

        Assert.NotNull(res.AuditId);
        Assert.Equal("USD", res.FromCurrency);
        Assert.Equal("EUR", res.ToCurrency);
        Assert.Equal(100m, res.OriginalAmount);
        Assert.Equal(1.2345m, res.ProviderRate);
        Assert.Equal(Math.Round(100m * 1.2345m, 2, MidpointRounding.AwayFromZero), res.ConvertedAmount);
        Assert.Equal("2026-08-01", res.ProviderDate);
        Assert.True(res.ExecutedAtUtc.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetAuditAsync_Returns_Stored_Record()
    {
        var rateProvider = new FakeRateProvider(new ProviderRateResult(2m, "2026-08-01"));
        var repo = new InMemoryAuditRepository();
        var service = new CurrencyConversionService(rateProvider, repo);

        var created = await service.ConvertAsync(10m, "EUR", "USD", CancellationToken.None);

        var fetched = await service.GetAuditAsync(created.AuditId, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal(created.AuditId, fetched!.AuditId);
        Assert.Equal(created.ConvertedAmount, fetched.ConvertedAmount);
        Assert.Equal(created.ProviderRate, fetched.ProviderRate);
    }
}
