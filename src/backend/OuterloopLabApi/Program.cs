using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Api;
using OuterloopLabApi.Cosmos;
using OuterloopLabApi.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

static string GetRequiredEnv(string key)
{
    var value = Environment.GetEnvironmentVariable(key);
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Missing required environment variable: {key}");
    return value;
}

var cosmosUri = GetRequiredEnv("COSMOS_DB_URI");
var cosmosDbName = GetRequiredEnv("COSMOS_DB_DATABASE");
var cosmosContainerName = GetRequiredEnv("COSMOS_DB_CONTAINER");
var cosmosAccountName = GetRequiredEnv("COSMOS_DB_ACCOUNT_NAME");
var cosmosResourceGroup = GetRequiredEnv("COSMOS_DB_RESOURCE_GROUP");
var cosmosRegion = GetRequiredEnv("COSMOS_DB_REGION");

// External currency conversion base URL: Frankfurter v1 as a default and a stable provider for rates.
var currencyApiBaseUrl = Environment.GetEnvironmentVariable("CURRENCY_API_BASE_URL")?.Trim();
if (string.IsNullOrWhiteSpace(currencyApiBaseUrl))
    currencyApiBaseUrl = "https://frankfurter.dev";

Container cosmosContainer;

// [CRITICAL] Dual-plane Cosmos startup implementation (as required):
// See: ./docs/azure-cosmos-reference.md
// Step 1: Best-effort ARM Client Control-Plane Provisioning
try
{
    var managedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID") ?? string.Empty;
    var credential = string.IsNullOrEmpty(managedIdentityClientId)
        ? new DefaultAzureCredential()
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId });

    var armClient = new ArmClient(credential);
    var subscription = await armClient.GetDefaultSubscriptionAsync();
    var resourceGroupResource = await subscription.GetResourceGroupAsync(cosmosResourceGroup);
    var cosmosAccount = await resourceGroupResource.Value.GetCosmosDBAccountAsync(cosmosAccountName);

    var sqlDbContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(
        new AzureLocation(cosmosRegion),
        new CosmosDBSqlDatabaseResourceInfo(cosmosDbName));

    await cosmosAccount.Value.GetCosmosDBSqlDatabases().CreateOrUpdateAsync(
        Azure.WaitUntil.Completed,
        cosmosDbName,
        sqlDbContent);

    var containerInfo = new CosmosDBSqlContainerResourceInfo(cosmosContainerName)
    {
        PartitionKey = new CosmosDBContainerPartitionKey { Paths = { "/id" } }
    };

    var sqlContainerContent = new CosmosDBSqlContainerCreateOrUpdateContent(
        new AzureLocation(cosmosRegion),
        containerInfo);

    var sqlDbResource = await cosmosAccount.Value.GetCosmosDBSqlDatabases().GetAsync(cosmosDbName);
    await sqlDbResource.Value.GetCosmosDBSqlContainers().CreateOrUpdateAsync(
        Azure.WaitUntil.Completed,
        cosmosContainerName,
        sqlContainerContent);
}
catch (Exception ex)
{
    Console.WriteLine($"[STARTUP WARNING] ARM Provisioning skipped/failed: {ex.Message}");
    // Proceed seamlessly to required token-authenticated Data-Plane verification step below
}

// Step 2: Mandatory Token-Authenticated Data-Plane Fallback
try
{
    var managedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_MANAGED_IDENTITY_CLIENT_ID") ?? string.Empty;
    var credential = string.IsNullOrEmpty(managedIdentityClientId)
        ? new DefaultAzureCredential()
        : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId });

    var client = new Microsoft.Azure.Cosmos.CosmosClient(cosmosUri, credential);
    var dbResponse = await client.CreateDatabaseIfNotExistsAsync(cosmosDbName);
    var _ = await dbResponse.Database.CreateContainerIfNotExistsAsync(cosmosContainerName, "/id");

    cosmosContainer = client.GetContainer(cosmosDbName, cosmosContainerName);
}
catch (Exception ex)
{
    // Step 3: Terminal Exit on Failure
    throw new InvalidOperationException("CRITICAL: Token-authenticated data-plane initialization failed. Terminating application.", ex);
}

builder.Services.AddSingleton(cosmosContainer);
builder.Services.AddSingleton<IAuditRepository>(sp => new CosmosAuditRepository(sp.GetRequiredService<Container>()));
builder.Services.AddSingleton<IRateProvider>(sp => new FrankfurterRateProvider(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("currency-rate-provider"),
    currencyApiBaseUrl));
builder.Services.AddHttpClient("currency-rate-provider");
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}



app.MapPost("/api/convert", async (ConvertRequest request, ICurrencyConversionService conversionService, CancellationToken ct) =>
{
    try
    {
        var result = await conversionService.ConvertAsync(request.Amount, request.FromCurrency, request.ToCurrency, ct);
        return Results.Ok(result);
    }
    catch (CurrencyValidationException vex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["validation"] = new[] { vex.Message }
        });
    }
    catch (CurrencyRateProviderException)
    {
        var pd = ProblemDetailsFactory.CurrencyRateProviderUnavailable();
        return Results.Problem(detail: pd.Detail, title: pd.Title, statusCode: pd.Status!.Value);
    }
});

app.MapGet("/api/audits/{auditId}", async (string auditId, ICurrencyConversionService conversionService, CancellationToken ct) =>
{
    try
    {
        var record = await conversionService.GetAuditAsync(auditId, ct);
        return record is null ? Results.NotFound() : Results.Ok(record);
    }
    catch (CurrencyValidationException vex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["validation"] = new[] { vex.Message }
        });
    }
});

app.Run();
