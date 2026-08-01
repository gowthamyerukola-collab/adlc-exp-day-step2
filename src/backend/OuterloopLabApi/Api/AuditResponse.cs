namespace OuterloopLabApi.Api;

public sealed record AuditResponse(
    string AuditId,
    string FromCurrency,
    string ToCurrency,
    decimal OriginalAmount,
    decimal ProviderRate,
    decimal ConvertedAmount,
    string? ProviderDate,
    DateTime ExecutedAtUtc,
    string ProviderBaseUrl);
