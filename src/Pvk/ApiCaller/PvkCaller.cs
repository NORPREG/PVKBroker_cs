using System.Net.Http.Headers;
using System.Text.Json;
using HelseId.Common.JwtTokens;
using HelseId.Common.Configuration;
using HelseId.ClientCredentials.Client;

namespace PvkBroker.ApiCaller;

public class PvkCaller
{
    /*
    // For use when PVK API supports DPoP
    private readonly IDPoPProofCreator? _idPoPProofCreator;
    public ApiConsumer(IDPoPProofCreator idPoPProofCreator)
    {
        _idPoPProofCreator = idPoPProofCreator;
    }

    ref: HelseId.Common.ApiConsumers

    */

    private readonly string _baseurl = "https://eksternapi-helsenett.hn2.test.nhn.no";
    private readonly string _url = $"{_baseurl}/HentInnbyggereAktivePiForDefinisjon/v2";

    public PvkCaller() {}

    public async Task<HttpResponseMessage?> CallApiHentInnbyggereAktivePiForDefinisjon(string accessToken)
    {
        using var httpClient = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Get, _url);

        // Build support DPoP when the PVK API supports this
        // Set useDPoP = true in the config in that case to get a dpop-enabled accessToken

        // var dPopProof = _idPoPProofCreator.CreateDPoPProof(apiUrl, "GET", accessToken: accessToken);
        // request.SetDPoPToken(accessToken, dPopProof);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        var payload = new
        {
            definisjonGuid = "2c11f0ca-7270-43f1-a473-bac325feb3f6",
            partKode = "prostraa",
            pagingReference = 0
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<ApiReponseHentInnbyggere>(
            responseBody,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
    }
}