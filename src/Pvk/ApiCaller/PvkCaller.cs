using Azure;
using HelseId.Samples.Common.Configuration;
// FROM DLL
using HelseId.Samples.Common.Endpoints;
using HelseId.Samples.Common.Interfaces.JwtTokens;
using HelseId.Samples.Common.JwtTokens;
using HelseId.Samples.Common.Models;
using IdentityModel.Client;
using Microsoft.OpenApi.Validations;
using PvkBroker.Configuration;
using PvkBroker.HelseId.ClientCredentials.Client;
using PvkBroker.HelseId.ClientCredentials.Configuration;
using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json; 

namespace PvkBroker.Pvk.ApiCaller;

public class PvkCaller
{
    private readonly IDPoPProofCreator _idPoPProofCreator;
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

        // Set the Authorization header with the access token
        // This is because PVK doesn't implement DPoP yet ...

        if (ConfigurationValues.PvkUseBearerToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }


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

            var response = await SendRequestAndHandleResponse(request);

            if (!response.Success)
            {
                Log.Error("Error in PVK HTTP response: {ErrorMessage}", response.ErrorMessage);
                if (allEvents.Count > 0)
                {
                    Log.Warning("PVK error, but some events were already collected. Returning collected events.");
                }
                return allEvents;
            }

            var jsonResponse = ResponseParser.ParseApiResponseHentInnbyggere(response.Data);
            var pageEvents = ResponseParser.ParseResponse(jsonResponse);

            allEvents.AddRange(pageEvents);
            pagingReference = jsonResponse.pagingReference;

            // sleep 100 milliseconds to avoid hitting rate limits
            await Task.Delay(100);

        } while (pagingReference != "0");

        return allEvents;
    }

    public async Task<bool> CallApiSettInnbyggersPersonvernInnstilling(string accessToken, string jsonPath)
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
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(payload, options);

        Console.WriteLine("JSON payload to be sent: " + json);

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        Console.WriteLine("Sending request to PVK API to set personverninnstilling for innbygger.");

        var response = await SendRequestAndHandleResponse(request);
        
        if (!response.Success)
        {
            Log.Error("Error in PVK HTTP response: {ErrorMessage}", response.ErrorMessage);
        }

        return response.Success;
    }

    private static HelseIdConfiguration SetUpHelseIdConfiguration()
    {
        var result = HelseIdSamplesConfiguration.ClientCredentialsClient;

        return result;
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

    public async Task<ApiResult<string>> SendRequestAndHandleResponse(HttpRequestMessage request)
    {
        // await DumpHttpRequestToConsole(request);
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.SendAsync(request);
        await LogHttpResponseToConsole(response);

        if (response.IsSuccessStatusCode)
        {
            Log.Information("Successful response from PVK API.");
            var responseBody = await response.Content.ReadAsStringAsync();

            var responseObj = new ApiResult<string>
            {
                Success = true,
                Data = responseBody
            };

            return responseObj;

        }
        else
        {            
            var responseObj = new ApiResult<string> {
                Success = false,
                ErrorMessage = $"Error in PVK HTTP response: {response.StatusCode} - {response.ReasonPhrase}"
            };

            return responseObj;
        }
    }

    private async Task DumpHttpRequestToConsole(HttpRequestMessage request)
    {
        var curl = new StringBuilder();

        curl.Append("curl");

        // Metode
        if (request.Method != HttpMethod.Get)
        {
            curl.Append($" -X {request.Method.Method}");
        }

        // Headers
        foreach (var header in request.Headers)
        {
            foreach (var value in header.Value)
            {
                curl.Append($" -H \"{header.Key}: {value}\"");
            }
        }

        // Body (hvis POST)
        if (request.Content != null)
        {
            var body = await request.Content.ReadAsStringAsync();
            var contentType = request.Content.Headers.ContentType?.ToString();
            if (!string.IsNullOrEmpty(contentType))
            {
                curl.Append($" -H \"Content-Type: {contentType}\"");
            }
            curl.Append($" --data '{body}'");
        }

        // URL
        curl.Append($" \"{request.RequestUri}\"");

        Console.WriteLine("\n--- CURL COMMAND ---\n");
        Console.WriteLine(curl.ToString());
    }
}