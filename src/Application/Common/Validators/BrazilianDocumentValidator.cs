namespace Application.Common.Validators;

public static class BrazilianDocumentValidator
{
    public static bool IsValidCpfOrCnpj(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length is 11 or 14;
    }
}
