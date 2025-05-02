using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using HelseId.Common.JwtTokens;
using HelseId.Common.TokenRequestBuilder;
using HelseId.Common.Configuration;
using HelseId.ClientCredentials.Client;
using HelseId.ClientCredentials.Configuration;

namespace PvkBroker.ApiCaller;

public class AccessTokenCaller
{

    private var _clientConfigurator = new ClientConfigurator();
    private var _client = clientConfigurator.ConfigureClient();

    public AccessTokenCaller() {}

    public async Task<string> GetAccessToken()
    {
        using var httpClient = new HttpClient();
        string accessToken = await _client.GetAccessToken(httpClient);

        return accessToken;
    }

    public async Task<ClaimsPrincipal> ValidateAccessTokenAsync(string accessToken) {

        TokenValidationParameters validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _clientConfigurator.configuration.StsUrl,
            ValidateAudience = true,
            ValidAudience = _clientConfigurator.configuration.ApiForPvkAudience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        }

        var signingKeys = await GetSigningKeysAsync(_clientConfigurator.configuration.JwksUri);
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

    private async Task<IEnumerable<SecurityKey>> GetSigningKeysAsync(string jwksUri)
    {
        var configManager = new Microsoft.IdentityModel.Protocols.ConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(
            jwksUri,
            new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfigurationRetriever());

        var config = await configManager.GetConfigurationAsync();
        return config.SigningKeys;
    }
}