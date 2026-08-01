using System.Text.RegularExpressions;

namespace OuterloopLabApi.Services;

public static class EnrollmentResolver
{
    private static readonly Regex TrailingNumberPattern = new(@"(\d+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static int? TryResolveNumber(string? enrollmentId)
    {
        if (string.IsNullOrWhiteSpace(enrollmentId))
        {
            return null;
        }

        var match = TrailingNumberPattern.Match(enrollmentId.Trim());
        if (!match.Success)
        {
            return null;
        }

        var number = int.Parse(match.Groups[1].Value);
        return number < 1000 ? number + 1000 : number;
    }

    public static string BuildContainerAppName(int enrollmentNumber) => $"ca-adlc-exp-{enrollmentNumber}";

    public static string BuildResourceGroupName(int enrollmentNumber) => $"rg-adlc-exp-2608-{enrollmentNumber}";

    public static string BuildCosmosAccountName(int enrollmentNumber) => $"cosmos-adlc-exp-{enrollmentNumber}";
}
