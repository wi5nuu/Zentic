using System.Text.RegularExpressions;

namespace Zentic.Domain.ValueObjects
{
    public class Email : IEquatable<Email>
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Value { get; private set; }

        private Email() { Value = string.Empty; }

        public Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty", nameof(value));

            value = value.Trim().ToLowerInvariant();

            if (!EmailRegex.IsMatch(value))
                throw new ArgumentException($"Invalid email format: {value}", nameof(value));

            Value = value;
        }

        public static bool IsValid(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return EmailRegex.IsMatch(value.Trim());
        }

        public string Domain
        {
            get
            {
                var at = Value.IndexOf('@');
                return at < 0 ? string.Empty : Value.Substring(at + 1);
            }
        }

        public override string ToString() => Value;

        public bool Equals(Email? other)
        {
            if (other is null) return false;
            return Value == other.Value;
        }

        public override bool Equals(object? obj) => Equals(obj as Email);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(Email? left, Email? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(Email? left, Email? right) => !(left == right);

        public static implicit operator string(Email email) => email.Value;
    }
}
