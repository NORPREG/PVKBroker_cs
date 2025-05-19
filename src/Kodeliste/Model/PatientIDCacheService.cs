using Serilog;

// If this service becomes too slow, try parallellizing the decryption

// Online estimates
// 10k rows 300-700 ms
// 100k rows 1.5 - 4 s
// 1M rows 10 - 40 s
// I guess will be below 30 s which is OK for our usage (1/day) with caching

namespace PvkBroker.Kodeliste
{
    public class PatientIDClassService
    {
    private readonly Dictionary<string, List<PatientID>> _fnrToPatientIdMap = new();

        public PatientIDClassService(KodelisteDbContext DbContext)
        {
            LoadCache(DbContext);
        }

        private void LoadCache(KodelisteDbContext dbContext)
        {
            var allPatientIds = dbContext.PatientIDs.AsNoTracking().ToList();

            foreach (var id in allPatientIds)
            {
                try
                {
                    var decryptedFnr = Encryption.Decrypt(id.fnr_aes);
                    if (string.IsNullOrEmpty(decryptedFnr))
                    {
                        Log.Error("Decrypted fnr is null or empty for PatientID: {@id}", id);
                        continue;
                    }

                    if (!_fnrToPatientIdMap.TryGetValue(decryptedFnr, out var list))
                    {
                        list = new List<PatientID>();
                        _fnrToPatientIdMap[decryptedFnr] = list;
                    }
                    list.Add(id);
                }
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
    }
}
