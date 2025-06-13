# PVK Broker -- C# edition using HelseID (sample) library for Oauth workflow
Gateway broker for communications between Helse Norge / Norsk Helsenett (NHN) Personvernkomponenten (PVK) and the Norwegian Registry for Radiation- and Proton Therapy (PROTONOR).
Also contains code to perform data transfer between local REDCap quality registries and the national REDCap instance when consent is available.

## Project purpose
The main goals of this repo are:
* Set up a running service for perform the following operations
* Set up authentications between the client and NHN (OAuth2 HelseID)
* Set up a NORPREG - REDCap integration for patient data transfer (REST API)
* Pull all reserved patients from PVK using the `HentInnbyggereAktivePiForDefinisjon` endpoint. Sync with the Kodeliste database. 
  * Remove newly reserved patients from NORPREG
  * Add patients with withdrawn reservation
  * Add patients without reservation after a quarantine period
* Add the PvkSync event to the Kodeliste database
* Also supports adding manually reserved patients to PVK using the `SettInnbyggersPersonvernInnstilling` endpoint

## External documentation
For general PVK documentation, [see this page at Norsk Helsenett](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/376602660/Generelt+om+PVK).

For the Oauth2 documentation, [see this page at Norsk Helsenett](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1368752157/Client+Assertion).

## Solution Overview

The solution is organized into several projects, each with a clear responsibility:

- **Service**: Contains the main orchestration logic for the NT Service and dependency injection setup.
- **Pvk**: Handles PVK API integration, token management, and related business logic.
- **Kodeliste**: Manages code lists, patient ID caching, and database access. See [here for the documented data model](https://norpreg-data-model.readthedocs.io/en/latest/models.html#modell-for-kodeliste-for-koblingsnokler)
- **Redcap**: Integrates with REDCap APIs for patient inclusion in NORPREG. See [here for the documented REDCap data model](https://norpreg-data-model.readthedocs.io/en/latest/models.html)]
- **Tools**: Shared utilities, encryption, and orchestration helpers.
- **ConsoleApp**: Barebones command line application for adhoc patient data input and integration testing
- **Configuration**: Centralized configuration and settings.
- **HelseID.ClientCredentials**: Handles HelseID OAuth2 client credentials flow.
- **HelseID.Common**: Shared code and interfaces, including HelseID endpoint discovery and token handling.

---

## Usage Examples

- **Fetch all PVK events:**
    ```sh
    dotnet run --project src\ConsoleApp\PvkBroker.ConsoleApp.csproj
    ```
- **Set definitions from a JSON file:**
    ```sh
    dotnet run --project src\ConsoleApp\PvkBroker.ConsoleApp.csproj path/to/yourfile.json
    ```

---
j

## Dependencies

- [System.CommandLine](https://www.nuget.org/packages/System.CommandLine)
- [MySql.Data](https://www.nuget.org/packages/MySql.Data)
- [Microsoft.EntityFrameworkCore.Relational](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational)
- [Microsoft.EntityFrameworkCore.SqlServer](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer)
- [Serilog](https://www.nuget.org/packages/Serilog)
- [Serilog.Sinks.File](https://www.nuget.org/packages/Serilog.Sinks.File)
- [HelseId.Samples.Common](lib/HelseId.Samples.Common.dll)

## License

[MIT](LICENSE)