using Azure.Core;
using Azure.Identity;

namespace OuterloopLabApi.Configuration;

public static class CredentialFactory
{
    public static TokenCredential Create(string? managedIdentityClientId)
        => string.IsNullOrWhiteSpace(managedIdentityClientId)
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });
}
