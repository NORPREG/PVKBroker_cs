using HelseId.ClientCredentials.Client;
using HelseId.ClientCredentials.Configuration;

// using PvkBroker.Encryption;
// using PvkBroker.Kodeliste;

namespace PvkBroker
{
    static class Program
    {
        AccessTokenCaller _accessTokenCaller = AccessTokenCaller();
        PvkCaller _pvkCaller = PvkCaller();

        static async Task Program()
        {
            string accessToken = await _accessTokenCaller.GetAccessToken();
            Console.WriteLine("Received access token:"; accessToken);

            ClaimsPrincipal principal = await _accessTokenCaller.ValidateAccessTokenAsync(accessToken);
            Console.WriteLine("Access token validation result:");
            foreach (Claim claim in principal.Claims)
            {
                Console.WriteLine("CLAIM TYPE: " + claim.Type + "; CLAIM VALUE: " + claim.Value);
            }

            string result = await _pvkCaller.CallApiHentInnbyggere(accessToken);
            Console.WriteLine("PVK API call result:", result);
        }
    }
}