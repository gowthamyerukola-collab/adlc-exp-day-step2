using System.Text.Json;
using OuterloopLabApi.Domain;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Tests;

public sealed class FlexibleCurrencyProviderResponseMapperTests
{
    private readonly FlexibleCurrencyProviderResponseMapper _mapper = new();

    [Fact]
    public void Map_UsesRatesPayload_WhenRatesObjectExists()
    {
        using var document = JsonDocument.Parse("""
            {
              "base": "USD",
              "date": "2026-08-01",
              "rates": {
                "EUR": 0.92
              }
            }
            """);

        var quote = _mapper.Map(document.RootElement, "USD", "EUR");

        Assert.Equal(0.92m, quote.Rate);
        Assert.Equal("2026-08-01", quote.ProviderMarker);
    }

    [Fact]
    public void Map_UsesConversionRatesPayload_WhenAlternateSchemaExists()
    {
        using var document = JsonDocument.Parse("""
            {
              "meta": {
                "sequence": 20260801140322
              },
              "conversion_rates": {
                "EUR": "0.9200"
              }
            }
            """);

        var quote = _mapper.Map(document.RootElement, "USD", "EUR");

        Assert.Equal(0.9200m, quote.Rate);
        Assert.Equal("20260801140322", quote.ProviderMarker);
    }

    [Fact]
    public void Map_Throws_WhenNoSupportedRateExists()
    {
        using var document = JsonDocument.Parse("""
            {
              "date": "2026-08-01",
              "rates": {}
            }
            """);

        Assert.Throws<CurrencyRateProviderUnavailableException>(() => _mapper.Map(document.RootElement, "USD", "EUR"));
    }
}
