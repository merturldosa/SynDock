using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is not configured.");
        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes (256 bits).");
    }

    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext: [IV (16 bytes)][ciphertext]
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        // Extract IV (first 16 bytes) and ciphertext
        const int ivLength = 16;
        if (fullCipher.Length < ivLength)
            throw new CryptographicException("Invalid ciphertext: too short.");

        var iv = new byte[ivLength];
        var cipher = new byte[fullCipher.Length - ivLength];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, ivLength);
        Buffer.BlockCopy(fullCipher, ivLength, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
