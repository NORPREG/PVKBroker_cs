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
        if (_client == null)
        {
            _client = _clientConfigurator.ConfigureClient();
        }

        var cached = await TokenCacher.GetFromCache();
        if (cached != null)
        {
            Console.WriteLine("[Gyldig access token funnet, bruker denne]");
            return cached.AccessToken;
        }
        
        Console.WriteLine("[Ingen gyldig access token funnet, henter ny fra HelseID STS]");

        var httpClient = _httpClientFactory.CreateClient();
        var tokenResponse = await _client.GetAccessToken(httpClient);

        if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
        {
            return null;
        }

        string accessToken = tokenResponse.AccessToken;

        var expirationTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        await TokenCacher.SaveToCache(accessToken, expirationTime);

        return accessToken;
    }

    public async Task ValidateAccessTokenAsync(string accessToken) {

        if (string.IsNullOrEmpty(accessToken))
        {
            Log.Error("Access token is null or empty. Cannot call PVK API.");
            throw new InvalidOperationException("Access token is null or empty. Cannot call PVK API.");
        }

        TokenValidationParameters validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = ConfigurationValues.StsUrl,
            ValidateAudience = true,
            ValidAudience = ConfigurationValues.ApiForPvkAudience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };

        var signingKeys = await GetSigningKeysAsync(ConfigurationValues.JwksUri);
        validationParameters.IssuerSigningKeys = signingKeys;

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(accessToken, validationParameters, out _);

            string? userId = principal.FindFirst("sub")?.Value;
            string[] scopes = principal.FindAll("scope").Select(c => c.Value).ToArray();
            string scopeString = string.Join(",", scopes);

            ValidateScopes(scopes);
            Log.Information("Validated access token for user {UserId} with scopes {Scopes}", userId, scopeString);

        }
        catch (SecurityTokenException ex)
        {
            Log.Error("Error validating access token: {@ex}", ex);
            throw new UnauthorizedAccessException($"Token validation failed: {ex.Message}");
        }
    }

    private void ValidateScopes(IEnumerable<string> tokenScopes)
    {
        foreach (var requiredScope in ConfigurationValues.ApiForPvkAllScopes)
        {
            if (!tokenScopes.Contains(requiredScope))
            {
                Log.Error("Access token does not have the required scope: {RequiredScope}", requiredScope);
                throw new UnauthorizedAccessException($"Access token does not have the required scope: {requiredScope}");
            }
        }
    }

    private async Task<IEnumerable<MicrosoftKey>> GetSigningKeysAsync(string jwksUri)
    {
        var configManager = new Microsoft.IdentityModel.Protocols.ConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(
            jwksUri,
            new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfigurationRetriever());

        var config = await configManager.GetConfigurationAsync();
        return config.SigningKeys;
    }
}