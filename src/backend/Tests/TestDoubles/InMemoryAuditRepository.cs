using OuterloopLabApi.Models;
using OuterloopLabApi.Services;

namespace Tests.TestDoubles;

public sealed class InMemoryAuditRepository : IAuditRepository
{
    private readonly Dictionary<string, AuditRecord> _store = new();

    public Task CreateAsync(AuditRecord record, CancellationToken ct)
    {
        _store[record.Id] = record;
        return Task.CompletedTask;
    }

    public Task<AuditRecord?> GetAsync(string id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var record);
        return Task.FromResult(record);
    }
}
