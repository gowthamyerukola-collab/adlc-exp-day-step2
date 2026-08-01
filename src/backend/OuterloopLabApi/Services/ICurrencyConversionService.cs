using OuterloopLabApi.Api;

namespace OuterloopLabApi.Services;

public interface ICurrencyConversionService
{
    Task<ConvertResponse> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken ct);
    Task<AuditResponse?> GetAuditAsync(string auditId, CancellationToken ct);
}
