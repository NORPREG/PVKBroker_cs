using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;
using System.Linq;

using PvkBroker.Datamodels;
using PvkBroker.Configuration;
using PvkBroker.Pvk.ApiCaller;
using PvkBroker.Pvk.TokenCaller;
using PvkBroker.Kodeliste;
using PvkBroker.Redcap;

namespace PvkBroker.ConsoleApp
{
    public class Orchestrations
    {
        // Handling singleton dependency injection

        private readonly RedcapInterface _redcap;
        private readonly KodelisteInterface _kodeliste;
        private readonly AccessTokenCaller _accessTokenCaller;
        private readonly PvkCaller _pvkCaller;

        public Orchestrations(
            RedcapInterface redcap,
            KodelisteInterface kodeliste,
            AccessTokenCaller accessTokenCaller,
            PvkCaller pvkCaller)
        {
            _redcap = redcap;
            _kodeliste = kodeliste;
            _accessTokenCaller = accessTokenCaller;
            _pvkCaller = pvkCaller;
        }

        public async Task<List<SimplePvkEvent>> CallPvkAndParseResponse()
        {

            string? accessToken = await _accessTokenCaller.GetAccessToken();

            if (string.IsNullOrEmpty(accessToken))
            {
                Log.Error("Access token is null or empty. Cannot call PVK API.");
                throw new InvalidOperationException("Access token is null or empty. Cannot call PVK API.");
            }

            // sleep 1 sec to ensure token is ready
            await Task.Delay(1000);

            List<SimplePvkEvent> newPvkEvents = await _pvkCaller.CallApiHentInnbyggereAktivePiForDefinisjon(accessToken);

            return newPvkEvents;
        }

        public async Task<ApiResult<string>> CallPvkAndSetDefinition(string jsonPath)
        {
            string? accessToken = await _accessTokenCaller.GetAccessToken();

            // sleep 1 sec to ensure token is ready
            await Task.Delay(1000);

            if (string.IsNullOrEmpty(accessToken))
            {
                Log.Error("Access token is null or empty. Cannot call PVK API.");
                throw new InvalidOperationException("Access token is null or empty. Cannot call PVK API.");
            }

            ApiResult<string> response = await _pvkCaller.CallApiSettInnbyggersPersonvernInnstilling(accessToken, jsonPath);
            return response;
        }

        public async Task HandleNewReservations(List<SimplePvkEvent> newReservations, int pvkSyncId) { 
            Log.Information("Found {NewReservations} new reservation events in PVK sync", newReservations.Count);
            foreach (SimplePvkEvent patient in newReservations)
            {
                try {
                     string? registerName = _kodeliste.GetRegisterName(patient.PatientKey);

                    if (patient.PatientKey == null)
                    {
                        Log.Warning("PatientKey is null for a new reservation. Skipping this entry.");
                        continue;
                    }

                    _kodeliste.AddPvkEvent(patient.PatientKey, true, pvkSyncId);
                    await _redcap.RemovePatient(patient.PatientKey);
                    Log.Information("Removing patient {patient} from NORPREG due to reservation", patient.PatientKey);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error handling new patient reservation at patient {patient}", patient.PatientKey);
                    continue;
                }
            }
        }

        public async Task HandleWithdrawnReservations(List<string> withdrawnReservations, int pvkSyncId) {
            Log.Information("Found {WithdrawnReservations} withdrawn reservation events in PVK sync", withdrawnReservations.Count);
            foreach (var patientKey in withdrawnReservations)
            {
                try
                {
                    var registerName = _kodeliste.GetRegisterName(patientKey);
                    _kodeliste.AddPvkEventWithdrawn(patientKey, pvkSyncId);

                    if (registerName == null)
                    {
                        Log.Warning("Register name is null for patient {patient}. Skipping this entry.", patientKey);
                        continue;
                    }

                    await _redcap.ExportAndImportAsync(patientKey, registerName);
                    Log.Information("Adding patient {patient} to NORPREG due to withdrawn reservation", patientKey);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error handling withdrawn patient reservation at patient {patient}", patientKey);
                    continue;
                }
            }
        }

        public async Task HandleNewPatients()
        {
            var quarantinePeriod = DateTime.Now.AddDays(-ConfigurationValues.QuarantinePeriodInDays);
            List<string> patientKeysInRedcap = await _redcap.GetAllRecordIdsAsync();
            List<PatientReservation> updatedPatientsReservations = _kodeliste.GetPatientReservations();

            foreach (var patient in updatedPatientsReservations)
            {
                if (patient.PatientKey == null)
                {
                    Log.Warning("PatientKey is null for a patient reservation. Skipping this entry.");
                    continue;
                }

                if (!patient.IsReserved && patient.DateAdded < quarantinePeriod && !patientKeysInRedcap.Contains(patient.PatientKey))
                {
                    try
                    {
                        var registerName = _kodeliste.GetRegisterName(patient.PatientKey);

                        if (registerName == null)
                        {
                            Log.Warning("Register name is null for patient {patient}. Skipping this entry.", patient.PatientKey);
                            continue;
                        }

                        await _redcap.ExportAndImportAsync(patient.PatientKey, registerName);
                        Log.Information("Adding patient {patient} to NORPREG after quarantine period", patient.PatientKey);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error handling patient {patient} after quarantine period", patient.PatientKey);
                        continue;
                    }
                }
            }
        }

        public ReservationDelta CompareCurrentReservationWithNewPvkEvents(List<SimplePvkEvent> newPvkEvents)
        {

            var reservationDelta = new ReservationDelta();
            List<PatientReservation> currentReservations = _kodeliste.GetPatientReservations();

            if (currentReservations.Count == 0)
            {
                Log.Information("No current reservations found in the database.");
                return reservationDelta; // Return empty delta if no current reservations
            }

            // Create a dictionary for fast lookup of newPvkEvents by PatientKey
            // PatientKey is added post hoc so filter out any nulls here to avoid exceptions

            var newPvkEventsLookup = newPvkEvents
                .Where(e => e.PatientKey != null) 
                .ToDictionary(e => e.PatientKey!);

            var currentReservationsLookup = currentReservations
                .Where(r => r.PatientKey != null)
                .ToDictionary(r => r.PatientKey!);

            // Find new reservations
            foreach (var newPvkEvent in newPvkEvents)
            {
                
                // PatientKey cannot be zero here since we just filtered them out
                if (!currentReservationsLookup.ContainsKey(newPvkEvent.PatientKey!))
                {
                    PatientReservation newReservation = new PatientReservation
                    {
                        PatientKey = newPvkEvent.PatientKey,
                        IsReserved = newPvkEvent.IsReserved,
                        EventTime = newPvkEvent.EventTime,
                        DateAdded = DateTime.Now
                    };

                    reservationDelta.NewReservations.Add(newReservation);
                }
            }

            // Find withdrawn reservations
            // What's the response (sistEndretTidspunkt) for withdrawn reservations? -- check in test!
            foreach (var currentReservation in currentReservations)
            {
                
                // PatientKey cannot be zero here since we just filtered them out
                if (!newPvkEventsLookup.ContainsKey(currentReservation.PatientKey!))
                {
                    
                    reservationDelta.WithdrawnReservations.Add(currentReservation);
                }
            }

            return reservationDelta;
        }

        public Task AddPatientKeyInPvkEvents(List<SimplePvkEvent> pvkEvents)
        {
            foreach (var eventItem in pvkEvents)
            {
                eventItem.PatientKey = _kodeliste.GetPatientKey(eventItem.PatientFnr);
            }
            return Task.CompletedTask;
        }
    }
}