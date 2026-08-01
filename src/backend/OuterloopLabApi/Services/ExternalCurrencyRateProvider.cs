using System.Text.Json;
using OuterloopLabApi.Domain;

namespace OuterloopLabApi.Services;

public interface ICurrencyRateProvider
{
    Task<CurrencyConversionQuote> GetQuoteAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken);
}

public sealed class ExternalCurrencyRateProvider(
    HttpClient httpClient,
    ICurrencyProviderResponseMapper responseMapper) : ICurrencyRateProvider
{
    public async Task<CurrencyConversionQuote> GetQuoteAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
    {
        try
        {
            var requestUri = $"/latest?from={Uri.EscapeDataString(fromCurrency)}&to={Uri.EscapeDataString(toCurrency)}";
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new CurrencyRateProviderUnavailableException("The upstream provider returned a non-success status code.");
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);
            return responseMapper.Map(document.RootElement, fromCurrency, toCurrency);
        }
        catch (CurrencyRateProviderUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            throw new CurrencyRateProviderUnavailableException("The upstream provider could not supply a usable rate.", exception);
        }
    }
}
