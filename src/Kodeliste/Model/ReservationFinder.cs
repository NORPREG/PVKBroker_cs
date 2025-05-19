using Serilog;

// denne kan jeg teste om jeg mocker databasen

namespace PvkBroker.Kodeliste
{
    public class ReservationDelta
    {
        public List<string> NewlyReserved { get; set; } = new();
        public List<string> WithdrawnReservation { get; set; } = new();
    }

    public ReservationDelta CompareReservationWithPvk(
        List<PatientReservation> currentReservations) // This is from Pvk Api and not Kodeliste
    {

        // { key: latest event }
        var latestEvents = dbCon.PvkEvents
            .GroupBy(pe => pe.fk_patient_key)
            .Select(g => g.OrderByDescending(pe => pe.event_time).FirstOrDefault())
            .ToDictionary(pe => pe.fk_patient_key, pe => pe);

        var result = new ReservationDelta();
        foreach (var reservation in currentReservations)
        {
            if (!latestEvents.TryGetValue(reservation.PatientKey, out var lastEvent))
            {
                // No earlier event, so newly reserved
                if (reservation.IsReserved)
                {
                    result.NewlyReserved.Add(reservation.PatientKey);
                }
                continue;
            }

            var isReservedDecrypted = Encryption.Decrypt(lastEvent.is_reserved_aes.ToString());
            var lastReservedDecrypted = bool.Parse(isReservedDecrypted);

            if (reservation.IsReserved && !lastReservedDecrypted)
            {
                // Newly reserved
                result.NewlyReserved.Add(reservation.PatientKey);
            }
            else if (!reservation.IsReserved && lastReservedDecrypted)
            {
                // Withdrawn reservation
                result.WithdrawnReservation.Add(reservation.PatientKey);
            }
        }
        return result;
    }
}