using IdentityModel.Client;
using System.Text;
using Serilog;

// FROM DLL
using HelseId.Samples.Common.Interfaces.PayloadClaimsCreators;
using HelseId.Samples.Common.Interfaces.TokenRequests;
using HelseId.Samples.Common.Models;

namespace PvkBroker.HelseId.ClientCredentials.Client;

public class Machine2MachineClient
{
    private ITokenRequestBuilder _tokenRequestBuilder;
    private ClientCredentialsTokenRequestParameters _tokenRequestParameters;
    private ClientCredentialsTokenRequest? request;
    private readonly IPayloadClaimsCreatorForClientAssertion _payloadClaimsCreatorForClientAssertion;

    public Machine2MachineClient(
        ITokenRequestBuilder tokenRequestBuilder,
        IPayloadClaimsCreatorForClientAssertion payloadClaimsCreatorForClientAssertion,
        ClientCredentialsTokenRequestParameters tokenRequestParameters)
    {
        _tokenRequestBuilder = tokenRequestBuilder;
        _payloadClaimsCreatorForClientAssertion = payloadClaimsCreatorForClientAssertion;
        _tokenRequestParameters = tokenRequestParameters;
        request = null;
    }

    public async Task<TokenResponse> GetAccessToken(HttpClient httpClient)
    {
        // Token caching is file based and happens in Pvk/TokenCaller/AccesTokenCaller.cs
        var tokenResponse = await GetAccessTokenFromHelseId(httpClient);

        return tokenResponse;
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

        var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(request);

        if (tokenResponse.IsError && tokenResponse.Error == "use_dpop_nonce" && !string.IsNullOrEmpty(tokenResponse.DPoPNonce))
        {
            request = await _tokenRequestBuilder.CreateClientCredentialsTokenRequest(
                _payloadClaimsCreatorForClientAssertion,
                _tokenRequestParameters,
                tokenResponse.DPoPNonce);

            tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(request);
        }
        return tokenResponse;
    }
}
