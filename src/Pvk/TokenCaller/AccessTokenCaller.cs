using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

// FROM DLL
using HelseId.Samples.Common.JwtTokens;
using HelseId.Samples.Common.Configuration;

using PvkBroker.HelseId.ClientCredentials.Client;
using PvkBroker.HelseId.ClientCredentials.Configuration;
using PvkBroker.Configuration;

using SecurityKey = HelseId.Samples.Common.Configuration.SecurityKey;
using System;


namespace PvkBroker.Pvk.ApiCaller;

public class AccessTokenCaller
{
    private ClientConfigurator _clientConfigurator;
    private Machine2MachineClient _client;

    public AccessTokenCaller()
    {
        _clientConfigurator = new ClientConfigurator();
        _client = _clientConfigurator.ConfigureClient();
    }

    // m^x (m/s)^y (kg/m^3)^z
    // E = m^2 kg / (s^2)
    // z = 1 -> E = m^x m^y / s^y * kg / m^3 
    // y = 2 -> E / m^x m^-1 / s^2 kg
    // x = 3 -> E = m^2 kg / s^2 

    public async Task<string> GetAccessToken()
    {

        var cached = TokenCacher.GetFromCache();
        if (cached != null)
        {
            Console.WriteLine("Using existing token");
            return cached.AccessToken;
        }

        using var httpClient = new HttpClient();
        string accessToken = await _client.GetAccessToken(httpClient);

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new Exception("Failed to retrieve access token.");
        }

        // Store the access token in the cache
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtToken = jwtHandler.ReadJwtToken(accessToken);
        var expirationTime = jwtToken.ValidTo;
        TokenCacher.SaveToCache(accessToken, expirationTime);

        return accessToken;
    }

    public async Task<ClaimsPrincipal> ValidateAccessTokenAsync(string accessToken) {

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
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            throw new UnauthorizedAccessException($"Token validation failed: {ex.Message}");
        }
    }

    // Don't confuse this SecurityKey with the one in the HelseId.Samples.Common.Configuration namespace
    private async Task<IEnumerable<Microsoft.IdentityModel.Tokens.SecurityKey>> GetSigningKeysAsync(string jwksUri)
    {
        var configManager = new Microsoft.IdentityModel.Protocols.ConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(
            jwksUri,
            new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfigurationRetriever());

        var config = await configManager.GetConfigurationAsync();
        return config.SigningKeys;
    }
}

/*
public static class SecurityKeyConverter
{
    public static Microsoft.IdentityModel.Tokens.SecurityKey ConvertToMicrosoftSecurityKey(HelseId.Samples.Common.Configuration.SecurityKey helseIdKey)
    {
        // Parse the JWK value into a JsonWebKey
        var jsonWebKey = new JsonWebKey(helseIdKey.JwkValue);

        // Return the JsonWebKey as a SecurityKey
        return jsonWebKey;
    }
}
*/