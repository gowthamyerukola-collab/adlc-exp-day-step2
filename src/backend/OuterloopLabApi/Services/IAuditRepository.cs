using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public interface IAuditRepository
{
    Task CreateAsync(AuditRecord record, CancellationToken ct);
    Task<AuditRecord?> GetAsync(string id, CancellationToken ct);
}
