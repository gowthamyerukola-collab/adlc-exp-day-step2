using OuterloopLabApi.Contracts;
using OuterloopLabApi.Domain;

namespace OuterloopLabApi.Services;

public interface ICurrencyConversionService
{
    Task<ConversionAuditRecord> CreateConversionAsync(ConversionRequest request, CancellationToken cancellationToken);
}

public sealed class CurrencyConversionService(
    ICurrencyRateProvider currencyRateProvider,
    IConversionAuditRepository repository) : ICurrencyConversionService
{
    public async Task<ConversionAuditRecord> CreateConversionAsync(ConversionRequest request, CancellationToken cancellationToken)
    {
        var quote = await currencyRateProvider.GetQuoteAsync(request.FromCurrency, request.ToCurrency, cancellationToken);
        var executionTimestampUtc = DateTimeOffset.UtcNow;

        var record = new ConversionAuditRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            RequestedAmount = request.Amount,
            SourceCurrency = request.FromCurrency,
            TargetCurrency = request.ToCurrency,
            AppliedRate = quote.Rate,
            ConvertedAmount = decimal.Round(request.Amount * quote.Rate, 4, MidpointRounding.AwayFromZero),
            ProviderMarker = quote.ProviderMarker,
            ExecutionTimestampUtc = executionTimestampUtc
        };

        return await repository.CreateAsync(record, cancellationToken);
    }
}
