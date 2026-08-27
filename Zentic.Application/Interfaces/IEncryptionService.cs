namespace Zentic.Application.Interfaces
{
    /// <summary>
    /// Service untuk enkripsi/dekripsi data sensitif (nomor rekening bank)
    /// </summary>
    public interface IEncryptionService
    {
        string Encrypt(string plaintext);
        string Decrypt(string ciphertext);
        string MaskAccountNumber(string accountNumber);
        string MaskEncryptedAccountNumber(string encryptedAccountNumber);
        string GetCurrentKeyId();
        void RotateKey(string newBase64Key);
        string ReEncrypt(string ciphertext);
    }
}
