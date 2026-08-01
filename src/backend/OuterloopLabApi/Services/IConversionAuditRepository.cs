using OuterloopLabApi.Domain;

namespace OuterloopLabApi.Services;

public interface IConversionAuditRepository
{
    Task<ConversionAuditRecord> CreateAsync(ConversionAuditRecord record, CancellationToken cancellationToken);
    Task<ConversionAuditRecord?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ConversionAuditRecord>> ListRecentAsync(int limit, CancellationToken cancellationToken);
}
