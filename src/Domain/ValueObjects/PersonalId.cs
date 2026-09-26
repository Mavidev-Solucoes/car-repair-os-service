namespace Domain.ValueObjects;

public sealed class PersonalId
{
    public PersonalId(string value)
    {
        var digits = Normalize(value);
        if (digits.Length is not (11 or 14))
        {
            throw new ArgumentException("PersonalId must contain 11 or 14 digits.", nameof(value));
        }

        Value = digits;
    }

    public string Value { get; }

    public static string Normalize(string value) => new string(value.Where(char.IsDigit).ToArray());
}
