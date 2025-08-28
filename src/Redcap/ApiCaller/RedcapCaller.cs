using PvkBroker.Configuration;

using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

using Serilog;
using Org.BouncyCastle.Asn1;

namespace PvkBroker.Redcap
{
    public class RedcapInterface
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RedcapInterface(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task ExportAndImportAsync(string registerNavn, string recordId)
        {
            if (!ConfigurationValues.RedcapApiToken.TryGetValue(registerNavn, out var sourceApiToken))
            {
                Log.Error($"Register '{registerNavn}' not found in configuration.");
                return;
            }

            // Same url for all KREST registers, but different api tokens
            string sourceUrl = ConfigurationValues.RedcapKrestUrl;

            string targetUrl = ConfigurationValues.RedcapNorpregUrl;
            string? targetApiToken = ConfigurationValues.RedcapApiToken["NORPREG"];

            if (string.IsNullOrEmpty(targetApiToken) || string.IsNullOrEmpty(sourceApiToken))
            {
                Log.Error("API tokens for REDCap is not configured.");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient();

            var fetchContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", sourceApiToken),
                new KeyValuePair<string, string>("content", "record"),
                new KeyValuePair<string, string>("action", "export"),
                new KeyValuePair<string, string>("format", "json"),
                new KeyValuePair<string, string>("type", "flat"),
                new KeyValuePair<string, string>("records[0]", recordId)
            });

            var fetchResponse = await httpClient.PostAsync(sourceUrl, fetchContent);
            if (!fetchResponse.IsSuccessStatusCode)
            {
                Log.Error($"Failed to fetch data from {sourceUrl}: {fetchResponse.StatusCode}");
                return;
            }

            var patientJson = await fetchResponse.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(patientJson) || patientJson == "[]")
            {
                Log.Warning($"No data found for record ID {recordId} in register {registerNavn}.");
                return;
            }

            var importContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", targetApiToken),
                new KeyValuePair<string, string>("content", "record"),
                new KeyValuePair<string, string>("format", "json"),
                new KeyValuePair<string, string>("action", "import"),
                new KeyValuePair<string, string>("type", "flat"),
                new KeyValuePair<string, string>("overwriteBehavior", "overwrite"),
                new KeyValuePair<string, string>("data", patientJson)
            });

            var importResponse = await httpClient.PostAsync(targetUrl, importContent);
            if (!importResponse.IsSuccessStatusCode)
            {
                Log.Error($"Failed to fetch data from {sourceUrl}: {fetchResponse.StatusCode}");
                return;
            }

            string result = await importResponse.Content.ReadAsStringAsync();

            Log.Information("Data from {registerNavn} imported successfully to {targetUrl}: {result}", registerNavn, targetUrl, result);
        }

        public async Task<List<string>> GetAllRecordIdsAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = ConfigurationValues.RedcapNorpregUrl;

            string? targetApiToken = ConfigurationValues.RedcapApiToken["NORPREG"];

            if (string.IsNullOrEmpty(targetApiToken))
            {
                Log.Error("API tokens for REDCap is not configured.");
                return new List<string>();
            }

            var fetchContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", targetApiToken), 
                new KeyValuePair<string, string>("content", "record"),
                new KeyValuePair<string, string>("action", "export"),
                new KeyValuePair<string, string>("format", "json"),
                new KeyValuePair<string, string>("type", "flat"),
                new KeyValuePair<string, string>("fields[0]", "record_id")
            });

            var response = await httpClient.PostAsync(url, fetchContent);
            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Failed to fetch data from {url}: {response.StatusCode}");
                return new List<string>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var records = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(jsonResponse);

            if (records == null || records.Count == 0)
            {
                Log.Warning($"No records found in REDCap NORPREG.");
                return new List<string>();
            }

            var recordIds = new List<string>();
            foreach (var record in records)
            {
                if (record.TryGetValue("record_id", out var recordIdElement))
                {
                    string recordIdElementString = recordIdElement.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(recordIdElementString))
                        recordIds.Add(recordIdElementString);
                }
            }
            return recordIds;
        }

        public async Task RemovePatient(string recordId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = ConfigurationValues.RedcapNorpregUrl;

            var targetApiToken = ConfigurationValues.RedcapApiToken["NORPREG"];

            if (string.IsNullOrEmpty(targetApiToken))
            {
                Log.Error("API token for REDCap NORPREG is not configured.");
                return;
            }

            var removeContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", targetApiToken),
                new KeyValuePair<string, string>("content", "record"),
                new KeyValuePair<string, string>("action", "delete"),
                new KeyValuePair<string, string>("returnFormat", "json"),
                new KeyValuePair<string, string>("records[0]", recordId)
            });
            var response = await httpClient.PostAsync(url, removeContent);
            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Failed to delete data from {url}: {response.StatusCode}");
                return;
            }
            Log.Information("Patient with record ID {recordId} removed successfully from {url}", recordId, url);
        }
    }
}