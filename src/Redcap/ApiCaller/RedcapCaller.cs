using PvkBroker.Configuration;

using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

using Serilog;

namespace PvkBroker.Redcap
{
    class RedcapInterface
    {
        static async Task ExportAndImportAsync(string registerNavn, string recordId)
        {
            if (!ConfigurationValues.RedcapApiTokens.TryGetValue(registerNavn, out var sourceApiToken))
            {
                Log.Error($"Register '{registerNavn}' not found in configuration.");
                return;
            }

            string sourceUrl = ConfigurationValues.RedcapKrestUrl;
            string sourceApiToken = sourceApiToken;

            string targetUrl = ConfigurationValues.RedcapNorpregUrl;
            string targetApiToken = ConfigurationValues.RedcapApiToken.RedcapApiToken["NORPREG"];

            var httpClient = new HttpClient();

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

            var importContent = new FormUrlEncodedContent()(new[]
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

            Log.Info("Data from {registerNavn} imported successfully to {targetUrl}: {result}", registerNavn, targetUrl, result);
        }

        static async Task<List<string>> GetAllRecordIdsAsync()
        {
            var httpClient = new HttpClient();
            var url = ConfigurationValues.RedcapNorpregUrl;

            var fetchContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", ConfigurationValues.RedcapApiToken["NORPREG"]),
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
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var records = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(jsonResponse);

            var recordIds = new List<string>();
            foreach (var record in records)
            {
                if (record.TryGetValue("record_id", out var recordIdElement))
                {
                    recordIds.Add(recordIdElement.GetString());
                }
            }
            return recordIds;
        }

        static async Task RemovePatient(string recordId)
        {
            var httpClient = new HttpClient();
            var url = ConfigurationValues.RedcapNorpregUrl;

            var removeContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("token", ConfigurationValues.RedcapApiToken["NORPREG"]),
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
            Log.Info("Patient with record ID {recordId} removed successfully from {url}", recordId, url);
        }
    }
}