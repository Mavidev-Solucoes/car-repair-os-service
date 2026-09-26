using System.Text.RegularExpressions;

namespace Application.Common.Validators;

public static partial class BrazilianLicensePlateValidator
{
    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LicensePlateRegex();

    public static bool IsValid(string? licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
        {
            return false;
        }

        var normalized = new string(licensePlate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return LicensePlateRegex().IsMatch(normalized);
    }
}
