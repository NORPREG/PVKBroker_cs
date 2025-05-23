# PVK Broker -- C# edition using HelseID (sample) library for Oauth workflow
Gateway broker for communications between Helse Norge / Norsk Helsenett (NHN) Personvernkomponenten (PVK) and the Norwegian Registry for Radiation- and Proton Therapy (PROTONOR).
Also contains code to perform data transfer between local REDCap quality registries and the national REDCap instance when consent is available.
In the last operation, pseudonymization is performed.

## Project purpose
The main goals of this repo are:
* Set up authentications between the client and NHN (OAuth2)
* Poll a local database to find fnr of patients to retrieve data from
* Send SamtykkeForespørsel for the relevant patients to the PVK endpoint at NHN
* Update the Kodeliste MySQL (consentStatus from Ukjent to Kjent)
* Pull a JSON data object of consenting patients from the relevant local REDCap quality registry using REST API
* Pseudonymize the data by removing directly identifiable inforamtion, and add a pseudonymized index from Kodelisten
* Push the JSON data object of the patient to the national REDCap using REST API.

## Some documentation below for the PYTHON version of the project, update this for the C# version
A few definitions:
* `definisjonGuid`: The ID of a specific consent question. Several consent questions can be included in a project. The type of a `definisjonGuid` can be both consent, reservation and sperre (for innsyn). In test, the `definisjonGuid` for "Samtykke 1" is `2c11f0ca-7270-43f1-a473-bac325feb3f6`.
* `definisjonNavn`: The friendly name of the specific consent question at Helse Norge. In test it is `Proton- og stråleregister samtykke`.
* `klientGuid`: The ID of this client program. In test this is `5aee1830-29f5-4f7f-8927-a20ee9fb4125`.
* `partKode`: The short name of the project. In test this is `prostraa`, however it should be changed to `protonor`
* `typePi`: The type of `definisjonGuid`. In this project it is `samtykke`. 
* `inkluderingType`: Relates to `SettInnbyggersInkludering`
	* `inkludert`: Included in research project or screening by law or "forskrift"
	* `deltager`: same as `inkludert`, but requires consent.
	* `oppfort`: Included in registry. Can both be through law/"forskrift" or consent

## External documentation
For general PVK documentation, [see this page at Norsk Helsenett](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/376602660/Generelt+om+PVK).

For the Oauth2 documentation, [see this page at Norsk Helsenett](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1368752157/Client+Assertion).

