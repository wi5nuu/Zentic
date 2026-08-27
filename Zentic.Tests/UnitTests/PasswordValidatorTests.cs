using Zentic.Application.Services;
using Xunit;

namespace Zentic.Tests.UnitTests
{
    public class PasswordValidatorTests
    {
        private static PasswordOptions Relaxed() => new()
        {
            MinLength = 8,
            MaxLength = 64,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialChar = true,
            MaxRepeatingChars = 2,
            MaxSequentialChars = 3,
            CheckCommonPasswords = true,
            ForbiddenPatterns = new List<string>()
        };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_EmptyPassword_ReturnsInvalid(string? password)
        {
            var validator = new PasswordValidator(Relaxed());

            var result = validator.Validate(password!);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("empty"));
        }

        [Fact]
        public void Validate_BelowMinLength_ReturnsInvalidWithLengthError()
        {
            var validator = new PasswordValidator(Relaxed());

            var result = validator.Validate("Sh0rt!");

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("at least"));
        }

        [Fact]
        public void Validate_StrongPassword_ReturnsValidWithHighScore()
        {
            var validator = new PasswordValidator(Relaxed());

            var result = validator.Validate("Str0ng!Secure#2026");

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.True(result.Score >= 60);
        }

        [Theory]
        [InlineData("alllowercase1!", "uppercase")]
        [InlineData("ALLUPPERCASE1!", "lowercase")]
        [InlineData("NoDigitsHere!!", "digit")]
        [InlineData("NoSpecial123Abc", "special character")]
        public void Validate_MissingCharacterClass_ReturnsSpecificError(string password, string expectedFragment)
        {
            var options = Relaxed();
            options.MinLength = 4;
            var validator = new PasswordValidator(options);

            var result = validator.Validate(password);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.ToLower().Contains(expectedFragment));
        }

        [Fact]
        public void Validate_MoreThanMaxRepeatingChars_ReturnsInvalid()
        {
            var validator = new PasswordValidator(Relaxed());

            var result = validator.Validate("HelloWorld111!x");

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("repeating"));
        }

        [Fact]
        public void Validate_SequentialRun_ReturnsInvalid()
        {
            var validator = new PasswordValidator(Relaxed());

            var result = validator.Validate("qWerty12345!@#");

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("sequential"));
        }

        [Fact]
        public void Validate_CommonPassword_ReturnsInvalid()
        {
            var options = Relaxed();
            options.MinLength = 6;
            var validator = new PasswordValidator(options);

            var result = validator.Validate("password123");

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("too common"));
        }

        [Fact]
        public void Validate_ForbiddenPattern_ReturnsInvalid()
        {
            var options = Relaxed();
            options.ForbiddenPatterns = new List<string> { @"zentic" };
            var validator = new PasswordValidator(options);

            var result = validator.Validate("MyZenticPass1!");

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("forbidden pattern"));
        }

        [Fact]
        public void ValidateWithHistory_PasswordMatchesHistory_ReturnsInvalidWithReuseError()
        {
            var validator = new PasswordValidator(Relaxed());
            var previous = "Older#Passw0rd!";
            var historyHashes = new[] { BCrypt.Net.BCrypt.HashPassword(previous, 11) };

            var result = validator.ValidateWithHistory(previous, historyHashes);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("used recently"));
        }

        [Fact]
        public void ValidateWithHistory_UniquePassword_ReturnsValid()
        {
            var validator = new PasswordValidator(Relaxed());
            var historyHashes = new[]
            {
                BCrypt.Net.BCrypt.HashPassword("Older#Passw0rd!", 11),
                BCrypt.Net.BCrypt.HashPassword("Even0lder!Pass#", 11)
            };

            var result = validator.ValidateWithHistory("Br4nd!New#Secret", historyHashes);

            Assert.True(result.IsValid);
        }
    }
}
