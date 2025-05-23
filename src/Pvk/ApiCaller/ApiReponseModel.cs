namespace PvkBroker.Pvk.ApiCaller;

// Model description at:
// https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2328952849/Hente+informasjon+fra+PVK+om+innbyggers+personverninnstillinger

public class ApiResponseHentInnbyggere
{
	public string? definisjonGuid { get; set; } // GUID GUID
	public string? definisjonNavn { get; set; } // Reservasjon til Norsk proton- og stråleterapiregister
	public string? partKode { get; set; } //  norpreg
	public string? typePi { get; set; } // samtykke, reservasjon, tilgangsbegrensning
	public Meta ReFasteMetadata { get; set; } // faste metedata for 
    public string? pagingReference { get; set; } // Dersom den har verdien 0, trenger det ikke å gjøres flere kall. Dersom annen verdi, må det gjøres etterfølgende kall med angitt pagingReference.

    public List<Pi>? personvernInnstillinger { get; set; }
}

public class Pi
{
	public string? innbyggerFnr { get; set; } // Fødselsnummer eller D-nummer
	public int? sekvensnummer { get; set; } // Sekvensnummer på innbyggerens innstilling for denne definisjonen -- starter på 1, og økes for hver gang den endres
	public DateTime opprettetTidspunkt { get; set; } // Når PI første gang ble satt
	public DateTime sistEndretTidspunkt { get; set; } // Når PI sist ble endret
}

public class ReFasteMetadata
{
    public OmfangElementer ommfangElementer { get; set; } // GUID
}

public class OmfangElementer
{
	public string? omfangKode { get; set; } //  UO
	public string? logiskOmfang { get; set; } //  Angitte
	public string? presisering { get; set; } //  Direkte personidentifiserende opplysninger
}

// Simplification of the response with PatientKey included
public class SimplePvkEvent
{
    public string PatientID { get; set; }
	public string PatientKey { get; set; }
	public string IsReserved { get; set; } // "0": Ikke reservert, "1": Reservert
    public DateTime EventTime { get; set; }
}