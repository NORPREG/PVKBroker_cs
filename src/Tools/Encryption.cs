using System;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Text;
using Serilog;

using PvkBroker.Configuration;

// TODO
// Change to prepended IV in string and not key. iv_b64 : cipher_b64
// Key length needs to be 16, 24 or 32 bytes for AES-128/192/256

namespace PvkBroker.Tools
{
    public class Encryption
    {
        public static string Decrypt(string ivCipher)
        {
            var parts = ivCipher.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("ivCipher m� v�re p� formatet 'iv:cipher'");

            string? aes_key_ascii = ConfigurationValues.KodelisteAesKey;

            if (string.IsNullOrWhiteSpace(aes_key_ascii))
            {
                Log.Error("Kodeliste AES key is not set in configuration. Cannot decrypt.");
                throw new InvalidOperationException("Kodeliste AES key is not set in configuration. Cannot decrypt.");
            }

            byte[] key = Encoding.ASCII.GetBytes(aes_key_ascii);
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

        public static string Encrypt(string plaintext)
        {
            string? aes_key_ascii = ConfigurationValues.KodelisteAesKey;

            if (string.IsNullOrWhiteSpace(aes_key_ascii))
            {
                Log.Error("Kodeliste AES key is not set in configuration. Cannot encrypt.");
                throw new InvalidOperationException("Kodeliste AES key is not set in configuration. Cannot encrypt.");
            }

            byte[] key = Encoding.ASCII.GetBytes(aes_key_ascii);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plaintext);
            }

            string ivBase64 = Convert.ToBase64String(aes.IV);
            string cipherBase64 = Convert.ToBase64String(ms.ToArray());
            return $"{ivBase64}:{cipherBase64}";
        }

        // To encrypt and decrypt is_reserved status
        public static string EncryptBool(bool value)
        {
            // Convert boolean to string
            string text = value ? "1" : "0";
            // Encrypt the string
            return Encrypt(text);
        }

        public static bool DecryptBool(string ivCipher)
        {
            // Decrypt the string
            string decryptedText = Decrypt(ivCipher);
            // Convert decrypted string back to boolean
            return decryptedText == "1";
        }

        // Old functions below for constant IV, not recommended
        // Need to change the same method in DICOM Broker as well

        #region OLD_FUNCTIONS
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
        #endregion
    }
}
