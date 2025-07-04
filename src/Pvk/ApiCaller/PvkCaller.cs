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
using PvkBroker.Datamodels;

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
        int pagingReference = 0;

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
                HandleError(response);
                if (allEvents.Count > 0)
                {
                    Log.Warning("PVK error, but some events were already collected. Returning collected events.");
                }
                return allEvents;
            }

            if (string.IsNullOrEmpty(response.Data))
            {
                Log.Warning("Received empty response from PVK API. No events to process.");
                return allEvents;
            }

            var jsonResponse = ResponseParser.ParseApiResponseHentInnbyggere(response.Data);
            
            string prettyJson = JsonSerializer.Serialize(jsonResponse, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Console.WriteLine($"Respons f�r tolkning:\n {prettyJson}\n\n");

            if (jsonResponse == null)
            {
                Log.Error("Failed to parse JSON response from PVK API. Response: {ResponseData}", response.Data);
                return allEvents;
            }

            var pageEvents = ResponseParser.ParseResponse(jsonResponse);

            allEvents.AddRange(pageEvents);
            pagingReference = jsonResponse.pagingReference;

            // sleep 100 milliseconds to avoid hitting rate limits
            await Task.Delay(100);

        } while (pagingReference != 0);

        return allEvents;
    }

    public async Task<ApiResult<string>> CallApiSettInnbyggersPersonvernInnstilling(string accessToken, string jsonPath)
    {
        string systemUrl = ConfigurationValues.PvkSystemUrl;
        string baseUrl = ConfigurationValues.PvkSettInnbyggersPersonvernInnstillingUrl;
        var url = $"{systemUrl}{baseUrl}";
        var request = CreateHttpRequestMessage(accessToken, url, "POST");

        ApiRequestSettInnbygger payload = await SettInnbyggerJsonReader.ReadJsonFile(jsonPath);

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(payload, options);

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await SendRequestAndHandleResponse(request);

        if (!response.Success)
        {
            HandleError(response, payload);
            return response;     
        }

        if (string.IsNullOrEmpty(response.Data))
        {
            Log.Warning("Received empty response from PVK API. No events to process.");
            return response;
        }

        var parsedResponse = ResponseParser.ParseApiResponseSettInnbygger(response.Data);

        if (parsedResponse == null)
        {
            Log.Error("Failed to parse JSON response from PVK API. Response: {ResponseData}", response.Data);
            return new ApiResult<string>
            {
                Success = false,
                ErrorMessage = "Failed to parse JSON response from PVK API."
            };
        }

        if (parsedResponse.instansEndret == "ikkeEndret")
        {
            Log.Information("PVK response indicates no change in instance. No action taken.");
            Console.WriteLine($"PVK response indicates no change in instance for PatientID {payload.innbyggerFnr}. No action taken.");
        }

        return response;
    }

    private static HelseIdConfiguration SetUpHelseIdConfiguration()
    {
        var result = HelseIdSamplesConfiguration.ClientCredentialsClient;

        return result;
    }

    public void HandleError(ApiResult<string> response, ApiRequestSettInnbygger? payload = null)
    {
        Log.Error("Error in PVK HTTP response: {ErrorMessage}", response.ErrorMessage);

        if (!string.IsNullOrEmpty(response.Data))
        {
            var parsedError = ResponseParser.ParseApiResponseSettInnbyggerError(response.Data);
            Log.Error("PVK API return error {Code}: {Message}", parsedError?.code, parsedError?.message);

            if (parsedError?.code == "EPVK-101518")
            {
                Console.WriteLine($"Fant ingen innbygger med innbyggerFnr {payload?.innbyggerFnr}.");
            }
            else if (parsedError?.code == "EPVK-101500")
            {
                Console.WriteLine(parsedError?.message);
                Console.WriteLine("(sjekk at du har riktig definisjonGuid / partKode i konfigurasjonen.)");
            }
            else
            {
                Console.WriteLine(parsedError?.message);
            }
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

    public async Task<ApiResult<string>> SendRequestAndHandleResponse(HttpRequestMessage request)
    {
        // await DumpHttpRequestToConsole(request);
        var httpClient = _httpClientFactory.CreateClient();

        try {

            httpClient.Timeout = TimeSpan.FromSeconds(10);
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
                var responseBody = await response.Content.ReadAsStringAsync();

                Log.Error("Feil i svar fra PVK: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);

                var responseObj = new ApiResult<string> {
                    Success = false,
                    ErrorMessage = $"Error in PVK HTTP response: {response.StatusCode} - {response.ReasonPhrase}",
                    Data = responseBody
                };

                return responseObj;
            }
        }
       catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP request failed: {Message}", ex.Message);
            Console.WriteLine($"HTTP request failed: {ex.Message}");
            return new ApiResult<string>
            {
                Success = false,
                ErrorMessage = $"HTTP request failed: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unexpected error occurred: {Message}", ex.Message);
            return new ApiResult<string>
            {
                Success = false,
                ErrorMessage = $"An unexpected error occurred: {ex.Message}"
            };
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