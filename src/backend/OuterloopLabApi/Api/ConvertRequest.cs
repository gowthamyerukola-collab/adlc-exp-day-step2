namespace OuterloopLabApi.Api;

public sealed record ConvertRequest(
    decimal Amount,
    string FromCurrency,
    string ToCurrency);
