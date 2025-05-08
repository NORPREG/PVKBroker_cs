using HelseId.Samples.Common.Interfaces.PayloadClaimsCreators;
using HelseId.Samples.Common.Models;

using IdentityModel.Client;

namespace PvkBroker.HelseId.CommonExtended.Interfaces.TokenRequests;

public interface ITokenRequestBuilder : ITokenRequestBuilder
{
    Task<ClientCredentialsTokenRequest> CreateClientCredentialsBearerTokenRequest(
        IPayloadClaimsCreator payloadClaimsCreator,
        ClientCredentialsTokenRequestParameters tokenRequestParameters);

    Task<ClientCredentialsTokenRequest> CreateClientCredentialsTokenRequest(
        IPayloadClaimsCreator payloadClaimsCreator,
        ClientCredentialsTokenRequestParameters tokenRequestParameters,
        string? dPoPNonce);

    Task<RefreshTokenRequest> CreateRefreshTokenRequest(
        IPayloadClaimsCreator payloadClaimsCreator,
        RefreshTokenRequestParameters tokenRequestParameters,
        string? dPoPNonce);

    Task<TokenExchangeTokenRequest> CreateTokenExchangeTokenRequest(
        IPayloadClaimsCreator payloadClaimsCreator,
        TokenExchangeTokenRequestParameters tokenRequestParameters,
        string? dPoPNonce);

    Task<AuthorizationCodeTokenRequest> CreateAuthorizationCodeTokenRequest(
        IPayloadClaimsCreator payloadClaimsCreator,
        AuthorizationCodeTokenRequestParameters tokenRequestParameters,
        string? dPoPNonce);
}