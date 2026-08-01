namespace OuterloopLabApi.Services;

public sealed class CurrencyValidationException : Exception
{
    public CurrencyValidationException(string message) : base(message)
    {
    }
}
