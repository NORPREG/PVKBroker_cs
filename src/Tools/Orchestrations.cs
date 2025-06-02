using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

using PvkBroker.Configuration;
using PvkBroker.Pvk.ApiCaller;
using PvkBroker.Pvk.TokenCaller;
using PvkBroker.Kodeliste;
using PvkBroker.Redcap;

namespace PvkBroker.Tools
{
    public class ReservationDelta
    {
        public List<SimplePvkEvent> NewReservations { get; set; } = new();
        public List<string> WithdrawnReservations { get; set; } = new();
    }

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

        public async Task HandleNewReservations(List<SimplePvkEvent> newReservations, int pvkSyncId) { 
            Log.Information("Found {NewReservations} new reservation events in PVK sync", newReservations.Count);
            foreach (SimplePvkEvent patient in newReservations)
            {
                try {
                    string registerName = _kodeliste.GetRegisterName(patient.PatientKey);
                    _kodeliste.AddPvkEvent(patient.PatientKey, pvkSyncId);
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
            List<string> patientKeysInRedcap = new HashSet<string>(_redcap.GetAllRecordIdsAsync());
            List<PatientReservation> updatedPatientsReservations = _kodeliste.GetPatientReservations();

            foreach (var patient in updatedPatientsReservations)
            {
                if (!patient.IsReserved && patient.dt_added < quarantinePeriod && !patientKeysInRedcap.Contains(patient.PatientKey))
                {
                    try
                    {
                        var registerName = _kodeliste.GetRegisterName(patient.PatientKey);
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
            var currentReservations = _kodeliste.GetPatientReservations();

            // Create a dictionary for fast lookup of newPvkEvents by PatientKey
            var newPvkEventsLookup = newPvkEvents.ToDictionary(e => e.PatientKey);
            var currentReservationsLookup = currentReservations.ToDictionary(r => r.PatientKey);

            // Find new reservations
            foreach (var newPvkEvent in newPvkEvents)
            {
                if (!currentReservationsLookup.ContainsKey(newPvkEvent.PatientKey))
                {
                    reservationDelta.NewReservations.Add(newPvkEvent);
                }
            }

            // Find withdrawn reservations
            // What's the response (sistEndretTidspunkt) for withdrawn reservations? -- check in test!
            foreach (var currentReservation in currentReservations)
            {
                if (!newPvkEventsLookup.ContainsKey(currentReservation.PatientKey))
                {
                    reservationDelta.WithdrawnReservations.Add(currentReservation.PatientKey);
                }
            }

            return reservationDelta;
        }

        public async Task<List<SimplePvkEvent>> CallPvkAndParseResponse(int pagingReference? = null)
        {
            string accessToken = await _accessTokenCaller.GetAccessToken();
            ClaimsPrincipal principal = await _accessTokenCaller.ValidateAccessTokenAsync(accessToken);

            List<SimplePvkEvent> newPvkEvents = await _pvkCaller.CallApiHentInnbyggereAktivePiForDefinisjon(accessToken);

            return newPvkEvents;
        }
    }
}