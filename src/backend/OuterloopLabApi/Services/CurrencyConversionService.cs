using System.Globalization;
using OuterloopLabApi.Api;
using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public sealed class CurrencyConversionService : ICurrencyConversionService
{
    private readonly IRateProvider _rateProvider;
    private readonly IAuditRepository _auditRepository;

    public CurrencyConversionService(IRateProvider rateProvider, IAuditRepository auditRepository)
    {
        _rateProvider = rateProvider;
        _auditRepository = auditRepository;
    }

    public async Task<ConvertResponse> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken ct)
    {
        var from = NormalizeCurrency(fromCurrency, "fromCurrency");
        var to = NormalizeCurrency(toCurrency, "toCurrency");

        if (amount <= 0)
            throw new CurrencyValidationException("Amount must be greater than 0.");

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            throw new CurrencyValidationException("FromCurrency and ToCurrency must be different.");

        var providerResult = await _rateProvider.GetRateAsync(from, to, ct);

        // Use consistent rounding so the stored converted amount can be reconstructed.
        var convertedAmount = Math.Round(amount * providerResult.Rate, 2, MidpointRounding.AwayFromZero);

        var executedAtUtc = DateTime.UtcNow;
        var auditId = Guid.NewGuid().ToString("D");

        var record = new AuditRecord
        {
            Id = auditId,
            FromCurrency = from,
            ToCurrency = to,
            OriginalAmount = amount,
            ProviderRate = providerResult.Rate,
            ConvertedAmount = convertedAmount,
            ProviderDate = providerResult.ProviderDate,
            ExecutedAtUtc = executedAtUtc,
            ProviderBaseUrl = (_rateProvider as FrankfurterRateProvider)?.BaseUrl ?? string.Empty
        };

        await _auditRepository.CreateAsync(record, ct);

        return new ConvertResponse(
            AuditId: record.Id,
            FromCurrency: record.FromCurrency,
            ToCurrency: record.ToCurrency,
            OriginalAmount: record.OriginalAmount,
            ProviderRate: record.ProviderRate,
            ConvertedAmount: record.ConvertedAmount,
            ProviderDate: record.ProviderDate,
            ExecutedAtUtc: record.ExecutedAtUtc);
    }

    public async Task<AuditResponse?> GetAuditAsync(string auditId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(auditId))
            throw new CurrencyValidationException("auditId is required.");

        var record = await _auditRepository.GetAsync(auditId, ct);
        if (record is null)
            return null;

        return new AuditResponse(
            AuditId: record.Id,
            FromCurrency: record.FromCurrency,
            ToCurrency: record.ToCurrency,
            OriginalAmount: record.OriginalAmount,
            ProviderRate: record.ProviderRate,
            ConvertedAmount: record.ConvertedAmount,
            ProviderDate: record.ProviderDate,
            ExecutedAtUtc: record.ExecutedAtUtc,
            ProviderBaseUrl: record.ProviderBaseUrl);
    }

    private static string NormalizeCurrency(string currencyCode, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new CurrencyValidationException($"{parameterName} is required.");

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || !normalized.All(char.IsLetter))
            throw new CurrencyValidationException($"{parameterName} must be a 3-letter alphabetic ISO-style code.");

        return normalized;
    }
}
