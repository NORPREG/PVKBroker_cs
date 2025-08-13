using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

using PvkBroker.Tools;

// If this service becomes too slow, try parallellizing the decryption

// Online estimates
// 10k rows 300-700 ms
// 100k rows 1.5 - 4 s
// 1M rows 10 - 40 s
// I guess will be below 30 s which is OK for our usage (1/day) with caching


namespace PvkBroker.Kodeliste
{
    public class PatientIDCacheService
    {
        private readonly Dictionary<string, List<PatientID>> _fnrToPatientIdMap = new();

        public PatientIDCacheService(KodelisteDbContext DbContext)
        {
            LoadCache(DbContext);
        }

        private void LoadCache(KodelisteDbContext dbContext)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var allPatientIds = dbContext.PatientIDs.ToList();

            foreach (var id in allPatientIds)
            {
                try
                {   
                    if (id.fnr_aes == null)
                    {
                        Log.Warning("PatientID with null fnr_aes found: {@id}", id);
                        continue;
                    }

                    var decryptedFnr = Encryption.Decrypt(id.fnr_aes);
                    if (string.IsNullOrEmpty(decryptedFnr))
                    {
                        Log.Error("Decrypted fnr is null or empty for PatientID: {@id}", id);
                        continue;
                    }

                    // Patient has single key but may have several PatientIDs
                    // with same fk_patient_key (F, D, H numbers)
                    if (!_fnrToPatientIdMap.TryGetValue(decryptedFnr, out var list))
                    {
                        list = new List<PatientID>();
                        _fnrToPatientIdMap[decryptedFnr] = list;
                    }
                    list.Add(id);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error decrypting fnr for PatientID: {@id}", id);
                }
            }
            
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Log.Information("Loaded {count} encrypted PatientIDs in {elapsedMs} ms", allPatientIds.Count, elapsedMs.ToString());
        }

        public void ReloadCache(KodelisteDbContext dbContext)
        {
            lock (_fnrToPatientIdMap)
            {
                _fnrToPatientIdMap.Clear();
                LoadCache(dbContext);
                Log.Information("PatientID cache reloaded successfully.");
            }
            
        }

        public IEnumerable<PatientID> GetPatientIdsByFnr(string fnr)
        {
            if (_fnrToPatientIdMap.TryGetValue(fnr, out var patientIds))
            {
                return patientIds;
            }
            return Enumerable.Empty<PatientID>();
        }

        public int GetPatientId(string patientKey)
        {
            lock (_fnrToPatientIdMap)
            {
                var patientIds = _fnrToPatientIdMap.Values
                    .SelectMany(list => list)
                    .FirstOrDefault(id => id.patient?.patient_key == patientKey);

                if (patientIds == null)
                {
                    Log.Warning("No PatientID found for patient key: {patientKey}", patientKey);
                    return -1;
                }

                return patientIds.id;
            }
        }
    }
}

/* Suggestion for parallelzed run:
 
 var allPatientIds = await dbContext.PatientIDs.AsNoTracking().ToListAsync();

var dict = new ConcurrentDictionary<string, List<PatientID>>();

Parallel.ForEach(allPatientIds, id =>
{
    try
    {
        var decryptedFnr = Encryption.Decrypt(id.fnr_aes);
        if (string.IsNullOrEmpty(decryptedFnr)) { return; }

        dict.AddOrUpdate(decryptedFnr,
            _ => new List<PatientID> { id },
            (_, list) => { lock(list) { list.Add(id); } return list; });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Feil under dekryptering for PatientID {@id}", id);
    }
});

_fnrToPatientIdMap = dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
*/