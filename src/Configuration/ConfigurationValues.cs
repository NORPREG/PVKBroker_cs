using Microsoft.IdentityModel.Tokens;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System;
using SecurityKey = HelseId.Samples.Common.Configuration.SecurityKey;

namespace PvkBroker.Configuration;

public static class ConfigurationValues
{
    public static string environment = "test";

    // The URL for HelseID (may be set as an environment variable)
    public static string TestStsUrl { get;  } = "https://helseid-sts.test.nhn.no";
    public static string ProdStsUrl { get; } = "https://helseid-sts.test.nhn.no";

    public static string StsUrl = environment == "test" ? TestStsUrl : ProdStsUrl;

    public static string StsPort = "443";
    public static string JwksUri = $"{StsUrl}/.well-known/openid-configuration";

    public const string ClientCredentialsResource = "connect/token";

    public const string ClientAmr = "private_key_jwt";

    public const string CachedAccessTokenFolder = "../../keys/cached/";
    public const string CachedAccessTokenFilePath = CachedAccessTokenFolder + "access_token_cache.json";

    // Audiences and scopes for using the PVK test token exchange
    public const string ApiForPvkAudience = "nhn:helsenorge.eksternapi";
    public const string ApiForPvkReadScope = $"{ApiForPvkAudience}/personverninnstilling_read";
    public const string ApiForPvkWriteScope = $"{ApiForPvkAudience}/personverninnstilling_write";

    public const string TestPvkSystemUrl = "https://eksternapi-helsenett.hn2.test.nhn.no";
    public const string ProdPvkSystemUrl = "https://eksternapi-helsenett.hn2.test.nhn.no";
    public static string PvkSystemUrl = environment == "test" ? TestPvkSystemUrl : ProdPvkSystemUrl;

    public static string PvkBaseUrl = "/personvern/Personverninnstillinger";

    // Don't put system URL here, happens later on
    public static string PvkHentInnbyggereAktivePiForDefinisjonUrl = $"{PvkBaseUrl}/HentInnbyggereAktivePiForDefinisjon/v2";
    public static string PvkHentInnbyggersPiForPartUrl = $"{PvkBaseUrl}/HentInnbyggersPiForPart/v2";
    public static string PvkSjekkInnbyggersPiStatusUrl = $"{PvkBaseUrl}/SjekkInnbyggersPiStatus/v2";

    public readonly int QuarantinePeriodInDays = 30;
    public readonly int PvkSyncTimeInHours = 24;


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

    public const string TestPvkApiClientId = "a1c7bdb1-07be-43cc-b876-95fc5c7aa180"; // This is HelseID client ID
    public const string ProdPvkApiClientId = "a1c7bdb1-07be-43cc-b876-95fc5c7aa180"; // This is HelseID client ID
    
    public const string NewPvkApiClientId = "5aee1830-29f5-4f7f-8927-a20ee9fb4125"; // where is this from?

    public const string PvkDefinisjonGuid_1 = "2c11f0ca-7270-43f1-a473-bac325feb3f6"; 
    public const string PvkDefinisjonNavn_1 = "Proton- og stråleregister samtykke"; 

    public const string PvkDefinisjonGuid_2 = "1615e446-93a9-4cdc-97b0-9e32f47c1df9";

    public static string PvkApiClientId = environment == "test" ? TestPvkApiClientId : ProdPvkApiClientId;

    public const string TestPvkPartKode = "prostraa";
    public const string ProdPvkPartKode = "prostraa";

    public static string PvkPartKode = environment == "test" ? TestPvkPartKode : ProdPvkPartKode;

    // MySQL database connection string
    public const string KodelisteServer = "localhost";
    public const string KodelisteDbName =  "kodeliste";

    // Switch to user-based login?
    public static string KodelisteUsername = Environment.GetEnvironmentVariable("KodelisteUsername"); // "cs";
    public static string KodelistePassword = Environment.GetEnvironmentVariable("KodelistePassword"); // "InitializeComponent547";

    string targetUrl = ConfigurationValues.RedcapNorpregUrl;
    string targetApiToken = ConfigurationValues.RedcapApiToken.RedcapApiToken["NORPREG"];

    public static readonly Dictionary<string, string> RedcapApiToken = new()
    {
        { "NORPREG", Environment.GetEnvironmentVariable("RedcapNorpregApiToken") },
        { "KREST-OUS", Environment.GetEnvironmentVariable("RedcapKrestOusApiToken") },
        { "KREST-HUS", Environment.GetEnvironmentVariable("RedcapKrestHusApiToken") }
    };

    public static readonly RedcapNorpregUrl = "redcap.helse-nord.no/api/";
    public static readonly RedcapKrestOusUrl = "redcap.helse-bergen.no/api/";
    public static readonly RedcapKrestHusUrl = "redcap.helse-bergen.no/api/";


    // -----------------------------------------------------------------------------------------------------------------
    // Client scopes:
    // -----------------------------------------------------------------------------------------------------------------

        // Sets both the client credential scope (for claims that the PvkApi HelseId API needs)
        // and the client info scope for use against the client info endpoint:

    public const string PvkApiScope = $"{ApiForPvkReadScope} {ApiForPvkWriteScope}"; // use this

    public static string GetPrivateKey()
    {
        // Test load PEM file
        var pem = File.ReadAllText("../../keys/test_pvk_private_key_encrypted.pem");
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
