using Microsoft.Azure.Cosmos;
using OuterloopLabApi.Models;
using OuterloopLabApi.Services;
using System.Net;

namespace OuterloopLabApi.Cosmos;

public sealed class CosmosAuditRepository : IAuditRepository
{
    private readonly Container _container;

    public CosmosAuditRepository(Container container)
    {
        _container = container;
    }

    public async Task CreateAsync(AuditRecord record, CancellationToken ct)
    {
        await _container.CreateItemAsync(record, new PartitionKey(record.Id), cancellationToken: ct);
    }

    public async Task<AuditRecord?> GetAsync(string id, CancellationToken ct)
    {
        try
        {
            var response = await _container.ReadItemAsync<AuditRecord>(id, new PartitionKey(id), cancellationToken: ct);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
