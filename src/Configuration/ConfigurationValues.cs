using Microsoft.IdentityModel.Tokens;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System;
using SecurityKey = HelseId.Samples.Common.Configuration.SecurityKey;

namespace PvkBroker.Configuration;

public static class ConfigurationValues
{
    public static string environment = "test-inet";

    // The URL for HelseID
    public static string TestStsUrl { get;  } = "https://helseid-sts.test.nhn.no";
    public static string ProdStsUrl { get; } = "https://helseid-sts.nhn.no";
    public static string TestInetStsUrl { get; } = "https://helseid-sts.test.nhn.no";

    public static string StsUrl = environment switch
    {
        "test" => TestStsUrl,
        "test-inet" => TestInetStsUrl,
        "prod" => ProdStsUrl,
        _ => throw new ArgumentException("Invalid environment specified")
    };

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

    public const string PvkApiScope = $"{ApiForPvkReadScope} {ApiForPvkWriteScope}"; // use this

    public const string TestPvkSystemUrl = "https://eksternapi-helsenett.hn2.test.nhn.no";
    public const string TestInetPvkSystemUrl = "https://eksternapi.hn2.test.nhn.no";
    public const string ProdPvkSystemUrl = "https://eksternapi-helsenett.helsenorge.no";
    
    public static string PvkSystemUrl = environment switch
    {
        "test" => TestPvkSystemUrl,
        "test-inet" => TestInetPvkSystemUrl,
        "prod" => ProdPvkSystemUrl,
        _ => throw new ArgumentException("Invalid environment specified")
    };

    public static string PvkBaseUrl = "/personvern/Personverninnstillinger";

    // Don't put system URL here, happens later on
    public static string PvkHentInnbyggereAktivePiForDefinisjonUrl = $"{PvkBaseUrl}/HentInnbyggereAktivePiForDefinisjon/v2";
    public static string PvkHentInnbyggersPiForPartUrl = $"{PvkBaseUrl}/HentInnbyggersPiForPart/v2";
    public static string PvkSjekkInnbyggersPiStatusUrl = $"{PvkBaseUrl}/SjekkInnbyggersPiStatus/v2";
    public static string PvkSettInnbyggersPersonvernInnstillingUrl = $"{PvkBaseUrl}/SettInnbyggersPersonvernInnstilling/v2";

    public static int QuarantinePeriodInDays = 30;
    public static int PvkSyncTimeInHours = 24;


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
    public const string ProdPvkApiClientId = ""; // This is HelseID client ID
    
    public const string PvkDefinisjonGuid_1 = "56a8756c-49f7-4cb9-bfc0-ba282baf0f83"; 
    public const string PvkDefinisjonNavn_1 = "Reservasjon mot oppføring i NORPREG";

    public const string PvkTypePi = "reservasjon"; // reservasjon, samtykke, tilgangsbegrensning

    // Same for test and test-inet
    public static string PvkApiClientId = environment == "prod" ? ProdPvkApiClientId : TestPvkApiClientId;

    public const string TestPvkPartKode = "norpreg";
    public const string ProdPvkPartKode = "norpreg";

    // Same for test and test-inet
    public static string PvkPartKode = environment == "prod" ? ProdPvkPartKode : TestPvkPartKode;

    // MySQL database connection string
    public const string KodelisteServer = "localhost";
    public const string KodelisteDbName =  "kodeliste";

    // Switch to user-based login?
    public static string KodelisteUsername = Environment.GetEnvironmentVariable("KodelisteUsername"); // "cs";
    public static string KodelistePassword = Environment.GetEnvironmentVariable("KodelistePassword"); // "InitializeComponent547";
    public static string KodelisteAesKey = "AesKeyForTest"; // Environment.GetEnvironmentVariable("KodelisteAesKey");

    // TODO: add correct URLs when REDCap setup is complete
    public static string RedcapNorpregUrl = "redcap.helse-nord.no/api/";
    public static string RedcapKrestUrl = "redcap.helse-bergen.no/api/";

    public static readonly Dictionary<string, string> RedcapApiToken = new()
    {
        { "NORPREG", Environment.GetEnvironmentVariable("RedcapNorpregApiToken") },
        { "KREST-OUS", Environment.GetEnvironmentVariable("RedcapKrestOusApiToken") },
        { "KREST-HUS", Environment.GetEnvironmentVariable("RedcapKrestHusApiToken") }
    };

    public static string TargetUrl = ConfigurationValues.RedcapNorpregUrl;
    public static string TargetApiToken = ConfigurationValues.RedcapApiToken["NORPREG"];

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
