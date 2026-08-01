using OuterloopLabApi.Models;

namespace OuterloopLabApi.Services;

public interface IRateProvider
{
    Task<ProviderRateResult> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken ct);
}
