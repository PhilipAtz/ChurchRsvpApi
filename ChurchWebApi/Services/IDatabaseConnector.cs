using System;
using System.Collections.Generic;
using ChurchWebApi.Services.AppModel;
using ChurchWebApi.Services.DatabaseModel;

namespace ChurchWebApi.Services
{
    public interface IDatabaseConnector
    {
        DatabasePerson GetDatabasePerson(Person person);
        DatabasePerson GetDatabasePerson(long personId);
        long CreatePerson(Person person);
        DatabaseTimeslot GetTimeslot(long timeslotId);
        DatabaseTimeslot GetTimeslot(DateTime startTime, DateTime endTime);
        long CreateTimeslot(Timeslot timeslot);
        IEnumerable<DatabaseBooking> GetActiveBookings(DateTime startTime, DateTime endTime);
        IEnumerable<DatabaseBooking> GetActiveBookings(long timeslotId);
        IEnumerable<DatabaseBooking> GetActiveBookings(DatabaseTimeslot timeslot);
        bool TimeslotIsFull(DateTime startTime, DateTime endTime);
        bool DeleteBookingByRowId(long bookingId);
        bool CreateBooking(Person person, DateTime startTime, DateTime endTime);
    }
}
