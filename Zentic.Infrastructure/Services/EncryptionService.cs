using Zentic.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Zentic.Infrastructure.Services
{
    /// <summary>
    /// AES-256-CBC encryption for sensitive data at rest (bank account numbers).
    /// Key must be stored in secrets management, never in appsettings.json.
    /// IV is generated per-encryption and prepended to the ciphertext (standard practice).
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        public EncryptionService(string base64Key)
        {
            _key = Convert.FromBase64String(base64Key);
        }

        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV(); // unique IV per encryption — fixes static IV vulnerability

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV to ciphertext so Decrypt can recover it
            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
            return Convert.ToBase64String(result);
        }

        public string Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

            var fullBytes = Convert.FromBase64String(ciphertext);

            using var aes = Aes.Create();
            aes.Key = _key;

            // Extract IV from first 16 bytes
            var iv = new byte[16];
            var cipher = new byte[fullBytes.Length - 16];
            Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
            Buffer.BlockCopy(fullBytes, 16, cipher, 0, cipher.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Show only last 4 digits — e.g. "****1234"
        /// Masking is applied after decryption by the caller.
        /// </summary>
        public string MaskAccountNumber(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length <= 4)
                return "****";

            return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
        }
    }
}
