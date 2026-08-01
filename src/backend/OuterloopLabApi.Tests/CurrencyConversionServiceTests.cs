using OuterloopLabApi.Contracts;
using OuterloopLabApi.Domain;
using OuterloopLabApi.Services;

namespace OuterloopLabApi.Tests;

public sealed class CurrencyConversionServiceTests
{
    [Fact]
    public async Task CreateConversionAsync_PersistsNormalizedAuditRecord()
    {
        var provider = new StubCurrencyRateProvider(new CurrencyConversionQuote(0.9200m, "2026-08-01"));
        var repository = new RecordingRepository();
        var service = new CurrencyConversionService(provider, repository);

        var record = await service.CreateConversionAsync(new ConversionRequest(100.00m, "USD", "EUR"), CancellationToken.None);

        Assert.NotNull(repository.LastCreated);
        Assert.Equal(record.Id, repository.LastCreated!.Id);
        Assert.Equal(100.00m, repository.LastCreated.RequestedAmount);
        Assert.Equal("USD", repository.LastCreated.SourceCurrency);
        Assert.Equal("EUR", repository.LastCreated.TargetCurrency);
        Assert.Equal(0.9200m, repository.LastCreated.AppliedRate);
        Assert.Equal(92.0000m, repository.LastCreated.ConvertedAmount);
        Assert.Equal("2026-08-01", repository.LastCreated.ProviderMarker);
        Assert.Equal(TimeSpan.Zero, repository.LastCreated.ExecutionTimestampUtc.Offset);
    }

    private sealed class StubCurrencyRateProvider(CurrencyConversionQuote quote) : ICurrencyRateProvider
    {
        public Task<CurrencyConversionQuote> GetQuoteAsync(string fromCurrency, string toCurrency, CancellationToken cancellationToken)
            => Task.FromResult(quote);
    }

    private sealed class RecordingRepository : IConversionAuditRepository
    {
        public ConversionAuditRecord? LastCreated { get; private set; }

        public Task<ConversionAuditRecord> CreateAsync(ConversionAuditRecord record, CancellationToken cancellationToken)
        {
            LastCreated = record;
            return Task.FromResult(record);
        }

        public Task<ConversionAuditRecord?> GetByIdAsync(string id, CancellationToken cancellationToken)
            => Task.FromResult<ConversionAuditRecord?>(null);

        public Task<IReadOnlyList<ConversionAuditRecord>> ListRecentAsync(int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConversionAuditRecord>>([]);
    }
}
