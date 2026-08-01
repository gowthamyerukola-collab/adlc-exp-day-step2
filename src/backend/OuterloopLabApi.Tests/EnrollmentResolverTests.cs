using OuterloopLabApi.Services;

namespace OuterloopLabApi.Tests;

public class EnrollmentResolverTests
{
    [Theory]
    [InlineData("1100", 1100)]
    [InlineData("1001", 1001)]
    [InlineData("100", 1100)]
    [InlineData("adlc-exp-1100", 1100)]
    [InlineData("day2", 1002)]
    public void TryResolveNumber_ExtractsTrailingNumber(string input, int expected)
    {
        Assert.Equal(expected, EnrollmentResolver.TryResolveNumber(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-numbers-here")]
    public void TryResolveNumber_ReturnsNull_ForInvalidInput(string? input)
    {
        Assert.Null(EnrollmentResolver.TryResolveNumber(input));
    }

    [Theory]
    [InlineData(1100, "ca-adlc-exp-1100")]
    [InlineData(1001, "ca-adlc-exp-1001")]
    public void BuildContainerAppName_FormatsName(int number, string expected)
    {
        Assert.Equal(expected, EnrollmentResolver.BuildContainerAppName(number));
    }
}
