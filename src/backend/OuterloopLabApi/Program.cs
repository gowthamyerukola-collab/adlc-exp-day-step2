using System.Text.RegularExpressions;
using Azure.Core;
using OuterloopLabApi.Configuration;
using OuterloopLabApi.Contracts;
using OuterloopLabApi.Domain;
using OuterloopLabApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

var applicationEnvironment = ApplicationEnvironment.FromEnvironment();
builder.Services.AddSingleton(applicationEnvironment);
builder.Services.AddSingleton<TokenCredential>(_ => CredentialFactory.Create(applicationEnvironment.ManagedIdentityClientId));
builder.Services.AddSingleton<ICosmosProvisioner, CosmosProvisioner>();
builder.Services.AddSingleton<IConversionAuditRepository, CosmosConversionAuditRepository>();
builder.Services.AddSingleton<ICurrencyProviderResponseMapper, FlexibleCurrencyProviderResponseMapper>();
builder.Services.AddHttpClient<ICurrencyRateProvider, ExternalCurrencyRateProvider>((sp, client) =>
{
    var environment = sp.GetRequiredService<ApplicationEnvironment>();
    client.BaseAddress = new Uri(environment.CurrencyApiBaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();

var app = builder.Build();

await app.Services.GetRequiredService<ICosmosProvisioner>()
    .EnsureInitializedAsync(app.Logger, app.Lifetime.ApplicationStopping);

app.MapGet("/", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/conversions", async Task<IResult> (
    ConversionRequest request,
    ICurrencyConversionService service,
    CancellationToken cancellationToken) =>
{
    var validationErrors = ConversionRequestValidator.Validate(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors, title: "One or more validation errors occurred.");
    }

    try
    {
        var record = await service.CreateConversionAsync(request, cancellationToken);
        var response = ConversionAuditResponse.FromRecord(record);
        return Results.Created($"/api/conversions/{response.Id}", response);
    }
    catch (CurrencyRateProviderUnavailableException)
    {
        return Results.Problem(
            title: "Currency rate provider unavailable",
            detail: "The currency rate provider could not supply a usable rate at this time.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/conversions/{id}", async Task<IResult> (
    string id,
    IConversionAuditRepository repository,
    CancellationToken cancellationToken) =>
{
    var record = await repository.GetByIdAsync(id, cancellationToken);
    return record is null
        ? Results.NotFound()
        : Results.Ok(ConversionAuditResponse.FromRecord(record));
});

app.MapGet("/api/conversions", async Task<IResult> (
    int? limit,
    IConversionAuditRepository repository,
    CancellationToken cancellationToken) =>
{
    var effectiveLimit = limit ?? 10;
    if (effectiveLimit is <= 0 or > 100)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["limit"] = ["Limit must be between 1 and 100."]
            },
            title: "One or more validation errors occurred.");
    }

    var client = new Microsoft.Azure.Cosmos.CosmosClient(cosmosUri, credential);

app.Run();

public partial class Program
{
}

internal static partial class ConversionRequestValidator
{
    private static readonly Regex CurrencyCodePattern = new("^[A-Z]{3}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static Dictionary<string, string[]> Validate(ConversionRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (request.Amount <= 0)
        {
            AddError(nameof(request.Amount), "Amount must be greater than zero.");
        }

        ValidateCurrency(nameof(request.FromCurrency), request.FromCurrency);
        ValidateCurrency(nameof(request.ToCurrency), request.ToCurrency);

        return errors.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        void ValidateCurrency(string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !CurrencyCodePattern.IsMatch(value))
            {
                AddError(fieldName, "Currency code must be a 3-letter uppercase code.");
            }
        }

        void AddError(string key, string message)
        {
            if (!errors.TryGetValue(key, out var fieldErrors))
            {
                fieldErrors = [];
                errors[key] = fieldErrors;
            }

            fieldErrors.Add(message);
        }
    }
}
