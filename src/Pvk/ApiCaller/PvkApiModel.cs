using System.Text;  
using Serilog;

using PvkBroker.Configuration;
using PvkBroker.Datamodels;

namespace PvkBroker.Pvk.ApiCaller;

// Model description at:
// https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2328952849/Hente+informasjon+fra+PVK+om+innbyggers+personverninnstillinger

public class ApiResponseHentInnbyggere
{
	public string? definisjonGuid { get; set; } // GUID GUID
	public string? definisjonNavn { get; set; } // Reservasjon til Norsk proton- og stråleterapiregister
	public string? partKode { get; set; } //  norpreg
	public string? typePi { get; set; } // samtykke, reservasjon, tilgangsbegrensning
	public ReFasteMetadata? reFasteMetadata { get; set; } // faste metedata for 
    public required int pagingReference { get; set; } // Dersom den har verdien 0, trenger det ikke å gjøres flere kall. Dersom annen verdi, må det gjøres etterfølgende kall med angitt pagingReference.

    public required List<Pi> personvernInnstillinger { get; set; }
}

public class Pi
{
	public required string innbyggerFnr { get; set; } // Fødselsnummer eller D-nummer
	public required int sekvensnummer { get; set; } // Sekvensnummer på innbyggerens innstilling for denne definisjonen -- starter på 1, og økes for hver gang den endres
	public DateTime opprettetTidspunkt { get; set; } // Når PI første gang ble satt
	public DateTime sistEndretTidspunkt { get; set; } // Når PI sist ble endret
}

public class ReFasteMetadata
{
    public List<OmfangElementer>? omfangElementer { get; set; } // GUID
}

public class OmfangElementer
{
	public string? omfangKode { get; set; } //  OF
	public string? logiskOmfang { get; set; } //  Angitte
	public string? presisering { get; set; } //  Direkte personidentifiserende opplysninger
}

public class InputDataSettInnbygger
{
    public string? innbyggerFnr { get; set; }
    public bool? aktiv { get; set; } // true (reservasjon satt) eller false (ingen reservasjon satt)
    public string? datetime { get; set; }
    public string? pathToBevisInnhold { get; set; } // Path to the signed proof document
}

public class ApiRequestSettInnbygger
{
    public string? innbyggerFnr { get; set; }
    public string? definisjonGuid { get; set; } // 56a8756c-49f7-4cb9-bfc0-ba282baf0f83
    public string? definisjonNavn { get; set; } // Reservasjon mot oppføring i NORPREG
    public string? partKode { get; set; } // norpreg
    public string? typePi { get; set; } // reservasjon
    public bool? aktiv { get; set; } // true (reservasjon satt) eller false (ingen reservasjon satt)
    public string? tidspunkt { get; set; } // ISO 8601 format, e.g. "2023-10-01T12:00:00Z"
    public string? signertBevisMimeType { get; set; } // image/png, image/jpeg, image/bmp, application/pdf
    public string? signertBevisInnhold { get; set; } // Base64 encoded content of the signed proof document
}

public class ApiResponseSettInnbygger
{
    public required string returKode { get; set; } // ok; ikkeOk
    public required string instansEndret { get; set; } // endret; ikkeEndret
}

public class ApiResponseSettInnbyggerError
{
    public required string code { get; set; }
    public required string message { get; set; } 
    public required string errorCategory { get; set; }
}

public class ApiResult<T>
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public T? Data { get; set; }
}

public class ResponseParser
{
    public static ApiResponseHentInnbyggere? ParseApiResponseHentInnbyggere(string responseBody)
	{ 
		var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

		if (string.IsNullOrEmpty(responseBody)) {
            Log.Error("Received empty response body from API.");
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ApiResponseHentInnbyggere>(responseBody, jsonOptions);
        }

        catch (JsonException ex)
        {
            Log.Error("Error deserializing API response: {@ex}", ex);
            return null;
        }
    }

