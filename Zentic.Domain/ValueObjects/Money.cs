using Zentic.Domain.Enums;

namespace Zentic.Domain.ValueObjects
{
    public class Money : IEquatable<Money>
    {
        public decimal Amount { get; private set; }
        public Currency Currency { get; private set; }

        private Money() { }

        public Money(decimal amount, Currency currency = Currency.IDR)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative", nameof(amount));

            // Normalize to currency minor unit (2 decimal places to match decimal(18,2))
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            Currency = currency;
        }

        public static Money Zero(Currency currency = Currency.IDR) => new Money(0, currency);

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot subtract different currencies: {Currency} and {other.Currency}");

            return new Money(Amount - other.Amount, Currency);
        }

        public Money Multiply(decimal factor)
        {
            return new Money(Amount * factor, Currency);
        }

        public bool IsGreaterThan(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot compare different currencies: {Currency} and {other.Currency}");

            return Amount > other.Amount;
        }

        public bool IsLessThan(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Cannot compare different currencies: {Currency} and {other.Currency}");

            return Amount < other.Amount;
        }

        public override string ToString() => $"{Amount:N2} {Currency}";

        public bool Equals(Money? other)
        {
            if (other is null) return false;
            return Amount == other.Amount && Currency == other.Currency;
        }

        public override bool Equals(object? obj) => Equals(obj as Money);

        public override int GetHashCode() => HashCode.Combine(Amount, Currency);

        public static bool operator ==(Money? left, Money? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(Money? left, Money? right) => !(left == right);
    }
}
