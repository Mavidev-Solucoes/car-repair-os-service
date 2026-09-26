using System.Text.RegularExpressions;

namespace Application.Common.Validators;

public static class BrazilianLicensePlateValidator
{
    private static readonly Regex MercosulPattern =
        new(@"^[A-Za-z]{3}[0-9]{1}[A-Za-z]{1}[0-9]{2}$", RegexOptions.Compiled);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var stripped = new string(value.Where(c => c != '-').ToArray());
        return MercosulPattern.IsMatch(stripped);
    }
}
