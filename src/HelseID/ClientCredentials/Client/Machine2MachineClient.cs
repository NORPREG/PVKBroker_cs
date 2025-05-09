using IdentityModel.Client;
using System.Text;

// FROM DLL
using HelseId.Samples.Common.Interfaces.PayloadClaimsCreators;
using HelseId.Samples.Common.Interfaces.TokenExpiration;
// using HelseId.Samples.Common.Interfaces.TokenRequests;
using HelseId.Samples.Common.Models;

using PvkBroker.HelseId.CommonExtended.Interfaces.TokenRequests;


using PvkBroker.Configuration;

namespace PvkBroker.HelseId.ClientCredentials.Client;

public class Machine2MachineClient
{
    private ITokenRequestBuilder _tokenRequestBuilder;
    // private IApiConsumer _apiConsumer;
    private ClientCredentialsTokenRequestParameters _tokenRequestParameters;
    private bool _clientCredentialsUseDpop;
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
        ClientCredentialsTokenRequestParameters tokenRequestParameters,
        bool clientCredentialsUseDpop)
    {
        _tokenRequestBuilder = tokenRequestBuilder;
        _expirationTimeCalculator = expirationTimeCalculator;
        _payloadClaimsCreatorForClientAssertion = payloadClaimsCreatorForClientAssertion;
        _tokenRequestParameters = tokenRequestParameters;
        _clientCredentialsUseDpop = clientCredentialsUseDpop;
        request = null;
    }

    public string GetAccessToken()
    {
        return _persistedAccessToken;
    }

    public async Task<string> GetAccessToken(HttpClient httpClient)
    {
        if (DateTime.UtcNow > _persistedAccessTokenExpiresAt)
        {
            var tokenResponse = await GetAccessTokenFromHelseId(httpClient);
            _persistedAccessTokenExpiresAt = _expirationTimeCalculator.CalculateTokenExpirationTimeUtc(tokenResponse.ExpiresIn);
            _persistedAccessToken = tokenResponse.AccessToken!;
        }
        else
        {
            // Push to logger
            Console.WriteLine("The access token has not yet expired, no call was made to HelseID for a new token.");
        }
        return _persistedAccessToken;
    }

    private async Task<TokenResponse> GetAccessTokenFromHelseId(HttpClient httpClient)
    {
        // We use the HTTP client to retrieve the response from HelseID:
        var tokenResponse = await RequestClientCredentialsTokenAsync(httpClient);

        if (tokenResponse.IsError || tokenResponse.AccessToken == null)
        {
            await WriteErrorToConsole(tokenResponse);
            throw new Exception();
        }

        WriteAccessTokenFromTokenResult(tokenResponse);

        return tokenResponse;
    }

    private async Task<TokenResponse> RequestClientCredentialsTokenAsync(HttpClient httpClient)
    {
        // The request to HelseID is created:

        if (_clientCredentialsUseDpop) {
            // Original function from HelseID DLL library
            request = await _tokenRequestBuilder.CreateClientCredentialsTokenRequest(
                _payloadClaimsCreatorForClientAssertion,
                _tokenRequestParameters,
                null);
        }
        else {
            // Extended function to create the request with Bearer token and not DPoP
            request = await _tokenRequestBuilder.CreateClientCredentialsBearerTokenRequest(
                _payloadClaimsCreatorForClientAssertion, 
                _tokenRequestParameters);
        }
        
        InspectRequest(request);

        var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(request);

        if (_clientCredentialsUseDpop && tokenResponse.IsError && tokenResponse.Error == "use_dpop_nonce" && !string.IsNullOrEmpty(tokenResponse.DPoPNonce))
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

    private static async Task WriteErrorToConsole(TokenResponse tokenResponse) {
        await Console.Error.WriteLineAsync("An error occured:");
        await Console.Error.WriteLineAsync(tokenResponse.Error);
    }

    private static void WriteAccessTokenFromTokenResult(TokenResponse tokenResponse) {
        Console.WriteLine("The application received this access token from HelseID:");
        Console.WriteLine(tokenResponse.AccessToken);
        Console.WriteLine("Copy/paste the access token string at https://jwt.ms to see the contents");
        Console.WriteLine("------");
    }

    /*
    private async Task CallApi(HttpClient httpClient, string accessToken)
    {
        try
        {
            Console.WriteLine("Using the access token to call the sample API");
            ApiResponse? response;
            response = await _apiConsumer.CallApiWithDPoPToken(httpClient, ConfigurationValues.PvkApiUrlForM2M, accessToken);
            var notPresent = "<not present>";
            var supplierOrganization = OrganizationStore.GetOrganization(response?.SupplierOrganizationNumber);
            var parentOrganization = OrganizationStore.GetOrganization(response?.ParentOrganizationNumber);
            var childOrganization = OrganizationStore.GetOrganizationWithChild(response?.ChildOrganizationNumber);
            Console.WriteLine($"Response from the sample API:");
            Console.WriteLine($"{response?.Greeting}");
            Console.WriteLine($"Supplier organization number (for multitenancy): '{supplierOrganization?.ParentName ?? notPresent}'");
            Console.WriteLine($"Parent organization number: '{parentOrganization?.ParentName ?? notPresent}'");
            Console.WriteLine($"Child organization number: '{childOrganization?.ChildName ?? notPresent}'");
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"The sample API did not accept the request at address '{ConfigurationValues.PvkApiUrlForM2M}'. (Have you started the sample API application (in the 'SampleAPI' folder)?");
            Console.WriteLine($"Error message: '{e.Message}'");
        }
    }
    */
}
