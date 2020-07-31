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
        IEnumerable<DatabaseBooking> GetBookings(DateTime startTime, DateTime endTime);
        IEnumerable<DatabaseBooking> GetBookings(long timeslotId);
        IEnumerable<DatabaseBooking> GetBookings(DatabaseTimeslot timeslot);
        bool TimeslotIsFull(DateTime startTime, DateTime endTime);
        bool DeleteTimeslot(DateTime startTime, DateTime endTime);
        bool DeleteBooking(long bookingId);
        bool CreateBooking(Person person, DateTime startTime, DateTime endTime);
        bool CancelBooking(Person person, DateTime startTime, DateTime endTime);
    }
}
