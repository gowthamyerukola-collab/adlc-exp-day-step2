using OuterloopLabApi.Configuration;
using OuterloopLabApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

var environment = LogViewerEnvironment.FromEnvironment();
builder.Services.AddSingleton(environment);
builder.Services.AddSingleton<LogAnalyticsLogService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { status = "ok" }));

app.MapGet(
    "/api/logs",
    async Task<IResult> (
        string enrollmentId,
        int? limit,
        string? search,
        int? hours,
        LogAnalyticsLogService service,
        CancellationToken cancellationToken) =>
    {
        var enrollmentNumber = EnrollmentResolver.TryResolveNumber(enrollmentId);
        if (enrollmentNumber is null)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["enrollmentId"] = ["Provide an enrollment id with a trailing number (for example 1100 or adlc-1100)."]
                },
                title: "One or more validation errors occurred.");
        }

        var effectiveLimit = Math.Clamp(limit ?? 100, 1, 500);
        var effectiveHours = Math.Clamp(hours ?? 1, 1, 720);
        var containerAppName = EnrollmentResolver.BuildContainerAppName(enrollmentNumber.Value);

        try
        {
            var result = await service.QueryAsync(enrollmentNumber.Value, effectiveHours, search, effectiveLimit, cancellationToken);
            return Results.Ok(new
            {
                enrollmentNumber = enrollmentNumber.Value,
                containerAppName,
                count = result.Count,
                logs = result.Logs
            });
        }
        catch (Exception exception)
        {
            return Results.Problem(
                title: "Log query failed",
                detail: $"Could not query logs for {containerAppName}: {exception.Message}",
                statusCode: StatusCodes.Status502BadGateway);
        }
    });

app.Run();

public partial class Program
{
}
