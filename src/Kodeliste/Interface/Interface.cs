using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq;
using System.Globalization;
using System;
using System.Data;

using PvkBroker.Configuration;
using PvkBroker.Datamodels;
using PvkBroker.Tools;

/*  Workflow when syncing against PVK 
1) Get list of all reserved patients from PVK
2) Loop through PatientID table
     3) For each PatientID table entry, decrypt the fnr
     4) For each fk_patient_key, decrypt the lastest PvkEvent->is_reserved_aes
5) For each patient with is_reserved = 1 and not in lastest reserved patients from PVK
     6) Remove from NORPREG with RedcapRemovePatient(patient_key)
7) For each patient with is_reserved = 0 and in lastest reserved patients from PVK
     8) Add to NORPREG with RedcapAddPatient(patient_key)
9) AddPvkSync(new_reservations, new_reservation_removals, error_message)

Thus we need the following functions
CHECK - List<ReservationStatus> GetReservedPatients(List<string> list_fnr)
- AddPvkEvent(string patient_key, bool is_reserved) // use dt_now
- AddPvkSync(int new_reservations, int new_reservation_removals, error_message)
 */

namespace PvkBroker.Kodeliste
{
    public class KodelisteInterface
    {
        private readonly PatientIDCacheService _cache;
        private readonly KodelisteDbContext _context;

