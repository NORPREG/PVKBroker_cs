using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
public class Program
{
    public stativ void Main(string[] args)
    {
        Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
            {

                services.AddHostedService<OrchestratorService>();

                // Register your services here
                services.AddSingleton<PvkBroker.Configuration>();
                services.AddSingleton<PvkBroker.PvkConfiguration>();
                services.AddSingleton<PvkBroker.Kodeliste>();
                services.AddSingleton<PvkBroker.Redcap>();
            })
            .Build()
            .Run();
    }
}