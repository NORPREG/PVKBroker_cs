using PvkBroker.Configuration;

namespace PvkBroker.Pvk.ApiCaller;

public class CachedToken
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class TokenCacher
{
    private const string CachedAccessTokenFilePath = ConfigurationValues.CachedAccessTokenFilePath;
    private const string CachedAccessTokenFolder = ConfigurationValues.CachedAccessTokenFolder;

    public static CachedToken? GetFromCache()
    {
        Directory.CreateDirectory(CachedAccessTokenFolder);

        if (!File.Exists(CachedAccessTokenFilePath))
            return null;

        try
        {
            var json = File.ReadAllText(CachedAccessTokenFilePath);
            var cachedToken = JsonSerializer.Deserialize<CachedToken>(json);
            if (cachedToken != null && cachedToken.ExpiresAt > DateTime.UtcNow)
            {
                return cachedToken;
            }
        }
        catch (JsonException ex)
        {
            // Handle JSON deserialization error
            Console.WriteLine($"Error deserializing cached token: {ex.Message}. Returning null.");
            return null;
        }
        catch (IOException)
        {
            // Handle file read error
            Console.WriteLine("Error reading cached token file. Returning null.");
            return null; 
        }
        catch (Exception ex)
        {
            // Handle any other exceptions
            Console.WriteLine($"Unexpected error: {ex.Message}. Returning null.");
            return null;
        }

        return null;
    }

    public static void SaveToCache(string accessToken, DateTime expiresAt)
    {
        var cachedToken = new CachedToken
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt
        };
        
        var json = JsonSerializer.Serialize(cachedToken);
        File.WriteAllText(CachedAccessTokenFilePath, json);
    }
}