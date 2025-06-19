using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;

using PvkBroker.Configuration;
using PvkBroker.Pvk.ApiCaller;
using PvkBroker.Pvk.TokenCaller;
using PvkBroker.Kodeliste;
using PvkBroker.Redcap;
using PvkBroker.Tools;

namespace PvkBroker.Service;
public class OrchestratorService : BackgroundService
{
    private readonly PvkCaller _pvkCaller;
    private readonly KodelisteInterface _kodeliste;
    private readonly RedcapInterface _redcap;
    private readonly AccessTokenCaller _accessTokenCaller;
    private readonly Encryption _encryption;
    private readonly Orchestrations _orchestration;

    private Timer? _timer;

    public OrchestratorService(
        PvkCaller pvkCaller,
        AccessTokenCaller accessTokenCaller,
        KodelisteInterface kodelisteInterface,
        RedcapInterface redcapInterface,
        Encryption encryption,
        Orchestrations orchestrations
    )

    {
        _pvkCaller = pvkCaller;
        _accessTokenCaller = accessTokenCaller;
        _kodeliste = kodelisteInterface;
        _redcap = redcapInterface;
        _encryption = encryption;
        _orchestration = orchestrations;

        SetupLogging.Initialize();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("Starting PvkBroker OrchestratorService...");

        var timer = new PeriodicTimer(TimeSpan.FromHours(ConfigurationValues.PvkSyncTimeInHours));

        do
        {
            await DoWorkAsync();
        }

        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DoWorkAsync()
    {
        try
        {
            _kodeliste.ReloadCache();

            await _orchestration.HandleNewPatients();

            List<SimplePvkEvent> newPvkEvents = await _orchestration.CallPvkAndParseResponse();

            // Get reservations from Kodeliste database BEFORE newest sync
            var reservationDelta = _orchestration.CompareCurrentReservationWithNewPvkEvents(newPvkEvents);

            if (reservationDelta.NewReservations.Count == 0 && reservationDelta.WithdrawnReservations.Count == 0)
            {
                Log.Information("No new or withdrawn reservations found. Skipping further processing.");
                return;
            }

            // Make row in PvkSync table, get index for linking PvkEvent rows
            string pvkSyncId = _kodeliste.CreatePvkSync(reservationDelta);

            await _orchestration.HandleNewReservations(reservationDelta.NewReservations, pvkSyncId);
            await _orchestration.HandleWithdrawnReservations(reservationDelta.WithdrawnReservations, pvkSyncId)

            Log.Information("OrchestratorService completed work successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unknown error occurred in OrchestratorService: {@ex}", ex);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping OrchestratorService...");
        _timer?.Change(Timeout.Infinite, 0);
        return base.StopAsync(cancellationToken);
    }
}