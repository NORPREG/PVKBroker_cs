using System;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Text;

namespace HelseId.Samples.PVKBroker.Encryption
{
    class Encryption
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

        static byte[] RemovePadding(byte[] data)
        {
            int paddingLength = data[^1]; // last byte defines padding length
            if (paddingLength < 1 || paddingLength > 16)
            {
                throw new Exception($"Invalid padding length: {paddingLength}");
            }
            return data.Take(data.Length - paddingLength).ToArray();
        }
    }
}