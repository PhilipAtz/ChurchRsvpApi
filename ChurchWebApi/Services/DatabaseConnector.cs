using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChurchWebApi.Services.AppModel;
using ChurchWebApi.Services.DatabaseModel;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;

namespace ChurchWebApi.Services
{
    public class DatabaseConnector : IDatabaseConnector
    {
        private readonly ILogger<DatabaseConnector> _log;
        private readonly IEncryptionLayer _encryptionLayer;
        private readonly ISqlRunner _sqlRunner;

        public DatabaseConnector(ILogger<DatabaseConnector> logger, IEncryptionLayer encryptionLayer, ISqlRunner sqlRunner)
        {
            _log = logger;
            _encryptionLayer = encryptionLayer;
            _sqlRunner = sqlRunner;
        }

        private string GetConditionForPerson(Person person, string tablePrefix = null)
        {
            tablePrefix = string.IsNullOrWhiteSpace(tablePrefix)
                ? string.Empty
                : $"{tablePrefix.TrimEnd('.')}.";
            var condition = new StringBuilder($"{tablePrefix}Name = @Name");
            
            condition.Append(string.IsNullOrWhiteSpace(person.Email)
                ? $" and {tablePrefix}Email is null"
                : $" and {tablePrefix}Email = @Email");

            condition.Append(string.IsNullOrWhiteSpace(person.Mobile)
                ? $" and {tablePrefix}Mobile is null"
                : $" and {tablePrefix}Mobile = @Mobile");

            return condition.ToString();
        }

        private DynamicParameters GetParametersForPerson(Person person)
        {
            var encryptedPerson = person.ToEncryptedDatabasePerson(_encryptionLayer);
            var parameters = new DynamicParameters();
            parameters.Add(nameof(DatabasePerson.Name), encryptedPerson.Name);

            if (!string.IsNullOrWhiteSpace(person.Email))
            {
                parameters.Add(nameof(DatabasePerson.Email), encryptedPerson.Email);
            }

            if (!string.IsNullOrWhiteSpace(person.Mobile))
            {
                parameters.Add(nameof(DatabasePerson.Mobile), encryptedPerson.Mobile);
            }

            return parameters;
        }

        public DatabasePerson GetDatabasePerson(Person person)
        {
            var sql = $"select Id, Name, Email, Mobile from Person where {GetConditionForPerson(person)}";
            var parameters = GetParametersForPerson(person);

            return _sqlRunner.EnqueueDatabaseCommand(
                con => con.Query<(long Id, string Name, string Email, string Mobile)>(
                    sql,
                    parameters))
                .Select(o => new EncryptedDatabasePerson
                {
                    Id = o.Id,
                    Name = o.Name,
                    Email = string.IsNullOrWhiteSpace(o.Email)
                        ? null
                        : o.Email,
                    Mobile = string.IsNullOrWhiteSpace(o.Mobile)
                        ? null
                        : o.Mobile,
                })
                .FirstOrDefault()
                ?.ToDatabasePerson(_encryptionLayer);
        }

        public DatabasePerson GetDatabasePerson(long personId)
        {
            const string sql = "select Id, Name, Email, Mobile from Person where Id = @personId";

            return _sqlRunner.EnqueueDatabaseCommand(
                con => con.Query<(long Id, string Name, string Email, string Mobile)>(
                    sql,
                    new { personId }))
                .Select(o => new DatabasePerson
                {
                    Id = o.Id,
                    Name = _encryptionLayer.Decrypt(o.Name),
                    Email = string.IsNullOrWhiteSpace(o.Email)
                        ? null
                        : _encryptionLayer.Decrypt(o.Email),
                    Mobile = string.IsNullOrWhiteSpace(o.Mobile)
                        ? null
                        : _encryptionLayer.Decrypt(o.Mobile),
                })
                .SingleOrDefault();
        }

        public long CreatePerson(Person person)
        {
            var existingPerson = GetDatabasePerson(person);
            if (existingPerson != null)
            {
                return existingPerson.Id;
            }

            var encryptedPerson = person.ToEncryptedDatabasePerson(_encryptionLayer);
            return _sqlRunner.EnqueueDatabaseCommand(con => con.Insert(encryptedPerson));
        }

        public DatabaseTimeslot GetTimeslot(long timeslotId)
        {
            const string sql = "select Id, StartTime, EndTime, Capacity from Timeslot where Id = @timeslotId";

            return _sqlRunner.EnqueueDatabaseCommand(
                con => con.Query<(long Id, DateTime StartTime, DateTime EndTime, int Capacity)>(
                    sql,
                    new { timeslotId }))
                .Select(o => new DatabaseTimeslot
                {
                    Id = o.Id,
                    StartTime = o.StartTime,
                    EndTime = o.EndTime,
                    Capacity = o.Capacity,
                })
                .SingleOrDefault();
        }

