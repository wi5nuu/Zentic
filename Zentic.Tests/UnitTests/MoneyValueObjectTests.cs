using Zentic.Domain.Enums;
using Zentic.Domain.ValueObjects;
using Xunit;

namespace Zentic.Tests.UnitTests
{
    public class MoneyValueObjectTests
    {
        [Fact]
        public void Constructor_NegativeAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new Money(-1m));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(150000)]
        [InlineData(999999999.99)]
        public void Constructor_NonNegativeAmount_SetsAmountAndDefaultCurrency(decimal amount)
        {
            var money = new Money(amount);

            Assert.Equal(amount, money.Amount);
            Assert.Equal(Currency.IDR, money.Currency);
        }

        [Fact]
        public void Zero_ReturnsZeroAmountWithCurrency()
        {
            var money = Money.Zero(Currency.USD);

            Assert.Equal(0m, money.Amount);
            Assert.Equal(Currency.USD, money.Currency);
        }

        [Fact]
        public void Add_SameCurrency_ReturnsSum()
        {
            var a = new Money(1000m);
            var b = new Money(250m);

            var result = a.Add(b);

            Assert.Equal(1250m, result.Amount);
        }

        [Fact]
        public void Add_DifferentCurrency_ThrowsInvalidOperationException()
        {
            var idr = new Money(1000m, Currency.IDR);
            var usd = new Money(10m, Currency.USD);

            var ex = Assert.Throws<InvalidOperationException>(() => idr.Add(usd));
            Assert.Contains("different currencies", ex.Message);
        }

        [Fact]
        public void Subtract_SameCurrency_ReturnsDifference()
        {
            var a = new Money(1000m);
            var b = new Money(400m);

            var result = a.Subtract(b);

            Assert.Equal(600m, result.Amount);
        }

        [Fact]
        public void Subtract_DifferentCurrency_ThrowsInvalidOperationException()
        {
            var idr = new Money(1000m, Currency.IDR);
            var sgd = new Money(5m, Currency.SGD);

            Assert.Throws<InvalidOperationException>(() => idr.Subtract(sgd));
        }

        [Theory]
        [InlineData(100, 2, 200)]
        [InlineData(99.9, 3, 299.7)]
        [InlineData(50, 0, 0)]
        public void Multiply_Factor_ReturnsScaledAmount(decimal amount, decimal factor, decimal expected)
        {
            var money = new Money(amount);

            var result = money.Multiply(factor);

            Assert.Equal(expected, result.Amount);
        }

        [Fact]
        public void Comparison_DifferentCurrency_ThrowsInvalidOperationException()
        {
            var idr = new Money(1000m, Currency.IDR);
            var eur = new Money(1m, Currency.EUR);

            Assert.Throws<InvalidOperationException>(() => idr.IsGreaterThan(eur));
            Assert.Throws<InvalidOperationException>(() => idr.IsLessThan(eur));
        }

        [Theory]
        [InlineData(true, false, 100, 50)]
        [InlineData(false, true, 50, 100)]
        [InlineData(false, false, 100, 100)]
        public void Comparisons_SameCurrency_CompareByAmount(bool expectedGreater, bool expectedLess, decimal left, decimal right)
        {
            var a = new Money(left);
            var b = new Money(right);

            Assert.Equal(expectedGreater, a.IsGreaterThan(b));
            Assert.Equal(expectedLess, a.IsLessThan(b));
        }

        [Fact]
        public void Equality_SameAmountAndCurrency_AreEqualIncludingOperators()
        {
            var a = new Money(500m, Currency.MYR);
            var b = new Money(500m, Currency.MYR);

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equality_DifferentAmountOrCurrency_AreNotEqual()
        {
            var baseMoney = new Money(500m, Currency.MYR);

            Assert.False(baseMoney.Equals(new Money(501m, Currency.MYR)));
            Assert.False(baseMoney.Equals(new Money(500m, Currency.IDR)));
            Assert.False(baseMoney.Equals(null!));
        }

        [Fact]
        public void ToString_FormatsAmountWithCurrencyCode()
        {
            var money = new Money(1500.5m, Currency.USD);

            Assert.Equal("1,500.50 USD", money.ToString());
        }
    }
}
