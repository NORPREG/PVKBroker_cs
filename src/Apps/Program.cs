using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PvkBroker.AccessToken;
using PvkBroker.Kodeliste;
using Microsoft.EntityFrameworkCore;
using System;
using Serilog;

var services = new ServiceCollection();

// Konfigurer evt. konstanter direkte eller via .env/appsettings
services.AddSingleton<ConfigurationValues>();

// DI for KodelisteDbContext
services.AddDbContext<KodelisteDbContext>(options =>
{
    string Server = ConfigurationValues.KodelisteServer;
    string DatabaseName = ConfigurationValues.KodelisteDbName;
    string UserName = ConfigurationValues.KodelisteUsername;
    string Password = ConfigurationValues.KodelistePassword;
    string connString = $"Server={Server};Database={DatabaseName};UserName={UserName};Password={Passoword};"
                    options.UseMySql(connstring, ServerVesion.AutoDetect(connString));
});

// DI for caching service
services.AddSingleton<PatientIDClassService>();
services.AddHttpClient();

// DI for tokenhenting
services.AddSingleton<ClientConfigurator>();
services.AddSingleton(provider =>
{
    var configurator = provider.GetRequiredService<ClientConfigurator>();
    return configurator.ConfigureClient();
});
services.AddSingleton<AccessTokenCaller>();

// Bygg
var serviceProvider = services.BuildServiceProvider();

try
{
    // Her tester du koden som i et kontrollert script
    var tokenCaller = serviceProvider.GetRequiredService<AccessTokenCaller>();
    var token = await tokenCaller.FetchTokenAsync(); // Eksempel

    Console.WriteLine("Access token hentet: " + token);

    var cache = serviceProvider.GetRequiredService<PatientIDClassService>();
    var result = cache.GetPatientIdsByFnr("12345678901");
    Console.WriteLine($"Antall funnet: {result.Count()}");
}
catch (Exception ex)
{
    Console.WriteLine("Feil: " + ex.Message);
}
