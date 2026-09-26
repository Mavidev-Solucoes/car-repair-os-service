namespace Domain.ValueObjects;

public sealed class PhoneNumber
{
    public PhoneNumber(string value)
    {
        var digits = Normalize(value);
        if (digits.Length is not (10 or 11))
        {
            throw new ArgumentException("Telephone must contain 10 or 11 digits.", nameof(value));
        }

        Value = digits;
    }

    public string Value { get; }

    public static string Normalize(string value) => new string(value.Where(char.IsDigit).ToArray());
}
