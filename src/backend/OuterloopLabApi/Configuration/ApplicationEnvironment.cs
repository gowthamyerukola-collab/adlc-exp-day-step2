namespace OuterloopLabApi.Configuration;

public sealed class ApplicationEnvironment
{
    public string CurrencyApiBaseUrl { get; init; } = "https://frankfurter.dev";
    public string? CosmosDbUri { get; init; }
    public string? CosmosDbDatabase { get; init; }
    public string? CosmosDbContainer { get; init; }
    public string? CosmosDbAccountName { get; init; }
    public string? CosmosDbResourceGroup { get; init; }
    public string? CosmosDbRegion { get; init; }
    public string? ManagedIdentityClientId { get; init; }

    public bool HasCompleteCosmosConfiguration =>
        !string.IsNullOrWhiteSpace(CosmosDbUri) &&
        !string.IsNullOrWhiteSpace(CosmosDbDatabase) &&
        !string.IsNullOrWhiteSpace(CosmosDbContainer) &&
        !string.IsNullOrWhiteSpace(CosmosDbAccountName) &&
        !string.IsNullOrWhiteSpace(CosmosDbResourceGroup) &&
        !string.IsNullOrWhiteSpace(CosmosDbRegion);

    public static ApplicationEnvironment FromEnvironment() => new()
    {
        CurrencyApiBaseUrl = Environment.GetEnvironmentVariable("CURRENCY_API_BASE_URL")?.Trim() switch
        {
            { Length: > 0 } value => value,
            _ => "https://frankfurter.dev"
        },
        CosmosDbUri = Environment.GetEnvironmentVariable("COSMOS_DB_URI"),
        CosmosDbDatabase = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE"),
        CosmosDbContainer = Environment.GetEnvironmentVariable("COSMOS_DB_CONTAINER"),
        CosmosDbAccountName = Environment.GetEnvironmentVariable("COSMOS_DB_ACCOUNT_NAME"),
        CosmosDbResourceGroup = Environment.GetEnvironmentVariable("COSMOS_DB_RESOURCE_GROUP"),
        CosmosDbRegion = Environment.GetEnvironmentVariable("COSMOS_DB_REGION"),
        ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID")
    };
}
