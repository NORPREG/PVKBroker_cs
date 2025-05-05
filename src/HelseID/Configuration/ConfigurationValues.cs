using Microsoft.IdentityModel.Tokens;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SecurityKey = HelseId.Samples.Common.Configuration.SecurityKey;

namespace HelseId.Configuration;

public static class ConfigurationValues
{
     public static string environment = "test";

     // The URL for HelseID (may be set as an environment variable)
     public static string TestStsUrl { get;  } = "https://helseid-sts.test.nhn.no";
     public static string ProdStsUrl { get; } = "https://helseid-sts.test.nhn.no";

     public static string StsUrl = environment == "test" ? TestStsUrl : ProdStsUrl;

     public static string StsPort = "443";
     public const string JwksUri = $"{StsUrl}/.well-known/openid-configuration";

     public const string ClientCredentialsResource = "connect/token";

     public const string ClientAmr = "private_key_jwt"

     // Audiences and scopes for using the PVK test token exchange
     public const string ApiForPvkAudience = "nhn:helsenorge.eksternapi";
     public const string ApiForPvkReadScope = $"{ApiForPvkAudience}/personverninnstilling_read";
     public const string ApiForPvkWriteScope = $"{ApiForPvkAudience}/personverninnstilling_write";

     public const string TestPvkBaseUrl = "https://eksternapi-helsenett.hn2.test.nhn.no";
     public const string ProdPvkBaseUrl = "https://eksternapi-helsenett.hn2.test.nhn.no";

     public static string PvkBaseUrl = environment == "test" ? TestPvkBaseUrl : ProdPvkBaseUrl;

     public const string TestPvkHentInnbyggereUrl = $"{PvkBaseUrl}/HentInnbyggereAktivePiForDefinisjon/v2";

     public const bool ClientCredentialsUseDpop = false;

     // HelseID scopes defined at https://helseid.atlassian.net/wiki/spaces/HELSEID/pages/5603417/Scopes
     private const string GeneralHelseIdScopes = "helseid://scopes/identity/pid helseid://scopes/identity/pid_pseudonym helseid://scopes/identity/assurance_level helseid://scopes/hpr/hpr_number helseid://scopes/identity/network helseid://scopes/identity/security_level";

     public static readonly string PvkApiUrlForM2M = $"{StsUrl}:{StsPort}/{ClientCredentialsResource}";

     // If used, the child organization number (underenhet) must match a number in the client's whitelist as it's held by HelseID:
     public const string OUSOrganizationNumber = "993467049";
     public const string OUSOrganizationName = "Oslo universitetssykehus";

     // ----------------------------------------------------------------------------------------------------------------------
     // These private key JWKs match the public keys in HelseID that are attached to the corresponding client configurations:
     // ----------------------------------------------------------------------------------------------------------------------
     // In a production scenario, a private key MUST be secured inside the client, and NOT be set in source code.
     // In this (test) case, the clients also share a private key.
     // In a production environment, all clients must have separate keys.
     // ----------------------------------------------------------------------------------------------------------------------

     private static readonly string GeneralPrivateRsaKey = GetPrivateKey();
     public static readonly SecurityKey PvkRsaKey = new(GeneralPrivateRsaKey, SecurityAlgorithms.RsaSha256);

     // -----------------------------------------------------------------------------------------------------------------
     // Client IDs:
     // -----------------------------------------------------------------------------------------------------------------
     // In HelseID, these are normally set as GUIDS, here we have named them for better readability
     // -----------------------------------------------------------------------------------------------------------------

     public const string TestPvkApiClientId = "a1c7bdb1-07be-43cc-b876-95fc5c7aa180"; // use this
     public const string ProdPvkApiClientId = "a1c7bdb1-07be-43cc-b876-95fc5c7aa180"; // use this
     public const string PvkApiClientId = environment == "test" ? TestPvkApiClientId : ProdPvkApiClientId;

     public const string TestPvkPartKode = "prostraa";
     public const string ProdPvkPartKode = "prostraa";
     public const string PvkPartKode = environment == "test" ? TestPvkPartKode : ProdPvkPartKode;

     // -----------------------------------------------------------------------------------------------------------------
     // Client scopes:
     // -----------------------------------------------------------------------------------------------------------------

     // Sets both the client credential scope (for claims that the PvkApi HelseId API needs)
     // and the client info scope for use against the client info endpoint:

     public const string PvkApiScope = $"{ApiForPvkReadScope} {ApiForPvkWriteScope}"; // use this

     public static string GetPrivateKey()
     {
          // Test load PEM file
          var pem = File.ReadAllText("keys/test_pvk_private_key_encrypted.pem");
          var rsa = RSA.Create();
          rsa.ImportFromEncryptedPem(pem.ToCharArray(), "test_password");

          var rsaKey = new RsaSecurityKey(rsa);
          JsonWebKey jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaKey);

          string alg = "RS256"; // OK with openssl inspection
          string use = "sig"; // check this

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
}
