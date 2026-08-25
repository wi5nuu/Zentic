using System.Text.RegularExpressions;

namespace Zentic.Domain.ValueObjects
{
    public class PhoneNumber : IEquatable<PhoneNumber>
    {
        // Indonesian phone number format: +62 or 0 followed by 8-13 digits
        private static readonly Regex PhoneRegex = new Regex(
            @"^(\+62|62|0)[0-9]{8,13}$",
            RegexOptions.Compiled);

        public string Value { get; private set; }

        private PhoneNumber() { Value = string.Empty; }

        public PhoneNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Phone number cannot be empty", nameof(value));

            // Remove common separators
            value = value.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            if (!PhoneRegex.IsMatch(value))
                throw new ArgumentException($"Invalid phone number format: {value}", nameof(value));

            Value = NormalizeFormat(value);
        }

        private static string NormalizeFormat(string value)
        {
            // Convert to international format +62
            if (value.StartsWith("0"))
                return "+62" + value.Substring(1);
            if (value.StartsWith("62"))
                return "+" + value;
            return value;
        }

        public static bool IsValid(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            return PhoneRegex.IsMatch(value);
        }

        public override string ToString() => Value;

        public string ToDisplayFormat()
        {
            // Format: +62 812-3456-7890
            if (Value.StartsWith("+62") && Value.Length > 3)
            {
                var number = Value.Substring(3);
                if (number.Length >= 10)
                {
                    return $"+62 {number.Substring(0, 3)}-{number.Substring(3, 4)}-{number.Substring(7)}";
                }
            }
            return Value;
        }

        public bool Equals(PhoneNumber? other)
        {
            if (other is null) return false;
            return Value == other.Value;
        }

        public override bool Equals(object? obj) => Equals(obj as PhoneNumber);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(PhoneNumber? left, PhoneNumber? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(PhoneNumber? left, PhoneNumber? right) => !(left == right);

        public static implicit operator string(PhoneNumber phone) => phone.Value;
    }
}
