using System.Net.Http.Headers;
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
using System.Net.Http;
using System;
using System.Text;
using System.Text.Json; 
using Serilog;

namespace PvkBroker.Pvk.ApiCaller;

public class PvkCaller
{
    private readonly IDPoPProofCreator? _idPoPProofCreator;
    private readonly HelseIdConfiguration HelseIdConfigurationValues;
    private readonly IHttpClientFactory _httpClientFactory;

    public PvkCaller(
        IHttpClientFactory httpClientFactory
    )
    {
        HelseIdConfigurationValues = SetUpHelseIdConfiguration();
        _idPoPProofCreator = new DPoPProofCreator(HelseIdConfigurationValues);
        _httpClientFactory = httpClientFactory;
    }

    public HttpRequestMessage CreateHttpRequestMessage(string accessToken, string url, string httpMethod)
    {
        var method = httpMethod.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            _ => throw new ArgumentException("Unsupported HTTP method: " + httpMethod)
        };

        var request = new HttpRequestMessage(method, url);
        var dPopProof = _idPoPProofCreator.CreateDPoPProof(url, httpMethod, accessToken: accessToken);
        request.SetDPoPToken(accessToken, dPopProof);

        return request;
     }

    public async Task<List<SimplePvkEvent>> CallApiHentInnbyggereAktivePiForDefinisjon(string accessToken)
    {
        var allEvents = new List<SimplePvkEvent>();
        string pagingReference = "0";

        do
        {
            var query = new Dictionary<string, string>
            {
                { "definisjonGuid", ConfigurationValues.PvkDefinisjonGuid_1 },
                { "partKode", ConfigurationValues.PvkPartKode },
                { "pagingReference", pagingReference.ToString() }
            };
        
            string queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            string systemUrl = ConfigurationValues.PvkSystemUrl;
            string baseUrl = ConfigurationValues.PvkHentInnbyggereAktivePiForDefinisjonUrl;
            var url = $"{systemUrl}{baseUrl}?{queryString}";
        
            var request = CreateHttpRequestMessage(accessToken, url, "GET");

            var responseBody = await SendRequestAndHandleResponse(request);
            var jsonResponse = ResponseParser.ParseApiResponseHentInnbyggere(responseBody);
            var pageEvents = ResponseParser.ParseResponse(jsonResponse);

            allEvents.AddRange(pageEvents);
            pagingReference = jsonResponse.pagingReference;

            // sleep 100 milliseconds to avoid hitting rate limits
            await Task.Delay(100);

        } while (pagingReference != "0");

        return allEvents;
    }

    public async Task<string> CallApiSettInnbyggersPersonvernInnstilling(string accessToken, string jsonPath)
    {
        Console.WriteLine("CallApiSettInnbyggersPersonvernInnstilling");

        string systemUrl = ConfigurationValues.PvkSystemUrl;
        string baseUrl = ConfigurationValues.PvkSettInnbyggersPersonvernInnstillingUrl;
        var url = $"{systemUrl}{baseUrl}";
        var request = CreateHttpRequestMessage(accessToken, url, "POST");

        Console.WriteLine("Setting personverninnstilling for innbygger using JSON file: " + jsonPath);

        ApiRequestSettInnbygger payload = await SettInnbyggerJsonReader.ReadJsonFile(jsonPath);

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var json = JsonSerializer.Serialize(payload, options);

        Console.WriteLine("JSON payload to be sent: " + json);

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        Console.WriteLine("Sending request to PVK API to set personverninnstilling for innbygger.");

        // var responseBody = await SendRequestAndHandleResponse(request);
        string responseBody = "sasd";
        
        if (responseBody == null)
        {
            throw new Exception("Failed to set personverninnstilling for innbygger.");
        }
        return responseBody;
    }

    private static HelseIdConfiguration SetUpHelseIdConfiguration()
    {
        var result = HelseIdSamplesConfiguration.ClientCredentialsClient;

        return result;
    }

    public static async Task LogHttpRequestToConsole(HttpRequestMessage request)
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

    public static async Task LogHttpResponseToConsole(HttpResponseMessage response)
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

    public async Task<string?> SendRequestAndHandleResponse(HttpRequestMessage request)
    {
        await LogHttpRequestToConsole(request);
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.SendAsync(request);
        await LogHttpResponseToConsole(response);

        if (response.IsSuccessStatusCode)
        {
            Log.Information("Successful response from PVK API.");
            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;

        }
        else
        {
            // handle failure
            Log.Error("Error in PVK HTTP response: {@response}", response);
            return null;
        }
    }
}