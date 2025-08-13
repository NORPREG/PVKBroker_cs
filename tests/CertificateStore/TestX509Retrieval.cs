using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;

namespace PvkBroker.Tests.CertificateStore
{
    [TestFixture]
    public class CertificateStoreTests
    {

        private const string thumbprint = "33D367292D83D446E1AF181B64CF9FBF912075E7";

        [Test]
        public void Should_Load_Keys_From_Store()
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var cert = store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, false)
                .OfType<X509Certificate2>()
                .FirstOrDefault();

            Assert.That(cert, Is.Not.Null, "Certificate not found in store");

            Assert.That(cert?.HasPrivateKey, Is.True, "Certificate does not have a private key");

            Console.WriteLine($"Certificate found: {cert.Subject} with thumbprint {cert.Thumbprint}");
        }
    }
}