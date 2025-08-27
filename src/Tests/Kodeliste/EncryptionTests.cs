using System;
using NUnit.Framework;
using PvkBroker.Tools;

namespace PvkBroker.Tests.Encryption
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
            string encrypted = PvkBroker.Tools.Encryption.Encrypt(_x);
            string decrypted = PvkBroker.Tools.Encryption.Decrypt(encrypted);

            Assert.That(_x, Is.EqualTo(decrypted), $"Failed for input: {_x}");
        }
    }
}