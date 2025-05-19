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
        public DateTime? EventTime { get; set; }
    }

    public class Interface
    {
        DbConnection dbCon;
        PatientIDClassService cache;

        private Interface()
        {
            dbCon = SetupConnection();
            cache = new PatientIDClassService(dbCon);
        }

        public static PatientReservation? GetPatientsReservation(string fnrInput)
        {
            var matchingPatientIDs = cache.GetPatientIdsByFnr(fnrInput);
            if (matchingPatientIDs.Count() == 0)
            {
                Log.Info("No previous information found for patient in PVK");
                return null;
            }

            var uniquePatientKeys = matchingPatientIDs
                .Select(pid => pid.fk_patient_key)
                .Distinct()
                .ToList();

            var patients = dbCon.Patients
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
                        var isReservedDecrypted = Encryption.Decrypt(lastEvent.is_reserved_aes.ToString());
                        isReserved = bool.TryParse(isReservedDecrypted, out var val) ? val : false;
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
                    EventTime = lastEvent?.event_time
                };
                return reservation;
            }
            return null;
        }
        public static AddPvkEvent(int patientKey, bool isReserved, int SyncId)
        {
            try
            {
                var is_reserved_aes = Encryption.Encrypt(isReserved.ToString());
                var newEvent = new PvkEvent
                {
                    fk_patient_key = patientKey,
                    is_reserved_aes = isReservedAes,
                    fk_sync_id = SyncId,
                };

                dbCon.PvkEvents.Add(newEvent);
                dbCon.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error("Error adding PvkEvent for patient {patientKey}: {@ex}", patientKey, ex);
                throw;
            }
        }

        public static int CreatePvkSync(DateTime syncTime)
        {
            try
            {
                var newSync = new PvkSync
                {
                    new_reservations = 0,
                    new_reservation_removals = 0,
                    error_message = null,
                    sync_time = syncTime
                };

                dbCon.PvkSyncs.Add(newSync);
                dbCon.SaveChanges();

                return newSync.sync_id;
            }
            catch (Exception ex)
            {
                Log.Error("Error adding PvkSync: {@ex}", ex);
                throw;
            }
        }

        public static void UpdatePvkSync(int syncId, int newReservations, int newReservationRemovals, string errorMessage)
        {
            var sync = dbCon.PvkSyncs.Find(syncId);
            if (sync == null)

            {
                Log.Error("PvkSync with ID {syncId} not found", syncId);
                throw new Exception($"PvkSync with ID {syncId} not found");
            }

            sync.new_reservations = newReservations;
            sync.new_reservation_removals = newReservationRemovals;
            sync.error_message = errorMessage;
            dbCon.SaveChanges();
        }
    }
}