        public KodelisteInterface(KodelisteDbContext context, PatientIDCacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        public List<PatientReservation> GetPatientReservations()
        {
            var pastientReservations = new List<PatientReservation>();

            var patients = _context.Patients
                .Where(p => p.pvk_events.Any())
                .Include(p => p.pvk_events)
                .ToList();

            foreach (var patient in patients)
            {
                var lastEvent = patient.pvk_events
                    .OrderByDescending(e => e.event_time)
                    .FirstOrDefault();

                bool isReserved = false;
                if (lastEvent != null && lastEvent.is_reserved_aes != null)
                {
                    try
                    {
                        isReserved = Encryption.DecryptBool(lastEvent.is_reserved_aes.ToString());
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Cannot decrypt is_reserved_aes at key {key}: {@ex}", patient.patient_key, ex);
                    }
                }


                pastientReservations.Add(new PatientReservation
                {
                    PatientId = patient.id,
                    PatientKey = patient.patient_key,
                    IsReserved = isReserved,
                    EventTime = lastEvent?.event_time,
                    DateAdded = patient.dt_added
                });
            }
            return pastientReservations;
        }

        public PatientReservation? GetPatientReservation(string fnrInput)
        {
            var matchingPatientIDs = _cache.GetPatientIdsByFnr(fnrInput);
            if (!matchingPatientIDs.Any())
            {
                Log.Information("No previous information found for patient in PVK");
                return null;
            }

            var uniquePatientKeys = matchingPatientIDs
                .Select(pid => pid.fk_patient_id)
                .Distinct()
                .ToList();

            var patients = _context.Patients
                .Include(p => p.pvk_events)
                .Where(p => uniquePatientKeys.Contains(p.id))
                .ToList();

            foreach (var patient in patients)
            {
                PvkEvent? lastEvent = patient.pvk_events
                    .OrderByDescending(e => e.event_time)
                    .FirstOrDefault();

                bool isReserved = false;
                if (lastEvent != null && lastEvent.is_reserved_aes != null)
                {
                    try
                    {
                        isReserved = Encryption.DecryptBool(lastEvent.is_reserved_aes.ToString());
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Cannot decrypt is_reserved_aes at key {key}: {@ex}", patient.patient_key, ex);
                    }
                }
                var reservation = new PatientReservation
                {
                    PatientId = patient.id,
                    IsReserved = isReserved,
                    EventTime = lastEvent?.event_time,
                    DateAdded = patient.dt_added
                };
                return reservation;
            }
            return null;
        }
        public void AddPvkEvent(string patientKey, bool isReserved, int SyncId)
        {
            try
            {
                var isReservedAes = Encryption.EncryptBool(isReserved);
                int patientId = _cache.GetPatientId(patientKey);

                var newEvent = new PvkEvent
                {
                    fk_patient_id = patientId,
                    is_reserved_aes = isReservedAes,
                    fk_sync_id = SyncId,
                };

                _context.PvkEvents.Add(newEvent);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error("Error adding PvkEvent for patient {patientKey}: {@ex}", patientKey, ex);
                throw;
            }
        }

        public int CreatePvkSync(ReservationDelta reservationDelta)
        {
            try
            {
                var newSync = new PvkSync
                {
                    new_reservations = reservationDelta.NewReservations.Count,
                    withdrawn_reservations = reservationDelta.WithdrawnReservations.Count,
                    error_message = null,
                    dt_sync = DateTime.UtcNow
                };

                _context.PvkSyncs.Add(newSync);
                _context.SaveChanges();

                return newSync.id;
            }
            catch (Exception ex)
            {
                Log.Error("Error adding PvkSync: {@ex}", ex);
                throw;
            }
        }

        public string? GetPatientKey(string fnr)
        {
            List<PatientID> patientIDs = _cache.GetPatientIdsByFnr(fnr).ToList();

            if (patientIDs.Count == 0)
            {
                Log.Error("No patient found with fnr {fnr}", fnr);
                return null;
            }
            else
            {

                // All PatientIDs with same fnr SHOULD have same patient_key, check this as a consistency check
                bool allSimilar = patientIDs
                    .All(pid => pid.fk_patient_id == patientIDs[0].fk_patient_id);
                if (!allSimilar)
                {
                    Log.Error("PatientIDs with same fnr {fnr} have different patient_keys", fnr);
                    return null;
                }

                string? patientKey = _context.Patients
                    .FirstOrDefault(p => p.id == patientIDs[0].fk_patient_id)
                    ?.patient_key;

                return patientKey;
            }
        }

        public void AddPatient(string name, string fnr)
        {
            var birth_date = DateTime.ParseExact(fnr.Substring(0, 6), "ddMMyy", CultureInfo.InvariantCulture);


            // Testing purposes
            if (!_context.Registries.Any(r => r.id == 1))
            {
                _context.Registries.Add(new Registry
                {
                    id = 1, // viktig!
                    name = "KREST-XXX",
                });
                _context.SaveChanges();
            }

            List<PatientID> patientIDs = _cache.GetPatientIdsByFnr(fnr).ToList();
            if (patientIDs.Count > 0)
            {
                Log.Information("Patient with fnr {fnr} already exists in the database", fnr);
                return;
            }

            var patient = new Patient
            { 
                patient_key = PatientKey.GeneratePatientKey(), // Generate a new unique patient key
                dt_added = DateTime.UtcNow,
                name_aes = Encryption.Encrypt(name),
                birth_date_aes = Encryption.Encrypt(DateTime.UtcNow.ToString("yyyy-MM-dd")), // Placeholder for birth date, should be provided
                ois_patient_id_aes = null, // Placeholder, should be provided if available
                epj_patient_id_aes = null, // Placeholder, should be provided if available
                fk_registry_id = 1, // Assuming registry ID 1 is KREST-XXX, adjust as needed
                patient_ids = new List<PatientID>()
                {
                    new PatientID
                    {
                        dt_added = DateTime.UtcNow,
                        fnr_aes = Encryption.Encrypt(fnr),
                        fnr_type = "F", // Assuming FNR type, adjust as needed
                        patient = null  // avoid circular logic
                    }
                }
            };

            try
            {
                _context.Patients.Add(patient);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error("Error adding patient {patientKey}: {@ex}", patient.patient_key, ex.Message);
                throw;
            }
        }

        public string? GetRegisterName(string? patientKey)
        {
            if (string.IsNullOrEmpty(patientKey))
            {
                Log.Error("Patient key is null or empty");
                return null;
            }

            string? registerName = _context.Patients
                .Where(p => p.patient_key == patientKey)
                .Include(p => p.registry)
                .Select(p => p.registry != null ? p.registry.name : null)
                .FirstOrDefault();

            if (registerName == null)
            {
                Log.Error("No register key found for patient {key}", patientKey);
                return null;
            }

            return registerName;
        }

        public DateTime GetPatientAddedDate(string patientKey)
        {
            var patient = _context.Patients
                .FirstOrDefault(p => p.patient_key == patientKey);
            if (patient == null)
            {
                Log.Error("No patient found with key {key}", patientKey);
                return DateTime.MinValue;
            }
            return patient.dt_added;
        }

        public void AddPvkEvent(SimplePvkEvent eventObject, int syncId)
        {
            try
            {
                if (eventObject == null || eventObject.PatientKey == null)
                {
                    Log.Error("Event object is null, cannot add PvkEvent");
                    return;
                }

                int patientId = _cache.GetPatientId(eventObject.PatientKey);

                if (patientId == -1)
                {
                    Log.Error("No patient found with key {patientKey}", eventObject.PatientKey);
                    return;
                }   

                var newEvent = new PvkEvent
                {
                    fk_patient_id = patientId,
                    is_reserved_aes = Encryption.EncryptBool(eventObject.IsReserved),
                    event_time = eventObject.EventTime,
                    fk_sync_id = syncId
                };
                _context.PvkEvents.Add(newEvent);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error("Error adding PvkEvent for patient {patientKey}: {@ex}", eventObject.PatientKey, ex);
                throw;
            }
        }

        public void AddPvkEventWithdrawn(string patientKey, int syncId)
        {
            try
            {
                int patientId = _cache.GetPatientId(patientKey);

                var newEvent = new PvkEvent
                {
                    fk_patient_id = patientId,
                    is_reserved_aes = Encryption.EncryptBool(false),
                    event_time = DateTime.UtcNow.AddHours(-ConfigurationValues.PvkSyncTimeInHours / 2), // we don't know the time, so halfway between last sync
                    fk_sync_id = syncId
                };
                _context.PvkEvents.Add(newEvent);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error("Error adding PvkEvent for patient {patientKey}: {@ex}", patientKey, ex);
                throw;
            }
        }

        public void ReloadCache()
        {
            try
            {
                _cache.ReloadCache(_context);
            }
            catch (Exception ex)
            {
                Log.Error("Error reloading Kodeliste cache: {@ex}", ex);
                throw;
            }
        }
    }
}
