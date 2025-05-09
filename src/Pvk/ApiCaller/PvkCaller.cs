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

namespace PvkBroker.Pvk.ApiCaller;



public class PvkCaller
{
    IDPoPProofCreator? _idPoPProofCreator;
    HelseIdConfiguration _configuration;

    public PvkCaller() {
        _configuration = SetUpHelseIdConfiguration();

    }

    public PvkCaller(IDPoPProofCreator idPoPProofCreator)
     {
        _idPoPProofCreator = idPoPProofCreator;
        _configuration = SetUpHelseIdConfiguration();
    }

     public HttpRequestMessage GetBaseRequestParameters(string accessToken, string url)
     {
          using var httpClient = new HttpClient();
          var request = new HttpRequestMessage(HttpMethod.Get, url);

          if (ConfigurationValues.ClientCredentialsUseDpop)
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

     public async Task<HttpResponseMessage?> CallApiHentInnbyggere(string accessToken, int pagingReference = 0)
     {
          var url = ConfigurationValues.PvkBaseUrl + ConfigurationValues.PvkHentInnbyggereUrl;

          var request = GetBaseRequestParameters(accessToken, url);

          var payload = new
          {
               definisjonGuid = ConfigurationValues.PvkApiClientId,
               partKode = ConfigurationValues.PvkPartKode,
               pagingReference = pagingReference
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

    private static HelseIdConfiguration SetUpHelseIdConfiguration()
    {
        var result = HelseIdSamplesConfiguration.ClientCredentialsClient;

        return result;
    }
}