using Microsoft.AspNetCore.Mvc;

namespace OuterloopLabApi.Api;

public static class ProblemDetailsFactory
{
    public static ProblemDetails CurrencyRateProviderUnavailable(string? detail = null)
    {
        return new ProblemDetails
        {
            Title = "Currency rate provider unavailable",
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = detail ?? "Unable to fetch currency exchange rate from external provider."
        };
    }
}
