namespace OuterloopLabApi.Contracts;

public sealed record ConversionRequest(decimal Amount, string FromCurrency, string ToCurrency);
