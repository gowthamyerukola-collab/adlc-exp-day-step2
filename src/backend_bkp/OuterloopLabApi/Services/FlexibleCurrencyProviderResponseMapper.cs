using System.Globalization;
using System.Text.Json;
using OuterloopLabApi.Domain;

namespace OuterloopLabApi.Services;

public interface ICurrencyProviderResponseMapper
{
    CurrencyConversionQuote Map(JsonElement root, string fromCurrency, string toCurrency);
}

public sealed class FlexibleCurrencyProviderResponseMapper : ICurrencyProviderResponseMapper
{
    public CurrencyConversionQuote Map(JsonElement root, string fromCurrency, string toCurrency)
    {
        if (!TryReadRate(root, toCurrency, out var rate))
        {
            throw new CurrencyRateProviderUnavailableException("The upstream provider payload did not contain a usable rate.");
        }

        var providerMarker = ReadProviderMarker(root) ?? "unknown";
        return new CurrencyConversionQuote(rate, providerMarker);
    }

    private static bool TryReadRate(JsonElement root, string currencyCode, out decimal rate)
    {
        foreach (var containerName in new[] { "rates", "conversion_rates" })
        {
            if (TryGetPropertyCaseInsensitive(root, containerName, out var ratesElement) &&
                ratesElement.ValueKind == JsonValueKind.Object &&
                TryGetPropertyCaseInsensitive(ratesElement, currencyCode, out var rateElement) &&
                TryConvertDecimal(rateElement, out rate))
            {
                return true;
            }
        }

        rate = default;
        return false;
    }

    private static string? ReadProviderMarker(JsonElement root)
    {
        foreach (var propertyName in new[] { "date", "timestamp", "time_last_update_utc", "time_last_update_unix", "sequence", "last_updated_at" })
        {
            if (TryFindValue(root, propertyName, out var markerElement))
            {
                return markerElement.ValueKind switch
                {
                    JsonValueKind.String => markerElement.GetString(),
                    JsonValueKind.Number => markerElement.ToString(),
                    _ => null
                };
            }
        }

        return null;
    }

    private static bool TryFindValue(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }

                if (TryFindValue(property.Value, propertyName, out value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryFindValue(item, propertyName, out value))
                {
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryConvertDecimal(JsonElement element, out decimal value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}
