using System.CommandLine;
using System.Collections.Generic;
using MySql.Data;
using MySql.Data.MySqlClient;
using Serilog;

using PvkBroker.Configuration;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Routing.Constraints;

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
    public class PatientReservation
    {
        public string PatientKey { get; set; }
        public bool IsReserved { get; set; }
        public DateTime? EventTime { get; set; } // On patient side, not API call time
        public string DateAdded { get; set; } // Date when the patient was added to KREST
    }

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

            var patients = _context.Patient
                .Where(p => p.pvk_events.Any())
                .Include(p => p.pvk_events)
                .ToList();

            foreach (var patient in patients)
            {
                var lastEvent = patient.pvk_events
                    .OrderByDescending(e => e.event_time)
                    .FirstOrDefault();

                bool isReserved = false;
                if (lastEvent != null)
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
                Log.Info("No previous information found for patient in PVK");
                return null;
            }

            var uniquePatientKeys = matchingPatientIDs
                .Select(pid => pid.fk_patient_key)
                .Distinct()
                .ToList();

            var patients = _context.Patient
                .Include(p => p.pvk_events)
                .Where(p => uniquePatientKeys.Contains(p.patient_key))
                .ToList();

            foreach (var patient in patients)
            {
                var lastEvent = patient.pvk_events
                    .OrderByDescending(e => e.event_time)
                    .FirstOrDefault();

                bool isReserved = false;
                if (lastEvent != null)
                {
                    try
                    {
                        bool isReserved = Encryption.DecryptBool(lastEvent.is_reserved_aes.ToString());
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Cannot decrypt is_reserved_aes at key {key}: {@ex}", patient.patient_key, ex);
                    }
                }
                var reservation = new PatientReservation
                {
                    PatientKey = patient.patient_key,
                    IsReserved = isReserved,
                    EventTime = lastEvent?.event_time,
                    DateAdded = patient.dt_added
                };
                return reservation;
            }
            return null;
        }
        public void AddPvkEvent(int patientKey, bool isReserved, int SyncId)
        {
            try
            {
                var isReservedAes = Encryption.EncryptBool(isReserved);
                var newEvent = new PvkEvent
                {
                    fk_patient_key = patientKey,
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
                    new_reservation_removals = reservationDelta.WithdrawnReservations.Count,
                    error_message = null,
                    sync_time = DateTime.UtcNow
                };

                _context.PvkSyncs.Add(newSync);
                _context.SaveChanges();

                return newSync.sync_id;
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
                    .All(pid => pid.fk_patient_key == patientIDs[0].fk_patient_key);
                if (!allSimilar)
                {
                    Log.Error("PatientIDs with same fnr {fnr} have different patient_keys", fnr);
                    return null;
                }

                return patientIDs[0].fk_patient_key;
            }
        }

        public string? GetRegisterName(string patientKey)
        {
            string registerName = _context.Patient
                .Where(p => p.patient_key == patientKey)
                .Include(p => p.registry)
                .Select(p => p.registry.name)
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
            var patient = _context.Patient
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
                var newEvent = new PvkEvent
                {
                    fk_patient_key = eventObject.PatientKey,
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
                var newEvent = new PvkEvent
                {
                    fk_patient_key = patientKey,
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
