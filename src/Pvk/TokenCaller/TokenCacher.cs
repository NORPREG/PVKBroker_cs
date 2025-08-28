using PvkBroker.Configuration;
using PvkBroker.Tools;

using Serilog;

namespace PvkBroker.Pvk.TokenCaller;

public class CachedToken
{
    public string? AccessToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class TokenCacher
{
    private const string CachedAccessTokenFilePath = ConfigurationValues.CachedAccessTokenFilePath;
    private const string CachedAccessTokenFolder = ConfigurationValues.CachedAccessTokenFolder;

    public static async Task<CachedToken?> GetFromCache()
    {
        Directory.CreateDirectory(CachedAccessTokenFolder);


        // If no token file exists, return null
        if (!File.Exists(CachedAccessTokenFilePath))
        {
            return null;
        }


        // Try to read and deserialize the cached encrypted token
        // Return token if OK, otherwise return null

        try
        {
            var json = await File.ReadAllTextAsync(CachedAccessTokenFilePath);
            var cachedToken = JsonSerializer.Deserialize<CachedToken>(json);
            if (cachedToken != null && cachedToken.ExpiresAt > DateTime.UtcNow && cachedToken.AccessToken != null)
            {
                Log.Information("Found cached Access Token that is still valid, using this.");
                cachedToken.AccessToken = Encryption.Decrypt(cachedToken.AccessToken);
                return cachedToken;
            }
        }
        catch (JsonException ex)
        {
            // Handle JSON deserialization error
            Log.Error("Error deserializing cached token: {@ex}. Returning null.", ex);
        }
        catch (IOException)
        {
            // Handle file read error
            Log.Error("Error reading cached token file. Returning null.");
        }
        catch (Exception ex)
        {
            // Handle any other exceptions
            Log.Error($"Unexpected error in reading cached token: {@ex}. Returning null.", ex);
        }

        return null;
    }

    public static async Task SaveToCache(string accessToken, DateTime expiresAt)
    {

        // Make token object with encrypted access token string
        // !! ExpiresAt directly from response, not from token parsing

        var cachedToken = new CachedToken
        {
            AccessToken = Encryption.Encrypt(accessToken),
            ExpiresAt = expiresAt
        };

        // Serialize and save to file
        var json = JsonSerializer.Serialize(cachedToken);
        await File.WriteAllTextAsync(CachedAccessTokenFilePath, json);
    }
}