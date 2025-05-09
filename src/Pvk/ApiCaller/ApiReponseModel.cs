namespace PvkBroker.Pvk.ApiCaller;

// need new data model to adhere to v2...
// https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2328952849/Hente+informasjon+fra+PVK+om+innbyggers+personverninnstillinger

public class ApiReponseHentInnbyggere
{
	public string? definisjonGuid { get; set; }
	public string? definisjonNavn { get; set; }
	public string? partKode { get; set; }
	public string? typePi { get; set; }
	public string? pagingReference { get; set; }

	public List<Pi>? personvernInnstillinger { get; set; }
}

public class Pi
{
	public string? innbyggerFnr { get; set; }

	// Only relevant with access limitations (tilgangsbegrensning)
	public List<innbyggerTbMetadata>? innbyggerTbMetadata { get; set; }
}

public class innbyggerTbMetadata
{
	public string? type;
	public string? nummer;
	public string? navn;
}