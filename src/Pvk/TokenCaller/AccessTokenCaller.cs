using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

using IdentityModel.Client;

// FROM DLL
using HelseId.Samples.Common.JwtTokens;
using HelseId.Samples.Common.Configuration;

using PvkBroker.HelseId.ClientCredentials.Client;
using PvkBroker.HelseId.ClientCredentials.Configuration;
using PvkBroker.Configuration;

using SecurityKey = HelseId.Samples.Common.Configuration.SecurityKey;
using MicrosoftKey = Microsoft.IdentityModel.Tokens.SecurityKey;
using System;

using Serilog;


namespace PvkBroker.Pvk.TokenCaller;

public class AccessTokenCaller
{
    private ClientConfigurator _clientConfigurator;
    private Machine2MachineClient? _client;
    private readonly IHttpClientFactory _httpClientFactory;

    public AccessTokenCaller(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _clientConfigurator = new ClientConfigurator();
    }

    public async Task<string?> GetAccessToken()
    {

        // _client is the inherited HelseId client for machine-to-machine calls

        if (_client == null)
        {
            _client = _clientConfigurator.ConfigureClient();
        }

        // Check cache for existing valid token
        // Return this if it exists

        var cached = await TokenCacher.GetFromCache();
        if (cached != null)
        {
            Log.Information("[Gyldig access token funnet, bruker denne]");
            return cached.AccessToken;
        }

        // No valid token in cache, get new from HelseID STS
        // Create new HttpClient (Dependency Injection) and give it to _client

        Log.Information("[Ingen gyldig access token funnet, henter ny fra HelseID STS]");
        var httpClient = _httpClientFactory.CreateClient();
        var tokenResponse = await _client.GetAccessToken(httpClient);

        // Should not happen
        if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
        {
            return null;
        }

        // Cache token to disk (encrypted) with expiration time
        var expirationTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        await TokenCacher.SaveToCache(tokenResponse.AccessToken, expirationTime);

        return tokenResponse.AccessToken;
    }
}