using OuterloopLabApi.Domain;

namespace OuterloopLabApi.Contracts;

public sealed record ConversionAuditResponse(
    string Id,
    decimal RequestedAmount,
    string SourceCurrency,
    string TargetCurrency,
    decimal AppliedRate,
    decimal ConvertedAmount,
    string ProviderMarker,
    DateTimeOffset ExecutionTimestampUtc)
{
    public static ConversionAuditResponse FromRecord(ConversionAuditRecord record) => new(
        record.Id,
        record.RequestedAmount,
        record.SourceCurrency,
        record.TargetCurrency,
        record.AppliedRate,
        record.ConvertedAmount,
        record.ProviderMarker,
        record.ExecutionTimestampUtc);
}
