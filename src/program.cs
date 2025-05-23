using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Runtime.Versioning;

using PvkBroker.Configuration;
using PvkBroker.Pvk;
using PvkBroker.Kodeliste;
using PvkBroker.Redcap;
using PvkBroker.Tools;

[SupportedOSPlatform("windows")]
public class Program
{
    public static void Main(string[] args)
    {
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
            {

                services.AddHostedService<OrchestratorService>();

                services.AddDbContext<KodelisteDbContext>(options =>
                {
                    string Server = ConfigurationValues.KodelisteServer;
                    string DatabaseName = ConfigurationValues.KodelisteDbName;
                    string UserName = ConfigurationValues.KodelisteUsername;
                    string Password = ConfigurationValues.KodelistePassword;
                    string connString = $"Server={Server};Database={DatabaseName};UserName={UserName};Password={Passoword};"
                    options.UseMySql(connstring, ServerVesion.AutoDetect(connString));
                });

                services.AddSingleton<PvkBroker.Configuration.ConfigurationValues>();
                services.AddSingleton<PvkBroker.Pvk.TokenCaller.AccessTokenCaller>();
                services.AddSingleton<PvkBroker.Pvk.ApiCaller.PvkCaller>();
                services.AddSingleton<PvkBroker.Kodeliste.KodelisteInterface>();
                services.AddSingleton<PvkBroker.Redcap.RedcapInterface>();
                services.AddSingleton<PvkBroker.Tools.ReservationComparator>();
                services.AddSingleton<PvkBroker.Kodeliste.Encryption>();
                services.AddSingleton<PvkBroker.Tools.Orchestrations>();
                services.AddSingleton<PvkBroker.Kodeliste.PatientIDCacheService>();
            })
             .Build()
             .Run();
    }
}