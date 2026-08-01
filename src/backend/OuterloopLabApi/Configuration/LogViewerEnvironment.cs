namespace OuterloopLabApi.Configuration;

public sealed class LogViewerEnvironment
{
    public string? LogAnalyticsWorkspaceId { get; init; }

    public string? LogAnalyticsTable { get; init; }

    public string? ManagedIdentityClientId { get; init; }

    public bool HasCompleteLogAnalyticsConfiguration =>
        !string.IsNullOrWhiteSpace(LogAnalyticsWorkspaceId) &&
        !string.IsNullOrWhiteSpace(LogAnalyticsTable);

    public static LogViewerEnvironment FromEnvironment() => new()
    {
        LogAnalyticsWorkspaceId = Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_ID"),
        LogAnalyticsTable = Environment.GetEnvironmentVariable("LOG_ANALYTICS_TABLE") ?? "ContainerAppConsoleLogs_CL",
        ManagedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID")
    };
}
