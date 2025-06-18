/*
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PvkBroker.Kodeliste.Encryption;

// FRA LLM, se NØYE gjennom før jeg kjører
public class ReservationComparerTests
{
    [Test]
    public void CompareReservationsWithLatestEvents_FindsCorrectDeltas()
    {
        // Arrange
        var mockContext = new Mock<KodelisteDbContext>();

        var fakeEvents = new List<PvkEvent>
        {
            new PvkEvent
            {
                fk_patient_key = 1,
                event_time = DateTime.UtcNow.AddHours(-1),
                is_reserved_aes = Encrypt("false")
            },
            new PvkEvent
            {
                fk_patient_key = 2,
                event_time = DateTime.UtcNow.AddHours(-1),
                is_reserved_aes = Encrypt("true")
            }
        };

        var mockSet = new Mock<DbSet<PvkEvent>>();
        mockSet.As<IQueryable<PvkEvent>>().Setup(m => m.Provider).Returns(fakeEvents.AsQueryable().Provider);
        mockSet.As<IQueryable<PvkEvent>>().Setup(m => m.Expression).Returns(fakeEvents.AsQueryable().Expression);
        mockSet.As<IQueryable<PvkEvent>>().Setup(m => m.ElementType).Returns(fakeEvents.AsQueryable().ElementType);
        mockSet.As<IQueryable<PvkEvent>>().Setup(m => m.GetEnumerator()).Returns(() => fakeEvents.AsQueryable().GetEnumerator());

        mockContext.Setup(c => c.PvkEvents).Returns(mockSet.Object);

        var comparer = new ReservationComparer(mockContext.Object);

        var currentReservations = new List<PatientReservation>
        {
            new PatientReservation { PatientKey = 1, IsReserved = true },  // Ny reservasjon
            new PatientReservation { PatientKey = 2, IsReserved = false }, // Tilbaketrukket
            new PatientReservation { PatientKey = 3, IsReserved = true }   // Helt ny
        };

        // Act
        var result = comparer.CompareReservationsWithLatestEvents(currentReservations);

        // Assert
        Assert.That(result.NewlyReserved, Does.Contain(1));
        Assert.That(result.WithdrawnReservation, Does.Contain(2));
        Assert.That(result.NewlyReserved, Does.Contain(3));
        Assert.That(result.NewlyReserved.Count, Is.EqualTo(2));
        Assert.That(result.WithdrawnReservation.Count, Is.EqualTo(1));
    }
}

public class PatientReservation
{
    public int PatientKey { get; set; }
    public bool IsReserved { get; set; }
}

public class PvkEvent
{
    public int fk_patient_key { get; set; }
    public DateTime event_time { get; set; }
    public string is_reserved_aes { get; set; }
}

public static class CryptoHelper
{
    public static string Encrypt(string value) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    public static string Decrypt(string value) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));
}

public class ReservationComparer
{
    private readonly KodelisteDbContext _dbContext;

    public ReservationComparer(KodelisteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ReservationDelta CompareReservationsWithLatestEvents(List<PatientReservation> currentReservations)
    {
        var latestEvents = _dbContext.PvkEvents
            .GroupBy(e => e.fk_patient_key)
            .Select(g => g.OrderByDescending(e => e.event_time).First())
            .ToDictionary(e => e.fk_patient_key, e => e);

        var result = new ReservationDelta();

        foreach (var reservation in currentReservations)
        {
            if (!latestEvents.TryGetValue(reservation.PatientKey, out var lastEvent))
            {
                if (reservation.IsReserved)
                    result.NewlyReserved.Add(reservation.PatientKey);
                continue;
            }

            var lastReservedDecrypted = bool.Parse(CryptoHelper.Decrypt(lastEvent.is_reserved_aes));

            if (reservation.IsReserved && !lastReservedDecrypted)
            {
                result.NewlyReserved.Add(reservation.PatientKey);
            }
            else if (!reservation.IsReserved && lastReservedDecrypted)
            {
                result.WithdrawnReservation.Add(reservation.PatientKey);
            }
        }

        return result;
    }
}

public class ReservationDelta
{
    public List<int> NewlyReserved { get; set; } = new();
    public List<int> WithdrawnReservation { get; set; } = new();
}

// Dummy DbContext for mocking
public class KodelisteDbContext
{
    public virtual DbSet<PvkEvent> PvkEvents { get; set; }
}
*/