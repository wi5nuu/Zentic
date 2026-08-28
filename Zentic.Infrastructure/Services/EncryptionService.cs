using Zentic.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Zentic.Infrastructure.Services
{
    /// <summary>
    /// AES-256-GCM authenticated encryption for sensitive data at rest (bank account numbers).
    /// Key must be stored in secrets management, never in appsettings.json.
    /// Supports key rotation with key IDs. Key ID is derived from key hash for deterministic identification.
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private const int KeySize = 32; // 256 bits
        private const int NonceSize = 12; // 96 bits for GCM
        private const int TagSize = 16; // 128 bits
        private const string KeyIdPrefix = "ZENC";
        
        private readonly Dictionary<string, byte[]> _keys = new();
        private readonly object _keyLock = new();
        private string _currentKeyId;

        public EncryptionService(string base64Key)
        {
            if (string.IsNullOrWhiteSpace(base64Key))
                throw new ArgumentException("Encryption key cannot be empty.", nameof(base64Key));

            var keyBytes = Convert.FromBase64String(base64Key);
            ValidateKey(keyBytes);
            
            _currentKeyId = DeriveKeyId(keyBytes);
            _keys[_currentKeyId] = keyBytes;
        }

        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;

            byte[] key;
            string currentKeyId;
            lock (_keyLock)
            {
                key = _keys[_currentKeyId];
                currentKeyId = _currentKeyId;
            }
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            
            using var aes = new AesGcm(key, TagSize);
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Format: keyId + ':' + nonce + ciphertext + tag
            var keyIdBytes = Encoding.UTF8.GetBytes(currentKeyId);
            var result = new byte[keyIdBytes.Length + 1 + NonceSize + cipherBytes.Length + TagSize];
            Buffer.BlockCopy(keyIdBytes, 0, result, 0, keyIdBytes.Length);
            result[keyIdBytes.Length] = (byte)':'; // delimiter
            Buffer.BlockCopy(nonce, 0, result, keyIdBytes.Length + 1, NonceSize);
            Buffer.BlockCopy(cipherBytes, 0, result, keyIdBytes.Length + 1 + NonceSize, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, result, keyIdBytes.Length + 1 + NonceSize + cipherBytes.Length, TagSize);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

            try
            {
                var fullBytes = Convert.FromBase64String(ciphertext);
                
                // Try to parse key ID from the beginning
                var delimiterIndex = Array.IndexOf(fullBytes, (byte)':');
                if (delimiterIndex > 0 && delimiterIndex < 50)
                {
                    var keyId = Encoding.UTF8.GetString(fullBytes, 0, delimiterIndex);
                    lock (_keyLock)
                    {
                        if (_keys.TryGetValue(keyId, out var key))
                            return DecryptWithKey(fullBytes, delimiterIndex + 1, key);
                    }
                }

                // Fallback to current key (for legacy data without key ID)
                lock (_keyLock)
                {
                    return DecryptWithKey(fullBytes, 0, _keys[_currentKeyId]);
                }
            }
            catch (CryptographicException)
            {
                throw;
            }
            catch
            {
                throw new CryptographicException("Failed to decrypt data. Data may be corrupted or encrypted with unknown key.");
            }
        }

        private string DecryptWithKey(byte[] fullBytes, int offset, byte[] key)
        {
            if (fullBytes.Length < offset + NonceSize + TagSize)
                throw new CryptographicException("Invalid ciphertext format.");

            var nonce = new byte[NonceSize];
            Buffer.BlockCopy(fullBytes, offset, nonce, 0, NonceSize);

            var cipherLength = fullBytes.Length - offset - NonceSize - TagSize;
            var cipherBytes = new byte[cipherLength];
            Buffer.BlockCopy(fullBytes, offset + NonceSize, cipherBytes, 0, cipherLength);

            var tag = new byte[TagSize];
            Buffer.BlockCopy(fullBytes, offset + NonceSize + cipherLength, tag, 0, TagSize);

            using var aes = new AesGcm(key, TagSize);
            var plainBytes = new byte[cipherLength];
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }

        public string MaskAccountNumber(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length <= 4)
                return "****";

            return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
        }

        /// <summary>
        /// Masks an encrypted account number (Base64) without decrypting it.
        /// Used for list views where decryption is not needed.
        /// </summary>
        public string MaskEncryptedAccountNumber(string encryptedAccountNumber)
        {
            if (string.IsNullOrEmpty(encryptedAccountNumber))
                return "****";

            // Encrypted data is Base64, mask most of it
            if (encryptedAccountNumber.Length <= 8)
                return "****";

            return new string('*', encryptedAccountNumber.Length - 8) + encryptedAccountNumber[^8..];
        }

        public string GetCurrentKeyId() => _currentKeyId;

        public void RotateKey(string newBase64Key)
        {
            var newKeyBytes = Convert.FromBase64String(newBase64Key);
            ValidateKey(newKeyBytes);

            var newKeyId = DeriveKeyId(newKeyBytes);
            lock (_keyLock)
            {
                _keys[newKeyId] = newKeyBytes;
                _currentKeyId = newKeyId;
            }
        }

        /// <summary>
        /// Re-encrypts data with the current key. Useful for migrating legacy data after key rotation.
        /// </summary>
        public string ReEncrypt(string ciphertext)
        {
            var plaintext = Decrypt(ciphertext);
            return Encrypt(plaintext);
        }

        private static void ValidateKey(byte[] key)
        {
            if (key.Length != KeySize)
                throw new ArgumentException($"Encryption key must be {KeySize} bytes (256 bits) for AES-256-GCM.", nameof(key));
        }

        private static string DeriveKeyId(byte[] key)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(key);
            return $"{KeyIdPrefix}{Convert.ToHexString(hash[..8]).ToLowerInvariant()}";
        }
    }
}
