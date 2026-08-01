using OuterloopLabApi.Models;
using OuterloopLabApi.Services;

namespace Tests.TestDoubles;

public sealed class FakeRateProvider : IRateProvider
{
    private readonly ProviderRateResult _result;
    private readonly Exception? _exception;

    public FakeRateProvider(ProviderRateResult result)
    {
        _result = result;
    }

    public FakeRateProvider(Exception exception)
    {
        _exception = exception;
        _result = default!;
    }

    public Task<ProviderRateResult> GetRateAsync(string fromCurrency, string toCurrency, CancellationToken ct)
    {
        if (_exception is not null)
            throw _exception;
        return Task.FromResult(_result);
    }
}
