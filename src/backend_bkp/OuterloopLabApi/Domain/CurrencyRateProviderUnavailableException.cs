namespace OuterloopLabApi.Domain;

public sealed class CurrencyRateProviderUnavailableException : Exception
{
    public CurrencyRateProviderUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
