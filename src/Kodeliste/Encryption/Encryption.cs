using System;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Text;
using Serilog;

// TODO
// Change to prepended IV in string and not key. iv_b64 : cipher_b64

namespace PvkBroker.Kodeliste
{
    public class Encryption
    {
        static public string DecryptWithHashKey(string encryptedB64, string key)
        {
            // Generate SHA-256 hash of the key
            byte[] hashedkey = SHA256.HashData(Encoding.UTF8.GetBytes(key));

            // Use complete SHA-256 hash as key, the first 16 bytes as IV
            byte[] aesKey = hashedkey;
            byte[] aesIV = hashedkey.Take(16).ToArray();

            // Base64 decoding of the encrypted text
            byte[] encryptedBytes = Convert.FromBase64String(encryptedB64);

            // Generate AES CBC decryption object
            using (Aes aes = Aes.Create())
            {
                // using Aes aes = aes.Create();
                aes.Key = aesKey;
                aes.IV = aesIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                using ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] decryptedBytes = decryptor.TransformFinalBlock(
                    encryptedBytes, 0, encryptedBytes.Length);


                // Remove PKCS#7 padding
                decryptedBytes = RemovePadding(decryptedBytes);

                // Return decrypted text
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        static public string EncryptWithHashKey(string text, string key)
        {
            // Generate SHA-256 hash of the key
            byte[] hashedkey = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            // Use complete SHA-256 hash as key, the first 16 bytes as IV
            byte[] aesKey = hashedkey;
            byte[] aesIV = hashedkey.Take(16).ToArray();
            // Generate AES CBC encryption object
            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = aesIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                using ICryptoTransform encryptor = aes.CreateEncryptor();
                byte[] textBytes = Encoding.UTF8.GetBytes(text);
                byte[] paddedTextBytes = AddPadding(textBytes);
                // Encrypt the text
                byte[] encryptedBytes = encryptor.TransformFinalBlock(
                    paddedTextBytes, 0, paddedTextBytes.Length);
                // Return Base64 encoded encrypted text
                return Convert.ToBase64String(encryptedBytes);
            }
        }

        static byte[] RemovePadding(byte[] data)
        {
            int paddingLength = data[^1]; // last byte defines padding length
            if (paddingLength < 1 || paddingLength > 16)
            {
                Log.Error("Invalid padding length: {paddingLength} (might be due to wrong key)", paddingLength);
                throw new Exception($"Invalid padding length: {paddingLength} (might be due to wrong key)");
            }
            return data.Take(data.Length - paddingLength).ToArray();
        }

        static byte[] AddPadding(byte[] data)
        {
            int blockSize = 16;
            int paddingLength = blockSize - (data.Length % blockSize);
            if (paddingLength == 0)
            {
                return data;
            }
            byte[] paddedData = new byte[data.Length + paddingLength];
            Array.Copy(data, paddedData, data.Length);
            for (int i = data.Length; i < paddedData.Length; i++)
            {
                paddedData[i] = (byte)paddingLength;
            }
            return paddedData;
        }
    }
}

// Code example where the IV is declared in the string iv_base64:cipher_base64
// This looks a LOT better than my code from above where PKCS7 is explicitly modeled

/*
public static string DecryptAesColonSeparated(string fnrAes, byte[] key)
{
    var parts = fnrAes.Split(':');
    if (parts.Length != 2)
        throw new ArgumentException("fnrAes m� v�re p� formatet 'iv:cipher'");

    byte[] iv = Convert.FromBase64String(parts[0]);
    byte[] cipherText = Convert.FromBase64String(parts[1]);

    using var aes = Aes.Create();
    aes.Key = key;
    aes.IV = iv;
    aes.Padding = PaddingMode.PKCS7;
    aes.Mode = CipherMode.CBC;

    using var decryptor = aes.CreateDecryptor();
    using var ms = new MemoryStream(cipherText);
    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
    using var sr = new StreamReader(cs);
    return sr.ReadToEnd();
}
*/