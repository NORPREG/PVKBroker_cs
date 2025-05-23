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
using Serilog;

namespace PvkBroker.Pvk.ApiCaller;

public class PvkCaller
{
    private readonly IDPoPProofCreator? _idPoPProofCreator;
    private readonly HelseIdConfiguration HelseIdConfigurationValues;
    private static HttpClient _httpClient;
    private readonly Kodeliste _kodeliste;

    public PvkCaller(Kodeliste kodeliste) {
        HelseIdConfigurationValues = SetUpHelseIdConfiguration();
        _httpClient = new HttpClient();
        _kodeliste = kodeliste;
        _idPoPProofCreator = new DPoPProofCreator(HelseIdConfigurationValues);
    }

    public HttpRequestMessage GetBaseRequestParameters(string accessToken, string url, string httpMethod)
     {

        HttpRequestMessage request;

        // Set the base URL and HTTP method
        if (httpMethod == "GET")
        {
            request = new HttpRequestMessage(HttpMethod.Get, url);
        }
        else 
        {
            request = new HttpRequestMessage(HttpMethod.Post, url);
        }

        // Set DPoP or Bearer token
        var dPopProof = _idPoPProofCreator.CreateDPoPProof(url, "GET", accessToken: accessToken);
        request.SetDPoPToken(accessToken, dPopProof);

        return request;
     }

    // Inactive
    public async Task<string?> CallApiSjekkInnbyggersPiStatus(string fnr, string accessToken, int pagingReference = 0)
    {
        var url = ConfigurationValues.PvkSystemUrl + ConfigurationValues.PvkSjekkInnbyggersPiStatusUrl;

        var request = GetBaseRequestParameters(accessToken, url, "POST");

        var payload = new
        {
            innbyggerFnr = fnr,
            definisjonGuid = ConfigurationValues.PvkDefinisjonGuid_1,
            definisjonNavn = ConfigurationValues.PvkDefinisjonNavn_1,
            partKode = ConfigurationValues.PvkPartKode,
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await SendRequestAndHandleResponse(request);

        return response;
    }

    // Inactive
    public async Task<string?> CallApiHentInnbyggersForPart(string fnr, string accessToken, int pagingReference = 0)
    {
        var url = ConfigurationValues.PvkSystemUrl + ConfigurationValues.PvkHentInnbyggersPiForPartUrl;

        var request = GetBaseRequestParameters(accessToken, url, "POST");

        var payload = new
        {
            innbyggerFnr = fnr,
            partKode = ConfigurationValues.PvkPartKode
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json"); 
        
        var response = await SendRequestAndHandleResponse(request);

        return response;
    }

    // Active
    public async Task<ApiResponseHentInnbyggere?> CallApiHentInnbyggereAktivePiForDefinisjon(string accessToken, int pagingReference = 0)
     {
        var query = new Dictionary<string, string>
        {
            { "definisjonGuid", ConfigurationValues.PvkDefinisjonGuid_1 },
            { "partKode", ConfigurationValues.PvkPartKode },
            { "pagingReference", pagingReference.ToString() }
        };


        var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var url = ConfigurationValues.PvkSystemUrl + ConfigurationValues.PvkHentInnbyggereAktivePiForDefinisjonUrl + "?" + queryString;
        var request = GetBaseRequestParameters(accessToken, url, "GET");

        var response = await SendRequestAndHandleResponse(request);

        return JsonSerializer.Deserialize<ApiResponseHentInnbyggere>(
            responseBody,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

        // return response;
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

    public static async Task<string> SendRequestAndHandleResponse(HttpRequestMessage request)
    {
        await LogHttpRequest(request);
        var response = await _httpClient.SendAsync(request);
        await LogHttpResponse(response);

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

    public static List<SimplePvkEvent> ParseResponse(ApiResponseModel apiResponse)
    {
        var pvkEvents = new List<SimplePvkEvent>();
        foreach (var eventItem in apiResponse.personvernInnstillinger)
        {
            var simpleEvent = new SimplePvkEvent
            {
                PatientID = eventItem.innbyggerFnr,
                PatientKey = _kodeliste.GetPatientKey(PatientID),
                EventTime = eventItem.sistEndretTidspunkt,
                IsReserved = "1"
            };
            pvkEvents.Add(simpleEvent);
        }
        return pvkEvents;
    }
}