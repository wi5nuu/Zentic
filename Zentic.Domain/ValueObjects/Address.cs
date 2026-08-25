namespace Zentic.Domain.ValueObjects
{
    public class Address : IEquatable<Address>
    {
        public string Street { get; private set; }
        public string City { get; private set; }
        public string Province { get; private set; }
        public string PostalCode { get; private set; }
        public string Country { get; private set; }

        private Address() 
        { 
            Street = string.Empty;
            City = string.Empty;
            Province = string.Empty;
            PostalCode = string.Empty;
            Country = string.Empty;
        }

        public Address(string street, string city, string province, string postalCode, string country = "Indonesia")
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new ArgumentException("Street cannot be empty", nameof(street));
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City cannot be empty", nameof(city));
            if (string.IsNullOrWhiteSpace(province))
                throw new ArgumentException("Province cannot be empty", nameof(province));
            if (string.IsNullOrWhiteSpace(country))
                throw new ArgumentException("Country cannot be empty", nameof(country));

            Street = street.Trim();
            City = city.Trim();
            Province = province.Trim();
            PostalCode = postalCode?.Trim() ?? string.Empty;
            Country = country.Trim();
        }

        public string GetFullAddress()
        {
            var parts = new List<string> { Street, City, Province };
            
            if (!string.IsNullOrWhiteSpace(PostalCode))
                parts.Add(PostalCode);
            
            parts.Add(Country);

            return string.Join(", ", parts);
        }

        public override string ToString() => GetFullAddress();

        public bool Equals(Address? other)
        {
            if (other is null) return false;
            return Street == other.Street
                && City == other.City
                && Province == other.Province
                && PostalCode == other.PostalCode
                && Country == other.Country;
        }

        public override bool Equals(object? obj) => Equals(obj as Address);

        public override int GetHashCode() => HashCode.Combine(Street, City, Province, PostalCode, Country);

        public static bool operator ==(Address? left, Address? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(Address? left, Address? right) => !(left == right);
    }
}
