using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
    public const bool PvkUseBearerToken = true; // Use bearer token for API calls until DPoP is implemented

    public const string CachedAccessTokenFolder = "../../keys/cached/";
    public const string CachedAccessTokenFilePath = CachedAccessTokenFolder + "access_token_cache.json";

    // Audiences and scopes for using the PVK test token exchange
    public const string ApiForPvkAudience = "nhn:helsenorge.eksternapi";
    public const string ApiForPvkReadScope = $"{ApiForPvkAudience}/personverninnstilling_read";
    public const string ApiForPvkWriteScope = $"{ApiForPvkAudience}/personverninnstilling_write";

    public static string[] ApiForPvkAllScopes = new[] 
    { 
        ApiForPvkReadScope, 
        ApiForPvkWriteScope 
    };

    public const string PvkApiScope = $"{ApiForPvkReadScope} {ApiForPvkWriteScope}"; // use this

    public const string TestPvkSystemUrl = "https://eksternapi-helsenett.hn2.test.nhn.no";
    public const string TestInetPvkSystemUrl = "https://eksternapi.hn2.test.nhn.no";
    public const string ProdPvkSystemUrl = "https://eksternapi-helsenett.helsenorge.no";

    // For Haukeland box we need SQLite as DB provider
    public const bool UseSqlite = true; // Set to true if using Haukeland box, otherwise false
    public const string SqliteDatabaseFile = "../../Db/dev_kodeliste.db"; // Path to the SQLite database file

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

    private static readonly string HelseIdKeyThumbprint = "33D367292D83D446E1AF181B64CF9FBF912075E7";
    public static readonly SecurityKey PvkRsaKey = KeyStore.GetPrivateKeyFromStore(HelseIdKeyThumbprint);

    // -----------------------------------------------------------------------------------------------------------------
    // Client IDs:
    // -----------------------------------------------------------------------------------------------------------------

    public const string TestPvkApiClientId = "a1c7bdb1-07be-43cc-b876-95fc5c7aa180"; // Defined in current documentation
    public const string ProdPvkApiClientId = "a1c7bdb1-07be-43cc-b876-95fc5c7aa180"; // Defined in current documentation
    public static string PvkApiClientId = environment == "prod" ? ProdPvkApiClientId : TestPvkApiClientId;

    public const string PvkDefinisjonGuid_1 = "56a8756c-49f7-4cb9-bfc0-ba282baf0f83"; // Defined in current documentation
    public const string PvkDefinisjonNavn_1 = "Reservasjon mot oppføring i NORPREG"; // Defined in current documentation

    public const string PvkTypePi = "reservasjon"; // Defined in current documentation

    public const string TestPvkPartKode = "norpreg"; // Defined in current documentation
    public const string ProdPvkPartKode = "norpreg"; // Defined in current documentation
    public static string PvkPartKode = environment == "prod" ? ProdPvkPartKode : TestPvkPartKode;

    // MySQL database connection string
    public const string KodelisteServer = "localhost";
    public const string KodelisteDbName =  "kodeliste";
    public static string? KodelisteUsername = "cs"; //  Environment.GetEnvironmentVariable("KodelisteUsername"); // "cs";
    public static string? KodelistePassword = "InitializeComponent547"; // Environment.GetEnvironmentVariable("KodelistePassword"); // "InitializeComponent547";
    public static string? KodelisteAesKey = "dacp1fOy5pFjaOYY1xirQSeONMJnRs8H"; // Environment.GetEnvironmentVariable("KodelisteAesKey");

    public static string RedcapNorpregUrl = "";
    public static string RedcapKrestUrl = "";

    public static readonly Dictionary<string, string?> RedcapApiToken = new()
    {
        { "NORPREG", Environment.GetEnvironmentVariable("RedcapNorpregApiToken") },
        { "KREST-OUS", Environment.GetEnvironmentVariable("RedcapKrestOusApiToken") },
        { "KREST-HUS", Environment.GetEnvironmentVariable("RedcapKrestHusApiToken") }
    };

    public static string TargetUrl = RedcapNorpregUrl;
    public static string? TargetApiToken = RedcapApiToken["NORPREG"];
}