For `PyOIDC` documentation, [see this page](https://pyoidc.readthedocs.io/en/latest/).

## Oauth2 authentication procedure

In this, refer to the EDI 2.0 HelseID procedure [documented in the DIPS Broker repo](https://github.com/NPSReg/DIPSBroker).

Before attempting to poll the PVK API using, an `access token` from the STS service of NHN needs to be retrieved.
The procedure, implemented using the `PyOIDC`, `PyJWT` (and `requests`) uses the following concepts from the `client credentials` workflow of [RFC6749](https://datatracker.ietf.org/doc/html/rfc6749):
* Client Credentials Grant
* JSON Web Token (JWT) credentials signed with known RSA key pair
* Access Token
* NO discovery metadata caching (PVK doesn't support endpoint for the token service)

The modules in `Broker/auth.py` contains the neccessary code to perform the handshake.

The procedure is implemented as follows:
1. A `AuthClient` object is instantiated without arguments.
2. The `cache_metadata()` function is defined, but as the actual discovery endpoint does not exist the OIDCPROV endpoint is used.
3. The initialization generates a `PyOIDC.Client` object, configured with the required `provider_info` (i.e., `issuer`, `token_endpoint`, `client_id`, `grant_type` etc.). Configuartion items that differ between the token endpoint and the OIDCPROV endpoint are hardcoded (such as `token_endpoint`, `audience`, `grant_type` etc.).
4. An RSA key pair (`self.key`) is loaded using the `AuthClient.load_keys()` method. NHN knows about the public part of this pair.
5. The `AuthClient.get_cached_access_token()` method is called, where the following happens:
	1. The method looks for a cached and not-expired `access_token`. If found, it is returned.
	2. If not found, a new one is generated (see the following chapters for payload contents):
		1. The `AuthClient.create_signed_jwt()` generates a signed JWT 
		2. The `AuthClient._get_access_token()` inserts the JWT together with assertion metadata into a REST API call to STS.
	3. The `access_token` is received, ready for use in PVK messages.

### JWT Contents
```
headers = {
	'alg': 'PS512',
	'typ': 'jwt',
	'kid': self.key.key_id
}

payload = {
	'iss': client_id,
	'sub': client_id,
	'aud': sikkerhet.helsenorge.no,
	'jti': rndstr(),
	'exp': utc_time_sans_frac + 60,
	'iat': utc_time_sans_frac,
	'nbf': utc_time_sans_frac
}
```

The key is signed and serialized using
```
signed_jwt = jwcrypto.jwt.JWT(header=headers, claims=payload)
signed_jwt.make_signed_token(self.key).serialize()
```

### Contents of final POST call to STS
The final `requests` POST to the security service at NHN (STS) is the built as following:
```
payload = {
	'grant_type': 'client_credentials',
	'client_assertion_type': 'urn:ietf:params:oauth:client-assertion-type:jwt-bearer',
	'client_id': client_id,
	'client_assertion': signed_jwt
}

res = requests.post(token_endpoint, data=payload)
access_token = res.json()['access_token']
```

### Comments to the Oauth2 procedure
Per now, the payload is generated manually with help from `AuthClient`. [In PyOIDC there are methods](https://pyoidc.readthedocs.io/en/latest/examples/cookbook.html) for building the JWT automatically, however for any adjustments to the payload / headers a `client_assertion` custom method can be given to ```PyOIDC.Client.do_access_token_request()```:
```
kwargs = dict(algorithm="RS256", authn_endpoint='token',
              authn_method="private_key_jwt",
              client_assertion=custom_assertion)
[...]
client.do_access_token_request(**kwargs)
```
However, this procedure is poorly documented, so we have rather implemented a custom function `._get_access_token()` to perform the JWT payload assembly.

### Differentiation between PVK and HelseID access token procedure
* No `DPoP` header is included in the PVK procedure (performed to stop access token theft by binding the access token to a single-use key)
* As a consequence, no single-use RSA key pair is generated for that purpose
* No `nonce` procedure is used in the PVK procedure (done to stop replay attacks)
* The token endpoint is different

## PVK message procedure
The `Broker/session.py` module contains a class to perform this task.
Here, an `AuthClient` is instantiated and an `access token` is received.

A `PVKSession` is instantiated with the `access token`. Now, the following endpoints / methods are available:
* `PVKSession.SjekkInnbyggersPiStatus(fnr)`: Checks the consent status for a single `definisjonGuid` for a single person
	* `payload = {'innbyggerFnr:fnr, 'definisjonGuid':, 'definisjonsNavn':, 'partKode':}`
* `PVKSessionHentInnbyggersPiForPart()`: Checks the consent status for all `definisjonGuid` in this `partKode` for a single person
	* `payload = {'innbyggerFnr:, 'partKode':}`
* `PVKSession.HentInnbyggereAktivePiForDefinisjon()`: Retrieve all consenting persons for the `definisjonGuid`.
	* `payload = {'definisjonGuid':, 'partKode':, 'pagingReference':}`
	* The paging reference is a incrementing index for repeat requests, set to 0 or the received value.
* `PVKSession.SettInnbyggersPersonvernInnstilling(fnr, consent_status)`: Set consent status for single person
	* `payload = {'innbyggerFnr:fnr, 'definisjonGuid':, 'definisjonsNavn':, 'partKode':, 'typePi':, 'aktiv': consent_status}`
* `PVKSession.SettInnbyggersInkludering(fnr, inkluderingType, aktiv)`: Sett inclusion in research project, screening program or registry. See the different `inkluderingType` above.
	* `payload = {'innbyggerFnr:fnr, 'partKode':, 'inkluderingType':inkluderingType, 'aktiv': aktiv}`

In all cases, the following header must be included:
```
header = {
	'Authorization':  f'Bearer {access_token}', 
	'Content-Type': 'application/json',
}
```

and the endpoint URLs is given by the endpoint names above (in test):
```
https://eksternapi-helsenett.hn2.test.nhn.no/personvern/PersonvernInnstillinger/{endpoint_name}/v2
```
except for the `SettInnbyggersInkludering`, which is available at
```
https://eksternapi-helsenett.hn2.test.nhn.no/personvern/v1/settinnbyggersinkludering
```

An example use would be (where `FNR_LINE = 13116900216` is Line Danser, the test patient):
```
auth_client = AuthClient()
access_token = auth_client.get_cached_access_token()
pvk_session = PVKSession(access_token)
pvk_session.SettInnbyggersInkludering(FNR_LINE, "oppfort", True)
response = pvk_session.SjekkInnbyggersPiStatus(FNR_LINE)
print(response.json()['aktiv'])
```

## Workflow when syncing against PVK 
* Get list of all reserved patients from PVK
* Loop through PatientID table
     * For each PatientID table entry, decrypt the fnr
     * For each fk_patient_key, decrypt the lastest PvkEvent->is_reserved_aes
* For each patient with is_reserved = 1 and not in lastest reserved patients from PVK
     * Remove from NORPREG with RedcapRemovePatient(patient_key)
* For each patient with is_reserved = 0 and in lastest reserved patients from PVK
     * Add to NORPREG with RedcapAddPatient(patient_key)

## TODO
* Build REDCap integration (using modules from DICOM Broker)
* Add business logic for patient transfer
* Add pseudonymization procedure
* Build Kodeliste SQL integration (ORM / SQLModel)
* Set up as windows service (NSSM?)


## Logging
The python library `logging` used configured with each module, and dumps messages to `PVKBroker.log`.
We plan to adjust the output to JSON and push all messages to an ELK stack.

## REDCap REST API
The REDCap communications are handled through its REST API. An persistent access token per HF is required for this procedure. The API is well documented through various [REDCap documentation](https://ws.engr.illinois.edu/sitemanager/getfile.asp?id=3112), and the   `Interfaces/REDCapInterface.py` module implements these.