        public DatabaseTimeslot GetTimeslot(DateTime startTime, DateTime endTime)
        {
            const string sql = "select Id, StartTime, EndTime, Capacity from Timeslot where StartTime = @startTime and EndTime = @endTime";

            return _sqlRunner.EnqueueDatabaseCommand(
                con => con.Query<(long Id, DateTime StartTime, DateTime EndTime, int Capacity)>(
                    sql,
                    new
                    {
                        startTime,
                        endTime,
                    }))
                .Select(o => new DatabaseTimeslot
                {
                    Id = o.Id,
                    StartTime = o.StartTime,
                    EndTime = o.EndTime,
                    Capacity = o.Capacity,
                })
                .FirstOrDefault();
        }

        public long CreateTimeslot(Timeslot timeslot)
        {
            var existingTimeslot = GetTimeslot(timeslot.StartTime, timeslot.EndTime);
            if (existingTimeslot != null)
            {
                return existingTimeslot.Id;
            }

            return _sqlRunner.EnqueueDatabaseCommand(con => con.Insert(timeslot.ToDatabaseTimeslot()));
        }

        public IEnumerable<DatabaseBooking> GetBookings(DateTime startTime, DateTime endTime) =>
            GetBookings(GetTimeslot(startTime, endTime));

        public IEnumerable<DatabaseBooking> GetBookings(long timeslotId) =>
            GetBookings(GetTimeslot(timeslotId));

        public IEnumerable<DatabaseBooking> GetBookings(DatabaseTimeslot timeslot)
        {
            if (timeslot == null)
            {
                return Array.Empty<DatabaseBooking>();
            }

            const string sql = "select Id as BookingId, PersonId, TimeslotId, Timestamp, Cancelled from Booking where TimeslotId = @timeslotId order by Timestamp asc";

            return _sqlRunner.EnqueueDatabaseCommand(con => con.Query<(long BookingId, long PersonId, long TimeslotId, DateTime Timestamp, bool Cancelled)>(
                    sql,
                    new { timeslotId = timeslot.Id }))
                .Select(o => new DatabaseBooking
                {
                    Id = o.BookingId,
                    PersonId = o.PersonId,
                    TimeslotId = o.TimeslotId,
                    Timestamp = o.Timestamp,
                    Cancelled = o.Cancelled,
                });
        }

        public bool TimeslotIsFull(DateTime startTime, DateTime endTime)
        {
            var timeslot = GetTimeslot(startTime, endTime);
            return GetBookings(startTime, endTime).Where(booking => !booking.Cancelled).Count() >= timeslot.Capacity;
        }

        public bool DeleteTimeslot(DateTime startTime, DateTime endTime)
        {
            var timeslot = GetTimeslot(startTime, endTime);
            if (timeslot == null)
            {
                return false;
            }

            var bookings = GetBookings(timeslot);
            foreach(var bookingId in bookings.Select(booking => booking.Id))
            {
                DeleteBooking(bookingId);
            }

            const string sql =
                @"delete from Timeslot where Id = @timeslotId;
                  select changes()";

            return _sqlRunner.EnqueueDatabaseCommand(con => con.ExecuteScalar<int>(
                sql,
                new { timeslotId = timeslot.Id })) > 0;
        }

        public bool DeleteBooking(long bookingId)
        {
            const string sql = 
                @"delete from Booking where Id = @bookingId;
                  select changes()";

            return _sqlRunner.EnqueueDatabaseCommand(con => con.ExecuteScalar<int>(
                sql,
                new { bookingId })) > 0;
        }

        public bool CancelBooking(Person person, DateTime startTime, DateTime endTime)
        {
            var personId = GetDatabasePerson(person)?.Id;
            var timeslotId = GetTimeslot(startTime, endTime)?.Id;

            if (!personId.HasValue || !timeslotId.HasValue)
            {
                return false;
            }

            var sql =
                @"update Booking
                  set Cancelled = 1
                  where PersonId = @personId and
                        TimeslotId = @timeslotId;
                  select changes()";

            return _sqlRunner.EnqueueDatabaseCommand(con => con.ExecuteScalar<int>(
                sql,
                new
                {
                    personId,
                    timeslotId,
                }) > 0);
        }

        public bool CreateBooking(Person person, DateTime startTime, DateTime endTime)
        {
            var timeslot = GetTimeslot(startTime, endTime);
            if (timeslot == null)
            {
                return false;
            }

            var bookings = GetBookings(startTime, endTime).ToList();
            if (bookings.Where(booking => !booking.Cancelled).Count() > timeslot.Capacity)
            {
                return false;
            }

            var personId = CreatePerson(person);
            if (bookings.Any(o => o.PersonId == personId && o.TimeslotId == timeslot.Id))
            {
                return false;
            }

            const string sql =
                @"insert into Booking (PersonId, TimeslotId, Timestamp, Cancelled) values (@personId, @timeslotId, @timestamp, 0);
                  select last_insert_rowid()";

            var bookingId = _sqlRunner.EnqueueDatabaseCommand(con => con.ExecuteScalar<long>(
                sql,
                new
                {
                    personId,
                    timeslotId = timeslot.Id,
                    timestamp = DateTime.Now,
                }));

            bookings = GetBookings(startTime, endTime).Where(booking => !booking.Cancelled).ToList();
            if (bookings.FindIndex(o => o.Id == bookingId) + 1 > timeslot.Capacity)
            {
                DeleteBooking(bookingId);
                return false;
            }

            return true;
        }
    }
}
