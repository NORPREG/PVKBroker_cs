using System.Net.Http.Headers;
using System.Text.Json;
using IdentityModel.Client;


using PvkBroker.Configuration;
using PvkBroker.HelseId.ClientCredentials.Client;
using PvkBroker.HelseId.ClientCredentials.Configuration;

// FROM DLL
using HelseId.Samples.Common.Endpoints;
using HelseId.Samples.Common.JwtTokens;
using HelseId.Samples.Common.Interfaces.JwtTokens;
using HelseId.Samples.Common.Configuration;
using HelseId.Samples.Common.Models;
using System.Net.Cache;
using System.Net.Http;
using System;

namespace PvkBroker.Pvk.ApiCaller;

public class PvkCaller
{
    IDPoPProofCreator? _idPoPProofCreator;
    HelseIdConfiguration _configuration;
    HttpClient _httpClient;

    public PvkCaller() {
        _configuration = SetUpHelseIdConfiguration();
        _httpClient = new HttpClient();
    }

    public PvkCaller(IDPoPProofCreator idPoPProofCreator)
     {
        _idPoPProofCreator = idPoPProofCreator;
        _configuration = SetUpHelseIdConfiguration();
        _httpClient = new HttpClient();
    }

    public HttpRequestMessage GetBaseRequestParameters(string accessToken, string url)
     {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (ConfigurationValues.ClientCredentialsUseDpop == true)
        {
            var dPopProof = _idPoPProofCreator.CreateDPoPProof(url, "GET", accessToken: accessToken);
            request.SetDPoPToken(accessToken, dPopProof);
        }

        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return request;
     }

     public async Task<string?> CallApiHentInnbyggere(string accessToken, int pagingReference = 0)
     {
        var url = ConfigurationValues.PvkSystemUrl + ConfigurationValues.PvkHentInnbyggereUrl;

        var request = GetBaseRequestParameters(accessToken, url);

        var payload = new
        {
            // definisjonGuid = ConfigurationValues.PvkApiClientId,
            definisjonGuid = ConfigurationValues.NewPvkDefinisjonGuid,
            // definisjonGuid = "1615e446-93a9-4cdc-97b0-9e32f47c1df9",
            partKode = ConfigurationValues.PvkPartKode,
            pagingReference = pagingReference
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        await LogHttpRequest(request);

        var response = await _httpClient.SendAsync(request);

        LogHttpResponse(response);

        // response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        return responseBody;

        /*
        return JsonSerializer.Deserialize<ApiResponseHentInnbyggere>(
            responseBody,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
        */
    }

    private static HelseIdConfiguration SetUpHelseIdConfiguration()
    {
        var result = HelseIdSamplesConfiguration.ClientCredentialsClient;

        return result;
    }

    public static async Task LogHttpRequest(HttpRequestMessage request)
    {
        if (request.Content != null)
        {
            // Les originalt innhold og kopier headers
            var originalContent = request.Content;
            var buffer = await originalContent.ReadAsByteArrayAsync();
            var contentString = System.Text.Encoding.UTF8.GetString(buffer);

            Console.WriteLine("Body:");
            Console.WriteLine(contentString);

            // Lag nytt innhold
            var newContent = new ByteArrayContent(buffer);

            // Kopier over gamle headers
            foreach (var header in originalContent.Headers)
            {
                newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Sett nytt innhold tilbake
            request.Content = newContent;
        }
    }

    public static async Task LogHttpResponse(HttpResponseMessage response)
    {
        Console.WriteLine("---- HTTP RESPONSE ----");
        Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");

        Console.WriteLine("Headers:");
        foreach (var header in response.Headers)
        {
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        if (response.Content != null)
        {
            Console.WriteLine("Content Headers:");
            foreach (var header in response.Content.Headers)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            string body = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Body:");
            Console.WriteLine(body);
        }

        Console.WriteLine("------------------------");
    }
}