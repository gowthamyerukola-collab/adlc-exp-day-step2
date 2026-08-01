using Azure.Core;
using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Configuration;
using OuterloopLabApi.Domain;

namespace OuterloopLabApi.Services;

public sealed class CosmosConversionAuditRepository : IConversionAuditRepository
{
    private readonly ApplicationEnvironment _environment;
    private readonly Lazy<CosmosClient> _cosmosClient;

    public CosmosConversionAuditRepository(ApplicationEnvironment environment, TokenCredential credential)
    {
        _environment = environment;
        _cosmosClient = new Lazy<CosmosClient>(() => new CosmosClient(environment.CosmosDbUri!, credential));
    }

    public async Task<ConversionAuditRecord> CreateAsync(ConversionAuditRecord record, CancellationToken cancellationToken)
    {
        var container = GetContainer();
        await container.CreateItemAsync(record, new PartitionKey(record.Id), cancellationToken: cancellationToken);
        return record;
    }

    public async Task<ConversionAuditRecord?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var container = GetContainer();

        try
        {
            var response = await container.ReadItemAsync<ConversionAuditRecord>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ConversionAuditRecord>> ListRecentAsync(int limit, CancellationToken cancellationToken)
    {
        var container = GetContainer();
        var query = new QueryDefinition("SELECT TOP @limit * FROM c ORDER BY c.executionTimestampUtc DESC")
            .WithParameter("@limit", limit);

        var records = new List<ConversionAuditRecord>();
        using var iterator = container.GetItemQueryIterator<ConversionAuditRecord>(query);

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            records.AddRange(page.Resource);
        }

        return records;
    }

    private Container GetContainer()
    {
        if (!_environment.HasCompleteCosmosConfiguration)
        {
            throw new InvalidOperationException("Cosmos DB environment variables are not fully configured.");
        }

        return _cosmosClient.Value.GetContainer(_environment.CosmosDbDatabase!, _environment.CosmosDbContainer!);
    }
}
