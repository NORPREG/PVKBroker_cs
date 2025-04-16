using System.Net.Http.Headers;
using System.Text.Json;
using HelseId.Common.JwtTokens;
using HelseId.Common.Configuration;
using HelseId.ClientCredentials.Client;

public class AccessToken
{
    private readonly JwtClaimsCreator _jwtClaimsCreator;

    public ApiCaller(JwtClaimsCreator jwtClaimsCreator)
    {
        _jwtClaimsCreator = jwtClaimsCreator;
    }

    public async Task CallPvkApiAsync()
    {
        // Step 1: Get the access token

        var clientConfigurator = new ClientConfigurator();
        var client = clientConfigurator.ConfigureClient();

        string accessToken = client.GetAccessToken();

        // Step 2: Construct the JWT
        var payloadClaims = new Dictionary<string, object>
        {
            { "sub", "your-subject" },
            { "aud", "test_proton_register_forskningsportalen" },
            { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "exp", DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() }
        };

        var jwt = CreateJwt(payloadClaims, accessToken);

        // Step 3: Make the HTTP request
        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://third-party-server.com/api/endpoint");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var payload = new
        {
            key1 = "value1",
            key2 = "value2"
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Request successful!");
        }
        else
        {
            Console.WriteLine($"Request failed with status code: {response.StatusCode}");
        }
    }

    private string CreateJwt(Dictionary<string, object> claims, string accessToken)
    {
        // Use JwtClaimsCreator to create the JWT payload
        var payload = _jwtClaimsCreator.CreateJwtClaims(
            new CustomPayloadClaimsCreator(), // Implement this as needed
            new PayloadClaimParameters { AccessToken = accessToken },
            new HelseIdConfiguration()
        );

        // Combine with additional claims
        foreach (var claim in claims)
        {
            payload[claim.Key] = claim.Value;
        }

        // Sign and encode the JWT (use a signing library or HelseID's signing utilities)
        return JwtTokenSigner.Sign(payload, "your-signing-key");
    }
}
