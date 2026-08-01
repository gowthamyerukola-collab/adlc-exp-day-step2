using System.Text.Json.Serialization;

namespace OuterloopLabApi.Models;

public sealed class AuditRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty; // Cosmos partition key: /id

    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal ProviderRate { get; set; }
    public decimal ConvertedAmount { get; set; }
    public string? ProviderDate { get; set; }
    public DateTime ExecutedAtUtc { get; set; }
    public string ProviderBaseUrl { get; set; } = string.Empty;
}
