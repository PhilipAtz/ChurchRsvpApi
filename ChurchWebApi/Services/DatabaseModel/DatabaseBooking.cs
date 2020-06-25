using System;
using ChurchWebApi.Services.AppModel;
using Dapper.Contrib.Extensions;

namespace ChurchWebApi.Services.DatabaseModel
{
    [Table("Booking")]
    public class DatabaseBooking
    {
        [Key]
        public long Id { get; set; }
        public long PersonId { get; set; }
        public long TimeslotId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Cancelled { get; set; }

        public DatabaseBooking() { }

        public DatabaseBooking(
            Booking booking,
            Func<Person, long> personIdRetriever,
            Func<Timeslot, long> timeslotIdRetriever)
        {
            PersonId = personIdRetriever(booking.Person);
            TimeslotId = timeslotIdRetriever(booking.Timeslot);
            Timestamp = booking.Timestamp;
            Cancelled = booking.Cancelled;
        }

        public Booking ToBooking(
            Func<long, Person> personRetriever,
            Func<long, Timeslot> timeslotRetriever)
        {
            return new Booking
            {
                Person = personRetriever(PersonId),
                Timeslot = timeslotRetriever(TimeslotId),
                Timestamp = Timestamp,
                Cancelled = Cancelled,
            };
        }
    }
}
