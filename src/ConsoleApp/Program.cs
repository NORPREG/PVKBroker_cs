using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PvkBroker.Configuration;
using PvkBroker.Pvk.ApiCaller;
using PvkBroker.Pvk.TokenCaller;
using PvkBroker.Kodeliste;
// using PvkBroker.Redcap;
using PvkBroker.Tools;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PvkBroker.Datamodels;
using static IdentityModel.OidcConstants;

namespace PvkBroker.ConsoleApp
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await MainAsync(args);
        }

        static async Task MainAsync(string[] args)
        {
            SetupLogging.Initialize();
            var services = SetupServices();
            var serviceProvider = services.BuildServiceProvider();

            // Ensuring database exists
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<KodelisteDbContext>();
                db.Database.EnsureCreated(); // Shift to migration mode later on
            }

            var _kodeliste = serviceProvider.GetRequiredService<KodelisteInterface>();
            var _orchestration = serviceProvider.GetRequiredService<Orchestrations>();

            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Programmet kjørt uten argumenter, henter PVK hendelser for alle innbyggere med reservasjon.");

                    _kodeliste.AddPatient("Test 1", "13116900216");
                    _kodeliste.AddPatient("Test 2", "01011158117");
                    _kodeliste.AddPatient("Test 3", "01011270391");
                    _kodeliste.AddPatient("Test 4", "05031167584");
                    _kodeliste.AddPatient("Test 6", "12057900499");

                    List<SimplePvkEvent> pvkResponse = new List<SimplePvkEvent> { };

                    // List<SimplePvkEvent> pvkResponse = await _orchestration.CallPvkAndParseResponse();

                    Console.WriteLine("Antall hendelser i PVK: " + pvkResponse.Count);

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    string jsonOutput = JsonSerializer.Serialize(pvkResponse, options);
                    if (pvkResponse.Count > 0)
                    {
                        Console.WriteLine("Alle PVK-hendelser i JSON-format:");
                        Console.WriteLine(jsonOutput);
                    }
                }

                else if (args.Length == 1)
                {
                    Console.WriteLine("Programmet kjørt med ett argument, henter data fra angitt JSON fil og setter status deretter.");

                    string filepath = args[0];
                    ApiResult<string> pvkResponse = await _orchestration.CallPvkAndSetDefinition(filepath);
                    // Console.WriteLine("\nPVK success: " + pvkResponse.Success);
                    // Console.WriteLine("PVK response: " + pvkResponse.Data);

                    if (pvkResponse.Success)
                    {
                        Console.WriteLine("Vellykket endring i PVK.");
                    }

                    else
                    {
                        Console.WriteLine("Feil ved setting av PVK hendelser.");
                    }

                }
                else
                {
                    Console.WriteLine("Ugyldige argumenter. Bruk en JSON filsti for å sette definisjon eller uten argument for å hente ut alle hendelser.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Feil: " + ex.Message);
            }
        }

        public static ServiceCollection SetupServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<AccessTokenCaller>();
            services.AddSingleton<PvkCaller>();
            services.AddSingleton<Encryption>();
            services.AddSingleton<Orchestrations>();
            services.AddSingleton<KodelisteInterface>();
            services.AddSingleton<PatientIDCacheService>();
            services.AddHttpClient();

            if (ConfigurationValues.UseSqlite)
            {
                services.AddDbContext<KodelisteDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={ConfigurationValues.SqliteDatabaseFile};Cache=Shared;");
                });
            }
            else
            {
                services.AddDbContext<KodelisteDbContext>(options =>
                {
                    string Server = ConfigurationValues.KodelisteServer;
                    string DatabaseName = ConfigurationValues.KodelisteDbName;
                    string UserName = ConfigurationValues.KodelisteUsername;
                    string Password = ConfigurationValues.KodelistePassword;
                    string connString = $"Server={Server};Database={DatabaseName};User Id={UserName};Password={Password};";
                    options.UseMySql(connString, ServerVersion.AutoDetect(connString));
                });
            }

            return services;
        }
    }
}

// Inline DI for KodelisteDbContext

// DI for other services

// 
// services.AddSingleton<PvkBroker.Redcap.RedcapInterface>();

// Bygg

// 
// _kodeliste.ReloadCache();
// Get reservations from Kodeliste database BEFORE newest sync
// var reservationDelta = _orchestration.CompareCurrentReservationWithNewPvkEvents(patientReservations, newPvkEvents);

// Make row in PvkSync table, get index for linking PvkEvent rows
// string pvkSyncId = _kodeliste.CreatePvkSync(reservationDelta);

// await _orchestration.HandleNewReservations(reservationDelta.NewReservations, pvkSyncId);
// await _orchestration.HandleWithdrawnReservations(reservationDelta.WithdrawnReservations, pvkSyncId);
// await _orchestration.HandleNewPatients();