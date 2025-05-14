using System;
using NUnit.Framework;
using PvkBroker.Kodeliste;

namespace PVKBroker.Tests.Encryption
{
    [TestFixture("Hello, World")]
    [TestFixture("1234567890")]
    [TestFixture("Special characters: !@#$%^&*()_+")]
    [TestFixture("Multiline\nString\nTest")]
    [TestFixture("")]
    public class EncryptionTests
    {

        private readonly string _x;

        public EncryptionTests(string x)
        {
            _x = x;
        }

        [Test]
        public void EncryptAndDecrypt_ReturnsOriginalString()
        {
            string key = "TestEncryptionKey";

            string encrypted = PvkBroker.Kodeliste.Encryption.EncryptWithHashKey(_x, key);
            string decrypted = PvkBroker.Kodeliste.Encryption.DecryptWithHashKey(encrypted, key);

            Assert.That(_x, Is.EqualTo(decrypted), $"Failed for input: {_x}");
        }

        [Test]
        public void EncryptAndDecryptWrongKey_ReturnsDifferentString()
        {
            string key = "TestEncryptionKey";
            string wrong_key = "TestEncryptionKey56";

            string encrypted = PvkBroker.Kodeliste.Encryption.EncryptWithHashKey(_x, key);

            try
            {
                // Might be correct PKCS7 padding by coincidence, then the decryption gives wrong result without failing
                string decrypted = PvkBroker.Kodeliste.Encryption.DecryptWithHashKey(encrypted, wrong_key);
                Assert.That(_x, Is.Not.EqualTo(decrypted), $"Decrypted string should not match original for input: {_x}");
            }
            catch (Exception ex)
            {
                // Likely case, the padding (last byte) is random and not [0, 16]
                Assert.That(ex.Message, Does.Contain("Invalid padding"));
            }
        }
    }


}