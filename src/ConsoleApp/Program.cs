using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using PvkBroker.Configuration;
using PvkBroker.Pvk.ApiCaller;
using PvkBroker.Pvk.TokenCaller;
// using PvkBroker.Kodeliste;
// using PvkBroker.Redcap;
using PvkBroker.Tools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

var services = new ServiceCollection();
SetupLogging.Initialize();

// Inline DI for KodelisteDbContext

/*
 * Commented out for gradual bootstrapping of the application
 * 
services.AddDbContext<KodelisteDbContext>(options =>
{
    string Server = ConfigurationValues.KodelisteServer;
    string DatabaseName = ConfigurationValues.KodelisteDbName;
    string UserName = ConfigurationValues.KodelisteUsername;
    string Password = ConfigurationValues.KodelistePassword;
    string connString = $"Server={Server};Database={DatabaseName};Uid={UserName};Password={Password};";
    options.UseMySql(connString, ServerVersion.AutoDetect(connString));
});
*/

// DI for other services
services.AddSingleton<PvkBroker.Pvk.TokenCaller.AccessTokenCaller>();
services.AddSingleton<PvkBroker.Pvk.ApiCaller.PvkCaller>();
// services.AddSingleton<PvkBroker.Kodeliste.KodelisteInterface>();
// services.AddSingleton<PvkBroker.Redcap.RedcapInterface>();
services.AddSingleton<PvkBroker.Tools.Encryption>();
services.AddSingleton<PvkBroker.ConsoleApp.Orchestrations>();
// services.AddSingleton<PvkBroker.Kodeliste.PatientIDCacheService>();
services.AddHttpClient();

// Bygg
var serviceProvider = services.BuildServiceProvider();

// var _kodeliste = serviceProvider.GetRequiredService<KodelisteInterface>();
var _orchestration = serviceProvider.GetRequiredService<PvkBroker.ConsoleApp.Orchestrations>();

try
{
    // _kodeliste.ReloadCache();

    List<SimplePvkEvent> pvkResponse = await _orchestration.CallPvkAndParseResponse();

    Console.WriteLine("Antall hendelser i PVK: " + pvkResponse.Count);
    Console.WriteLine("Første hendelse: " + pvkResponse[0].ToString()); 

    // Get reservations from Kodeliste database BEFORE newest sync
    // var reservationDelta = _orchestration.CompareCurrentReservationWithNewPvkEvents(patientReservations, newPvkEvents);

    // Make row in PvkSync table, get index for linking PvkEvent rows
    // string pvkSyncId = _kodeliste.CreatePvkSync(reservationDelta);

    // await _orchestration.HandleNewReservations(reservationDelta.NewReservations, pvkSyncId);
    // await _orchestration.HandleWithdrawnReservations(reservationDelta.WithdrawnReservations, pvkSyncId);
    // await _orchestration.HandleNewPatients();
}
catch (Exception ex)
{
    Console.WriteLine("Feil: " + ex.Message);
}
