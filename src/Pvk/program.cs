using PvkBroker.HelseId.ClientCredentials.Client;
using PvkBroker.HelseId.ClientCredentials.Configuration;
using PvkBroker.Pvk.ApiCaller;

using System.Security.Claims;

// using PvkBroker.Encryption;
// using PvkBroker.Kodeliste;

namespace PvkBroker.Pvk
{
    public class Program
    {
        AccessTokenCaller _accessTokenCaller = new AccessTokenCaller();
        PvkCaller _pvkCaller = new PvkCaller();

        static async Task Main()
        {
            string accessToken = await _accessTokenCaller.GetAccessToken();
            Console.WriteLine("Received access token:", accessToken);

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