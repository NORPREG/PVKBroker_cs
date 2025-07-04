using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using SecurityKey = HelseId.Samples.Common.Configuration.SecurityKey;

namespace PvkBroker.Configuration;

class KeyStore
{
    public static string GetAlg(RsaSecurityKey rsaKey)
    {
        return rsaKey.KeySize switch
        {
            >= 4096 => SecurityAlgorithms.RsaSha512,
            >= 3072 => SecurityAlgorithms.RsaSha384,
            _ => SecurityAlgorithms.RsaSha256
        };
    }

    public static string ConvertRsaKeyToJWKSTring(RsaSecurityKey rsaKey)
    {
        JsonWebKey jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaKey);

        string use = "sig";
        string alg = GetAlg(rsaKey);

        // The HelseID expects the JWK to be in a specific format

        string GeneralPrivateRsaKey =
            $$"""
                    { 
                    "p": "{{jwk.P}}", 
                    "kty": "{{jwk.Kty}}", 
                    "q": "{{jwk.Q}}", 
                    "d": "{{jwk.D}}", 
                    "e": "{{jwk.E}}", 
                    "use": "{{use}}", 
                    "qi": "{{jwk.QI}}", 
                    "dp": "{{jwk.DP}}", 
                    "alg": "{{alg}}", 
                    "dq": "{{jwk.DQ}}", 
                    "n": "{{jwk.N}}"               
                    }
                    """;

        return GeneralPrivateRsaKey;
    }

    public static SecurityKey GetPrivateKeyFromStore(string thumbprint)
    {
        /*
         * Private keys are initially converted from PEM to self-signed X.509 PXF using openssl
         * Then they are imported into the Windows certificate store (My / LocalMachine)
         * Remember to keep them updated! Set calendar reminder for this
         */

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var cert = store.Certificates
            .Find(X509FindType.FindByThumbprint, thumbprint, false)
            .OfType<X509Certificate2>()
            .FirstOrDefault();

        if (cert == null)
        {
            Log.Error($"Certificate with thumbprint {thumbprint} not found in store.");
            throw new Exception($"Certificate with thumbprint {thumbprint} not found in store.");
        }

        if (!cert.HasPrivateKey)
        {
            Log.Error($"Certificate with thumbprint {thumbprint} does not have a private key.");
            throw new Exception($"Certificate with thumbprint {thumbprint} does not have a private key.");
        }

        using var rsa = cert.GetRSAPrivateKey();

        if (rsa == null)
        {
            Log.Error($"Certificate with thumbprint {thumbprint} does not have a valid RSA private key.");
            throw new Exception($"Certificate with thumbprint {thumbprint} does not have a valid RSA private key.");
        }

        var rsaKey = new RsaSecurityKey(rsa);

        // return rsaKey;

        string GeneralPrivateRsaKey = ConvertRsaKeyToJWKSTring(rsaKey);
        return new(GeneralPrivateRsaKey, GetAlg(rsaKey));
    }
}