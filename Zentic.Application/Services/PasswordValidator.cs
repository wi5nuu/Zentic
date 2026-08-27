using System.Text.RegularExpressions;
using BCrypt.Net;

namespace Zentic.Application.Services
{
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public int Score { get; set; }
    }

    public interface IPasswordValidator
    {
        PasswordValidationResult Validate(string password);
        PasswordValidationResult ValidateWithHistory(string password, IEnumerable<string> passwordHistory);
    }

    public class PasswordValidator : IPasswordValidator
    {
        private readonly PasswordOptions _options;

        public PasswordValidator(PasswordOptions? options = null)
        {
            _options = options ?? new PasswordOptions();
        }

        public PasswordValidationResult Validate(string password)
        {
            var result = new PasswordValidationResult { IsValid = true };
            
            if (string.IsNullOrWhiteSpace(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot be empty.");
                return result;
            }

            if (password.Length < _options.MinLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Password must be at least {_options.MinLength} characters long.");
            }

            if (password.Length > _options.MaxLength)
            {
                result.IsValid = false;
                result.Errors.Add($"Password cannot exceed {_options.MaxLength} characters.");
            }

            if (_options.RequireUppercase && !password.Any(char.IsUpper))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one uppercase letter.");
            }

            if (_options.RequireLowercase && !password.Any(char.IsLower))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one lowercase letter.");
            }

            if (_options.RequireDigit && !password.Any(char.IsDigit))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one digit.");
            }

            if (_options.RequireSpecialChar && !password.Any(c => _options.SpecialChars.Contains(c)))
            {
                result.IsValid = false;
                result.Errors.Add($"Password must contain at least one special character ({_options.SpecialChars}).");
            }

            if (_options.MaxRepeatingChars > 0 && HasRepeatingChars(password, _options.MaxRepeatingChars))
            {
                result.IsValid = false;
                result.Errors.Add($"Password cannot contain more than {_options.MaxRepeatingChars} repeating characters.");
            }

            if (_options.MaxSequentialChars > 0 && HasSequentialChars(password, _options.MaxSequentialChars))
            {
                result.IsValid = false;
                result.Errors.Add($"Password cannot contain more than {_options.MaxSequentialChars} sequential characters (e.g., abc, 123).");
            }

            if (_options.ForbiddenPatterns?.Any() == true)
            {
                foreach (var pattern in _options.ForbiddenPatterns)
                {
                    if (Regex.IsMatch(password, pattern, RegexOptions.IgnoreCase))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Password cannot contain forbidden pattern: {pattern}");
                    }
                }
            }

            if (_options.CheckCommonPasswords && IsCommonPassword(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password is too common. Please choose a more unique password.");
            }

            result.Score = CalculateScore(password);
            return result;
        }

        public PasswordValidationResult ValidateWithHistory(string password, IEnumerable<string> passwordHistory)
        {
            var result = Validate(password);
            
            foreach (var historyHash in passwordHistory)
            {
                if (BCrypt.Net.BCrypt.Verify(password, historyHash))
                {
                    result.IsValid = false;
                    result.Errors.Add("Password has been used recently. Please choose a different password.");
                    break;
                }
            }

            return result;
        }

        private bool HasRepeatingChars(string password, int maxRepeating)
        {
            int count = 1;
            for (int i = 1; i < password.Length; i++)
            {
                if (password[i] == password[i - 1])
                {
                    count++;
                    if (count > maxRepeating) return true;
                }
                else
                {
                    count = 1;
                }
            }
            return false;
        }

        private bool HasSequentialChars(string password, int maxSequential)
        {
            for (int i = 0; i <= password.Length - maxSequential; i++)
            {
                // Check ascending sequence (e.g., abc, 123)
                bool isAscending = true;
                for (int j = 1; j < maxSequential; j++)
                {
                    if (password[i + j] - password[i + j - 1] != 1)
                    {
                        isAscending = false;
                        break;
                    }
                }
                if (isAscending) return true;

                // Check descending sequence (e.g., cba, 321)
                bool isDescending = true;
                for (int j = 1; j < maxSequential; j++)
                {
                    if (password[i + j - 1] - password[i + j] != 1)
                    {
                        isDescending = false;
                        break;
                    }
                }
                if (isDescending) return true;
            }
            return false;
        }

        private bool IsCommonPassword(string password)
        {
            var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "password123", "123456", "123456789", "qwerty", "abc123",
                "admin", "admin123", "welcome", "welcome123", "company", "company123",
                "zentic", "zentic123", "indonesia", "jakarta", "bandung", "surabaya"
            };
            return commonPasswords.Contains(password);
        }

        private int CalculateScore(string password)
        {
            int score = 0;
            score += Math.Min(password.Length, 20) * 2;
            if (password.Any(char.IsUpper)) score += 10;
            if (password.Any(char.IsLower)) score += 10;
            if (password.Any(char.IsDigit)) score += 10;
            if (password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c))) score += 15;
            if (password.Distinct().Count() > password.Length * 0.7) score += 10;
            return Math.Min(score, 100);
        }
    }

    public class PasswordOptions
    {
        public int MinLength { get; set; } = 12;
        public int MaxLength { get; set; } = 128;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialChar { get; set; } = true;
        public string SpecialChars { get; set; } = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        public int MaxRepeatingChars { get; set; } = 2;
        public int MaxSequentialChars { get; set; } = 3;
        public List<string> ForbiddenPatterns { get; set; } = new();
        public bool CheckCommonPasswords { get; set; } = true;
        public int PasswordHistoryCount { get; set; } = 5;
        public int MaxAgeDays { get; set; } = 90;
        public int MinAgeDays { get; set; } = 1;
    }
}