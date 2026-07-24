using System.Security.Cryptography;
using System.Text;

namespace JournalApp.Services;

/// <summary>AES-256 encryption for journal text before it leaves the device. Ciphertext is base64(IV + cipher).</summary>
public static class EncryptionService
{
    private static readonly byte[] _Key = SHA256.HashData(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "default_key_for_demo_only"));

    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _Key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String([.. aes.IV, .. cipherBytes]);
    }

    public static string Decrypt(string cipherText)
    {
        var all = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = _Key;
        aes.IV = all[..16];

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(all, 16, all.Length - 16);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
