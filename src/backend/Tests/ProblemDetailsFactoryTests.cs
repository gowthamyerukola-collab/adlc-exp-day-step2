using OuterloopLabApi.Api;
using Xunit;

namespace Tests;

public sealed class ProblemDetailsFactoryTests
{
    [Fact]
    public void CurrencyRateProviderUnavailable_Has_503_And_Expected_Title()
    {
        var pd = ProblemDetailsFactory.CurrencyRateProviderUnavailable();
        Assert.Equal(503, pd.Status);
        Assert.Equal("Currency rate provider unavailable", pd.Title);
    }
}
