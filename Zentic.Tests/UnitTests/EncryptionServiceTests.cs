using Zentic.Infrastructure.Services;
using System.Security.Cryptography;
using Xunit;

namespace Zentic.Tests.UnitTests
{
    public class EncryptionServiceTests
    {
        private static string CreateValidBase64Key()
        {
            var key = new byte[32]; // 256 bits
            RandomNumberGenerator.Fill(key);
            return Convert.ToBase64String(key);
        }

        [Fact]
        public void Encrypt_EmptyString_ReturnsEmpty()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var result = service.Encrypt("");
            Assert.Equal("", result);
        }

        [Fact]
        public void Decrypt_EmptyString_ReturnsEmpty()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var result = service.Decrypt("");
            Assert.Equal("", result);
        }

        [Fact]
        public void Encrypt_Null_ReturnsNull()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var result = service.Encrypt(null!);
            Assert.Null(result);
        }

        [Fact]
        public void Decrypt_Null_ReturnsNull()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var result = service.Decrypt(null!);
            Assert.Null(result);
        }

        [Fact]
        public void EncryptDecrypt_RoundTrip_ReturnsOriginal()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var plaintext = "1234567890";
            var encrypted = service.Encrypt(plaintext);
            var decrypted = service.Decrypt(encrypted);
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void EncryptDecrypt_LongString_ReturnsOriginal()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var plaintext = new string('A', 10000);
            var encrypted = service.Encrypt(plaintext);
            var decrypted = service.Decrypt(encrypted);
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void EncryptDecrypt_SpecialCharacters_ReturnsOriginal()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var plaintext = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            var encrypted = service.Encrypt(plaintext);
            var decrypted = service.Decrypt(encrypted);
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void EncryptDecrypt_Unicode_ReturnsOriginal()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var plaintext = "Halo Dunia 你好世界 مرحبا";
            var encrypted = service.Encrypt(plaintext);
            var decrypted = service.Decrypt(encrypted);
            Assert.Equal(plaintext, decrypted);
        }

        [Fact]
        public void Encrypt_ProducesDifferentCiphertextEachTime()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var plaintext = "same input";
            var encrypted1 = service.Encrypt(plaintext);
            var encrypted2 = service.Encrypt(plaintext);
            Assert.NotEqual(encrypted1, encrypted2); // Different nonces
        }

        [Fact]
        public void Decrypt_WithWrongKey_ThrowsCryptographicException()
        {
            var service1 = new EncryptionService(CreateValidBase64Key());
            var service2 = new EncryptionService(CreateValidBase64Key());
            var encrypted = service1.Encrypt("secret");
            var ex = Record.Exception(() => service2.Decrypt(encrypted));
            Assert.NotNull(ex);
            Assert.IsAssignableFrom<CryptographicException>(ex);
        }

        [Fact]
        public void GetCurrentKeyId_ReturnsKeyId()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var keyId = service.GetCurrentKeyId();
            Assert.StartsWith("ZENC", keyId);
            Assert.NotEmpty(keyId);
        }

        [Fact]
        public void RotateKey_NewEncryptionsUseNewKey()
        {
            var key1 = CreateValidBase64Key();
            var key2 = CreateValidBase64Key();
            var service = new EncryptionService(key1);
            var originalKeyId = service.GetCurrentKeyId();

            service.RotateKey(key2);
            var newKeyId = service.GetCurrentKeyId();

            Assert.NotEqual(originalKeyId, newKeyId);

            // New encryptions use new key
            var encrypted = service.Encrypt("test");
            var decrypted = service.Decrypt(encrypted);
            Assert.Equal("test", decrypted);
        }

        [Fact]
        public void MaskAccountNumber_ShortNumber_ReturnsStars()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var result = service.MaskAccountNumber("1234");
            Assert.Equal("****", result);
        }

        [Fact]
        public void MaskAccountNumber_LongNumber_ShowsLastFour()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var result = service.MaskAccountNumber("1234567890");
            Assert.Equal("******7890", result);
        }

        [Fact]
        public void MaskAccountNumber_Null_ReturnsStars()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var result = service.MaskAccountNumber(null!);
            Assert.Equal("****", result);
        }

        [Fact]
        public void MaskAccountNumber_Empty_ReturnsStars()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var result = service.MaskAccountNumber("");
            Assert.Equal("****", result);
        }

        [Fact]
        public void MaskEncryptedAccountNumber_ReturnsMasked()
        {
            var service = new EncryptionService(CreateValidBase64Key());
            var input = "abcdefghijklmnop";
            var result = service.MaskEncryptedAccountNumber(input);
            Assert.StartsWith("**", result);
            Assert.Equal(input.Length, result.Length);
            Assert.EndsWith("ijklmnop", result);
        }

        [Fact]
        public void ReEncrypt_WithRotatedKey_StillDecrypts()
        {
            var key1 = CreateValidBase64Key();
            var key2 = CreateValidBase64Key();
            var service = new EncryptionService(key1);

            var original = "sensitive data";
            var encrypted = service.Encrypt(original);

            service.RotateKey(key2);
            var reEncrypted = service.ReEncrypt(encrypted);
            var decrypted = service.Decrypt(reEncrypted);

            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void Constructor_EmptyKey_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new EncryptionService(""));
        }

        [Fact]
        public void Constructor_InvalidKeyLength_ThrowsArgumentException()
        {
            var shortKey = Convert.ToBase64String(new byte[16]); // Too short
            Assert.Throws<ArgumentException>(() => new EncryptionService(shortKey));
        }
    }
}
