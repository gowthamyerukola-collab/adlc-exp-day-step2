using System.Text.Json.Serialization;

namespace OuterloopLabApi.Domain;

public sealed class ConversionAuditRecord
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("requestedAmount")]
    public decimal RequestedAmount { get; init; }

    [JsonPropertyName("sourceCurrency")]
    public string SourceCurrency { get; init; } = string.Empty;

    [JsonPropertyName("targetCurrency")]
    public string TargetCurrency { get; init; } = string.Empty;

    [JsonPropertyName("appliedRate")]
    public decimal AppliedRate { get; init; }

    [JsonPropertyName("convertedAmount")]
    public decimal ConvertedAmount { get; init; }

    [JsonPropertyName("providerMarker")]
    public string ProviderMarker { get; init; } = string.Empty;

    [JsonPropertyName("executionTimestampUtc")]
    public DateTimeOffset ExecutionTimestampUtc { get; init; }
}
