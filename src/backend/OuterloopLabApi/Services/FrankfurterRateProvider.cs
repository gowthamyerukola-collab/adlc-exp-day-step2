using System.Text.Json;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public sealed class FrankfurterRateProvider : IRateProvider
{
    public string BaseUrl { get; }

    private readonly HttpClient _httpClient;

    public FrankfurterRateProvider(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        BaseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<ProviderRateResult> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken ct)
    {
        // Frankfurter provides rates with a `rates` object; we also support alternate JSON property names.
        // Default schema expectation:
        // { "date": "YYYY-MM-DD", "rates": { "USD": 1.23, ... } }
        // Alternate schema we also tolerate:
        // { "date": "YYYY-MM-DD", "conversion_rates": { "USD": 1.23, ... } }
        var endpoint = $"{BaseUrl}/v1/latest?base={Uri.EscapeDataString(fromCurrency)}";

        try
        {
            using var response = await _httpClient.GetAsync(endpoint, ct);
            if (!response.IsSuccessStatusCode)
                throw new CurrencyRateProviderException($"Provider returned HTTP {(int)response.StatusCode}.");

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            var providerDate = root.TryGetProperty("date", out var dateProp) ? dateProp.GetString() : null;

            decimal? rate = null;
            if (root.TryGetProperty("rates", out var ratesProp) && ratesProp.ValueKind == JsonValueKind.Object)
            {
                rate = TryReadRate(ratesProp, toCurrency);
            }

            if (rate is null && root.TryGetProperty("conversion_rates", out var conversionRatesProp) && conversionRatesProp.ValueKind == JsonValueKind.Object)
            {
                rate = TryReadRate(conversionRatesProp, toCurrency);
            }

            if (rate is null)
                throw new CurrencyRateProviderException($"Provider payload did not include a usable rate for {fromCurrency}->{toCurrency}.");

            return new ProviderRateResult(rate.Value, providerDate);
        }
        catch (CurrencyRateProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Convert raw network/serialization errors into a domain exception.
            throw new CurrencyRateProviderException("Failed to read currency rate provider response.", ex);
        }
    }

    private static decimal? TryReadRate(JsonElement ratesObject, string toCurrency)
    {
        if (!ratesObject.TryGetProperty(toCurrency, out var rateElem))
            return null;

        if (rateElem.ValueKind == JsonValueKind.Number)
            return rateElem.GetDecimal();

        if (rateElem.ValueKind == JsonValueKind.String && decimal.TryParse(rateElem.GetString(), out var parsed))
            return parsed;

        return null;
    }
}
