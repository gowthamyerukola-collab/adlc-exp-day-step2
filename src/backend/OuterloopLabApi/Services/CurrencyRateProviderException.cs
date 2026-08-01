namespace OuterloopLabApi.Services;

public sealed class CurrencyRateProviderException : Exception
{
    public CurrencyRateProviderException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
