namespace Domain.ValueObjects;

public class PhoneNumber
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 10) // Add your strict regex here later
            throw new ArgumentException("Phone number is invalid.");
        Value = value;
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;
    public static explicit operator PhoneNumber(string value) => new(value);
}