    public static ApiResponseSettInnbygger? ParseApiResponseSettInnbygger(string responseBody)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        if (string.IsNullOrEmpty(responseBody)) {
            Log.Error("Received empty response body from API.");
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<ApiResponseSettInnbygger>(responseBody, jsonOptions);
        }
        catch (JsonException ex)
        {
            Log.Error("Error deserializing API response: {@ex}", ex);
            return null;
        }
    }

    public static ApiResponseSettInnbyggerError? ParseApiResponseSettInnbyggerError(string responseBody)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        if (string.IsNullOrEmpty(responseBody)) {
            Log.Error("Received empty response body from API.");
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<ApiResponseSettInnbyggerError>(responseBody, jsonOptions);
        }
        catch (JsonException ex)
        {
            Log.Error("Error deserializing API error response: {@ex}", ex);
            return null;
        }
    }

    public static List<SimplePvkEvent> ParseResponse(ApiResponseHentInnbyggere apiResponse)
    {

        var pvkEvents = new List<SimplePvkEvent>();
        foreach (var eventItem in apiResponse.personvernInnstillinger)
        {
            var simpleEvent = new SimplePvkEvent
            {
                PatientFnr = eventItem.innbyggerFnr,
                // PatientKey = _kodeliste.GetPatientKey(eventItem.innbyggerFnr),
                EventTime = eventItem.sistEndretTidspunkt,
                IsReserved = true
            };
            pvkEvents.Add(simpleEvent);
        }
        return pvkEvents;
    }
}

public class SettInnbyggerJsonReader
{
    static readonly Dictionary<string, string> allowedMimeTypes = new()
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".bmp"] = "image/bmp"
    };

    public static async Task<ApiRequestSettInnbygger> ReadJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Log.Error("JSON file not found at path: {FilePath}", filePath);
            throw new FileNotFoundException($"JSON file not found at path: {filePath}");
        }

        string inputDirectory = Path.GetDirectoryName(filePath)!;

        try
        {
            string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var input = JsonSerializer.Deserialize<InputDataSettInnbygger>(json);

            var payload = new ApiRequestSettInnbygger
            {
                innbyggerFnr = input?.innbyggerFnr,
                definisjonGuid = ConfigurationValues.PvkDefinisjonGuid_1,
                definisjonNavn = ConfigurationValues.PvkDefinisjonNavn_1,
                partKode = ConfigurationValues.PvkPartKode,
                typePi = ConfigurationValues.PvkTypePi,
                aktiv = input?.aktiv,
                tidspunkt = input?.datetime
            };

            if (!string.IsNullOrWhiteSpace(input?.pathToBevisInnhold))
            {
                string resolvedPath = Path.Combine(inputDirectory, input.pathToBevisInnhold);

                if (string.IsNullOrEmpty(resolvedPath) || !Path.Exists(resolvedPath))
                {
                    Log.Error("Resolved path to bevisInnhold does not exist: {ResolvedPath}", resolvedPath);
                    throw new FileNotFoundException($"Resolved path does not exist: {resolvedPath}");
                }

                string? extension = Path.GetExtension(resolvedPath)?.ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(extension) && allowedMimeTypes.TryGetValue(extension, out string? mime))
                {
                    if (File.Exists(resolvedPath))
                    {
                        byte[] fileBytes = await File.ReadAllBytesAsync(resolvedPath);
                        string base64 = Convert.ToBase64String(fileBytes);

                        const int maxSizeInBytes = 4 * 1024 * 1024; // 4 MB
                        if (fileBytes.Length > maxSizeInBytes)
                        {
                            Log.Warning($"Filen er for stor ({fileBytes.Length} bytes), maks størrelse er {maxSizeInBytes} bytes.");
                        }
                        else
                        {
                            payload.signertBevisMimeType = mime;
                            payload.signertBevisInnhold = base64;
                        }
                    }
                    else
                    {
                        Log.Error($"Filen finnes ikke: {resolvedPath}");
                    }
                }
                else
                {
                    Log.Warning($"Filtype ikke støttet: {extension}");
                }
            }

            return payload;
        }
        catch (Exception ex)
        {
            Log.Error("Error reading JSON file: {@ex}", ex);
            throw;
        }
    }
}