# PvkBroker.ConsoleApp

A .NET 9 console application for orchestrating and managing PVK (Pasient- og VareKatalog) broker operations, including database synchronization, token management, and integration with external services such as HelseID and Redcap.

## Features

- Barebones command line application for adhoc patient data input and integration testing
- - Orchestrates PVK reservation management.
- Integrates with HelseID for secure authentication and token handling using the HelseID sample library.
- Supports MySQL and SQL Server via Entity Framework Core.
- Configurable logging with Serilog.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- MySQL or SQL Server database (for KodelisteDbContext)
- Access to HelseID and Pvk endpoints (if required by your orchestration)

## Getting Started

### 1. Clone the Repository
git clone https://github.com/NORPREG/PVKBroker_cs
cd <repo-root>

### 2. Configure the Application

Edit the configuration files or environment variables as needed. Key configuration values are managed in:

- `src/Configuration/ConfigurationValues.cs`

Set up connection strings and credentials for your database and external services.

### 3. Build the Project
`dotnet build src\ConsoleApp\PvkBroker.ConsoleApp.csproj`


### 4. Run the Application
`dotnet run --project src\ConsoleApp\PvkBroker.ConsoleApp.csproj`

## Project Structure

- `Program.cs` – Entry point, sets up dependency injection and orchestrates main operations.
- `Orchestrations/Orchestrations.cs` – Contains orchestration logic for PVK operations.
- `Configuration/` – Application and service configuration.
- `Pvk/`, `Kodeliste/`, `Redcap/`, `Tools/` – Core business logic and integrations.

## Dependencies

- [System.CommandLine](https://www.nuget.org/packages/System.CommandLine)
- [MySql.Data](https://www.nuget.org/packages/MySql.Data)
- [Microsoft.EntityFrameworkCore.Relational](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational)
- [Microsoft.EntityFrameworkCore.SqlServer](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer)
- [Serilog](https://www.nuget.org/packages/Serilog)
- [Serilog.Sinks.File](https://www.nuget.org/packages/Serilog.Sinks.File)
- [HelseId.Samples.Common](lib/HelseId.Samples.Common.dll)

## Logging

Logging is configured via Serilog. Log files are written to the `LogFiles` directory by default. Adjust settings in `src/Configuration/SetupLogging.cs` as needed.

## Contributing

Contributions are welcome! Please open issues or submit pull requests for improvements or bug fixes.

## License

[MIT](../LICENSE)
