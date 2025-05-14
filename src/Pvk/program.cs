using PvkBroker.HelseId.ClientCredentials.Client;
using PvkBroker.HelseId.ClientCredentials.Configuration;
using PvkBroker.Pvk.ApiCaller;
using PvkBroker.Configuration;

using System.Security.Claims;

using System.Runtime.CompilerServices;
using Serilog;
using System;

// using PvkBroker.Encryption;
// using PvkBroker.Kodeliste;


// eventually move this logic out to src / program.cs and make that the executable

namespace PvkBroker.Pvk
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            SetupLogging.Initialize();

            AccessTokenCaller _accessTokenCaller = new AccessTokenCaller();
            string accessToken = await _accessTokenCaller.GetAccessToken();

            // Console.WriteLine("Received access token:", accessToken);

            ClaimsPrincipal principal = await _accessTokenCaller.ValidateAccessTokenAsync(accessToken);
            Log.Information("Successfully validated Access Token.");
            Console.WriteLine("Access token validation result:");
            foreach (Claim claim in principal.Claims)
            {
                Console.WriteLine("CLAIM TYPE: " + claim.Type + "; CLAIM VALUE: " + claim.Value);
            }

            PvkCaller _pvkCaller = new PvkCaller();
            string result = await _pvkCaller.CallApiHentInnbyggereAktivePiForDefinisjon(accessToken);
            // string result = await _pvkCaller.CallApiSjekkInnbygger("13116900216", accessToken);
            // string result = await _pvkCaller.CallApiHentInnbyggerForPart("13116900216", accessToken);
        }
    }
}