namespace PvkBroker.Pvk.ApiCaller;

// Model description at:
// https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2328952849/Hente+informasjon+fra+PVK+om+innbyggers+personverninnstillinger

public class ApiResponseHentInnbyggere
{
	public string? definisjonGuid { get; set; } // GUID GUID
	public string? definisjonNavn { get; set; } // Reservasjon til Norsk proton- og str�leterapiregister
	public string? partKode { get; set; } //  norpreg
	public string? typePi { get; set; } // samtykke, reservasjon, tilgangsbegrensning
	public Meta ReFasteMetadata { get; set; } // faste metedata for 
    public string? pagingReference { get; set; } // Dersom den har verdien 0, trenger det ikke � gj�res flere kall. Dersom annen verdi, m� det gj�res etterf�lgende kall med angitt pagingReference.

    public List<Pi>? personvernInnstillinger { get; set; }
}

public class Pi
{
	public string? innbyggerFnr { get; set; } // F�dselsnummer eller D-nummer
	public int? sekvensnummer { get; set; } // Sekvensnummer p� innbyggerens innstilling for denne definisjonen -- starter p� 1, og �kes for hver gang den endres
	public DateTime opprettetTidspunkt { get; set; } // N�r PI f�rste gang ble satt
	public DateTime sistEndretTidspunkt { get; set; } // N�r PI sist ble endret
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