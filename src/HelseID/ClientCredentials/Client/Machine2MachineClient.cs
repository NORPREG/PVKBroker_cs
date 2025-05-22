using IdentityModel.Client;
using System.Text;

// FROM DLL
using HelseId.Samples.Common.Interfaces.PayloadClaimsCreators;
using HelseId.Samples.Common.Interfaces.TokenExpiration;
using HelseId.Samples.Common.Interfaces.TokenRequests;
using HelseId.Samples.Common.Models;

using Serilog;
using PvkBroker.Configuration;
using System.Reflection.Metadata.Ecma335;

namespace PvkBroker.HelseId.ClientCredentials.Client;

public class Machine2MachineClient
{
    private ITokenRequestBuilder _tokenRequestBuilder;
    // private IApiConsumer _apiConsumer;
    private ClientCredentialsTokenRequestParameters _tokenRequestParameters;
    private IExpirationTimeCalculator _expirationTimeCalculator;
    private DateTime _persistedAccessTokenExpiresAt = DateTime.MinValue; // cache this to disk
    private string _persistedAccessToken = string.Empty; // cache this to disk
    private ClientCredentialsTokenRequest? request;
    private readonly IPayloadClaimsCreatorForClientAssertion _payloadClaimsCreatorForClientAssertion;

    public Machine2MachineClient(
        // IApiConsumer apiConsumer,
        ITokenRequestBuilder tokenRequestBuilder,
        IExpirationTimeCalculator expirationTimeCalculator,
        IPayloadClaimsCreatorForClientAssertion payloadClaimsCreatorForClientAssertion,
        ClientCredentialsTokenRequestParameters tokenRequestParameters)
    {
        _tokenRequestBuilder = tokenRequestBuilder;
        _expirationTimeCalculator = expirationTimeCalculator;
        _payloadClaimsCreatorForClientAssertion = payloadClaimsCreatorForClientAssertion;
        _tokenRequestParameters = tokenRequestParameters;
        request = null;
    }

    public string GetAccessToken()
    {
        return _persistedAccessToken;
    }

    public async Task<string> GetAccessToken(HttpClient httpClient)
    {
        // Token caching is file based and happens in Pvk/TokenCaller/AccesTokenCaller.cs

        var tokenResponse = await GetAccessTokenFromHelseId(httpClient);
        string accessToken = tokenResponse.AccessToken!;

        return accessToken;
    }

    private async Task<TokenResponse> GetAccessTokenFromHelseId(HttpClient httpClient)
    {
        // We use the HTTP client to retrieve the response from HelseID:
        var tokenResponse = await RequestClientCredentialsTokenAsync(httpClient);

        if (tokenResponse.IsError || tokenResponse.AccessToken == null)
        {
            Log.Error("Error in Access Token response from HelseID: {@tokenResponse}.", tokenResponse);
            throw new Exception();
        }
        else
        {
            Log.Information("Successfully received Access Token from HelseID.");
        }

        // WriteAccessTokenFromTokenResult(tokenResponse);
        return tokenResponse;
    }

    private async Task<TokenResponse> RequestClientCredentialsTokenAsync(HttpClient httpClient)
    {
        // The request to HelseID is created:

        request = await _tokenRequestBuilder.CreateClientCredentialsTokenRequest(
            _payloadClaimsCreatorForClientAssertion,
            _tokenRequestParameters,
            null);
        
        // InspectRequest(request);

        var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(request);

        if (tokenResponse.IsError && tokenResponse.Error == "use_dpop_nonce" && !string.IsNullOrEmpty(tokenResponse.DPoPNonce))
        {
            request = await _tokenRequestBuilder.CreateClientCredentialsTokenRequest(_payloadClaimsCreatorForClientAssertion, _tokenRequestParameters, tokenResponse.DPoPNonce);
            tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(request);
        }
        return tokenResponse;
    }

    private void InspectRequest(ClientCredentialsTokenRequest request)
    {
        Console.WriteLine("Inspecting ClientCredentialsTokenRequest:");
        Console.WriteLine($"Address: {request.Address}");
        Console.WriteLine($"ClientId: {request.ClientId}");
        Console.WriteLine($"Authorization: {request.Headers.Authorization}");
        Console.WriteLine($"Scope: {request.Scope}");
        Console.WriteLine($"GrantType: {request.GrantType}");
        Console.WriteLine($"DPoP: {request.DPoPProofToken}");
        Console.WriteLine($"ClientAssertion: {request.ClientAssertion?.Value}");
        Console.WriteLine($"ClientAssertionType: {request.ClientAssertion?.Type}");
        Console.WriteLine($"Parameters: {string.Join(", ", request.Parameters.Select(p => $"{p.Key}: {p.Value}"))}");
        Console.WriteLine("------");
    }

}
