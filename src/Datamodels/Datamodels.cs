namespace PvkBroker.Datamodels;

public class PatientReservation
{
    public int PatientId { get; set; }
    public bool IsReserved { get; set; }
    public DateTime? EventTime { get; set; } // On patient side, not API call time
    public DateTime DateAdded { get; set; } // Date when the patient was added to KREST
}
public class ReservationDelta
{
    public List<PatientReservation> NewReservations { get; set; } = new List<PatientReservation>();
    public List<PatientReservation> WithdrawnReservations { get; set; } = new List<PatientReservation>();
}

public class SimplePvkEvent
{
    public required string PatientFnr { get; set; }
    public string? PatientKey { get; set; }
    public required bool IsReserved { get; set; }
    public required DateTime EventTime { get; set; }
}