using System.Net.Http.Headers;
using System.Text.Json;
using HelseId.Common.JwtTokens;
using HelseId.Common.Configuration;
using HelseId.ClientCredentials.Client;
using HelseId.Configuration;
using HelseId.Samples.Common.Endpoints;

namespace PvkBroker.ApiCaller;

public class PvkCaller
{
     var _configuration = SetUpHelseIdConfiguration();

     public PvkCaller() {
          _idPoPProofCreator = null;
     }

     public PvkCaller(IDPoPProofCreator idPoPProofCreator)
     {
          _idPoPProofCreator = idPoPProofCreator;
     }

     public HttpRequestMessage GetBaseRequestParameters(string accessToken, string url)
     {
          using var httpClient = new HttpClient();
          var request = new HttpRequestMessage(HttpMethod.Get, url);

          if (_idPoPProofCreator)
          {
               var dPopProof = _idPoPProofCreator.CreateDPoPProof(_url, "GET", accessToken: accessToken);
               request.SetDPoPToken(accessToken, dPopProof);
          }

          else
          {
               request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
          }

          return request
     }

     public async Task<HttpResponseMessage?> CallApiHentInnbyggere(string accessToken, string pagingReference = 0)
     {
          var url = _configuration.PvkBaseUrl + _configuration.PvkHentInnbyggereUrl;

          var request = GetBaseRequestParameters(accessToken, url);

          // need new payload to adhere to v2.....
          // https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/
          // 2328952849/Hente+informasjon+fra+PVK+om+innbyggers+personverninnstillinger

          var payload = new
          {
               definisjonGuid = _configuration.PvkApiClientId,
               partKode = _configuration.PvkPartKode,
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
}