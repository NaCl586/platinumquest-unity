using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class PasswordEncryption
{
    private static readonly byte[] Key = CreateKey();
    private static byte[] CreateKey()
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(
                Encoding.UTF8.GetBytes("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"
                )
            );
        }
    }

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using Aes aes = Aes.Create();

        aes.Key = Key;
        aes.GenerateIV();

        using ICryptoTransform encryptor =
            aes.CreateEncryptor();

        byte[] plainBytes =
            Encoding.UTF8.GetBytes(plainText);

        byte[] encryptedBytes =
            encryptor.TransformFinalBlock(
                plainBytes,
                0,
                plainBytes.Length
            );

        // IV + encrypted data
        byte[] result =
            new byte[aes.IV.Length + encryptedBytes.Length];

        Buffer.BlockCopy(
            aes.IV,
            0,
            result,
            0,
            aes.IV.Length
        );

        Buffer.BlockCopy(
            encryptedBytes,
            0,
            result,
            aes.IV.Length,
            encryptedBytes.Length
        );

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        byte[] data =
            Convert.FromBase64String(encryptedText);

        using Aes aes = Aes.Create();

        aes.Key = Key;

        int ivLength = aes.BlockSize / 8;

        if (data.Length <= ivLength)
            throw new CryptographicException(
                "Invalid encrypted data."
            );

        byte[] iv = new byte[ivLength];

        byte[] encryptedBytes =
            new byte[data.Length - ivLength];

        Buffer.BlockCopy(
            data,
            0,
            iv,
            0,
            ivLength
        );

        Buffer.BlockCopy(
            data,
            ivLength,
            encryptedBytes,
            0,
            encryptedBytes.Length
        );

        aes.IV = iv;

        using ICryptoTransform decryptor =
            aes.CreateDecryptor();

        byte[] plainBytes =
            decryptor.TransformFinalBlock(
                encryptedBytes,
                0,
                encryptedBytes.Length
            );

        return Encoding.UTF8.GetString(plainBytes);
    }
}