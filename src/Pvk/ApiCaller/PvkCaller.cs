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
using Serilog;

namespace PvkBroker.Pvk.ApiCaller;

public class PvkCaller
{
    private readonly IDPoPProofCreator? _idPoPProofCreator;
    private readonly HelseIdConfiguration HelseIdConfigurationValues;
    private readonly Kodeliste _kodeliste;
    private readonly IHttpClientFactory _httpClientFactory;

    public PvkCaller(
        Kodeliste kodeliste,
        IHttpClientFactory httpClientFactory
    )
    {
        HelseIdConfigurationValues = SetUpHelseIdConfiguration();
        _kodeliste = kodeliste;
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

    public async Task<List<SimplePvkEvent>> CallApiHentInnbyggereAktivePiForDefinisjon(string accessToken, int pagingReference = 0)
    {
        var query = new Dictionary<string, string>
        {
            { "definisjonGuid", ConfigurationValues.PvkDefinisjonGuid_1 },
            { "partKode", ConfigurationValues.PvkPartKode },
            { "pagingReference", pagingReference.ToString() }
        };


        var queryString = string.Join("&", query.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        var url = ConfigurationValues.PvkSystemUrl + ConfigurationValues.PvkHentInnbyggereAktivePiForDefinisjonUrl + "?" + queryString;
        var request = CreateHttpRequestMessage(accessToken, url, "GET");

        var responseBody = await SendRequestAndHandleResponse(request);
        var jsonResponse = ResponseParser.ParseApiResponseHentInnbyggere(responseBody);
        List<SimplePvkEvent> pvkEvents = ResponseParser.ParseResponse(jsonResponse);

        return pvkEvents;
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