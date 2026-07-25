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
    }
}
