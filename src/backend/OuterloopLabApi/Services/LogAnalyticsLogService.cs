using Azure.Core;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using OuterloopLabApi.Configuration;

namespace OuterloopLabApi.Services;

public sealed class LogAnalyticsLogService
{
    private readonly LogViewerEnvironment _environment;
    private readonly LogsQueryClient _client;

    public LogAnalyticsLogService(LogViewerEnvironment environment, TokenCredential credential)
    {
        _environment = environment;
        _client = new LogsQueryClient(credential);
    }

    public async Task<LogQueryResult> QueryAsync(int enrollmentNumber, int hours, string? search, int limit, CancellationToken cancellationToken)
    {
        if (!_environment.HasCompleteLogAnalyticsConfiguration)
        {
            throw new InvalidOperationException("Log Analytics environment variables are not fully configured.");
        }

        var query = BuildQuery(enrollmentNumber, hours, search, limit);
        var response = await _client.QueryWorkspaceAsync(
            _environment.LogAnalyticsWorkspaceId!,
            query,
            new QueryTimeRange(TimeSpan.FromHours(hours)),
            cancellationToken: cancellationToken);

        var logs = new List<Dictionary<string, object?>>();
        foreach (var table in response.Value.AllTables)
        {
            foreach (var row in table.Rows)
            {
                logs.Add(ToDictionary(table, row));
            }
        }

        return new LogQueryResult(logs.Count, logs);
    }

    private string BuildQuery(int enrollmentNumber, int hours, string? search, int limit)
    {
        var query = $"{_environment.LogAnalyticsTable}\n" +
            $"| where TimeGenerated > ago({hours}h)\n" +
            $"| where ContainerAppName_s has '{enrollmentNumber}'";
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search.Replace("'", "''");
            query += $"\n| where Log_s contains '{escaped}'";
        }

        query += "\n| order by TimeGenerated desc\n" +
            "| project TimestampIST = format_datetime(datetime_utc_to_local(TimeGenerated, 'Asia/Kolkata'), 'yyyy-MM-dd HH:mm:ss'), Message = Log_s, ContainerAppName = ContainerAppName_s\n" +
            $"| take {limit}";
        return query;
    }

    private static Dictionary<string, object?> ToDictionary(LogsTable table, LogsTableRow row)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < table.Columns.Count; i++)
        {
            dictionary[table.Columns[i].Name] = row[i];
        }

        return dictionary;
    }
}

public sealed record LogQueryResult(int Count, IReadOnlyList<Dictionary<string, object?>> Logs);
