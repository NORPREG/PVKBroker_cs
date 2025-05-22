using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

using PvkBroker.Configuration;
using PvkBroker.Pvk;
using PvkBroker.Kodeliste;
using PvkBroker.Redcap;

public class OrchestratorService : BackgroundService
{
    private readonly Configuration _configuration;
    private readonly PvkCaller _pvkCaller;
    private readonly Kodeliste _kodeliste;
    private readonly Redcap _redcap;

    private Timer? _timer;

    public OrchestratorService(Configuration configuration, PvkCaller pvkCaller, Kodeliste kodeliste, Redcap redcap)
    {
        _configuration = configuration;
        _pvkCaller = pvkCaller;
        _kodeliste = kodeliste;
        _redcap = redcap;
    }

    protected overried Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Starting OrchestratorService...");
        // Set up a timer to call the Pvk API every day
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        try
        {
            Log.Information("OrchestratorService is doing work...");

            // Pack the business logic loop into another function?

            // Call the Pvk API
            var pvkData = _pvkCaller.GetPvkData();
            // Process the data with Kodeliste
            var processedData = _kodeliste.ProcessData(pvkData);

            foreach (var patient in processedData)
            {
                var patientKey = _kodeliste.GetPatientKey(patient);
                var registerNavn = _kodeliste.GetRegisterKey(patient);
                _redcap.ExportAndImport(patientKey, registerNavn);
            }
            
            Log.Information("OrchestratorService completed work successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred in OrchestratorService: {@ex}", ex);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping OrchestratorService...");
        _timer?.Change(Timeout.Infinite, 0);
        return base.StopAsync(cancellationToken);
    }

}