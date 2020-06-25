using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using ChurchWebApi.Services.AppModel;
using ChurchWebApi.Services.DatabaseModel;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;

namespace ChurchWebApi.Services
{
    public class DatabaseConnector : IDatabaseConnector
    {
        private const string DatabaseFile = "database.sqlite";
        public static int MaxNameLength = 350;
        public static int MaxEmailLength = 200;
        public static int MaxMobileLength = 50;
        private readonly ILogger<DatabaseConnector> _log;
        private readonly IEncryptionLayer _encryptionLayer;
        private readonly ISqlRunner _sqlRunner;

        public DatabaseConnector(ILogger<DatabaseConnector> logger, IEncryptionLayer encryptionLayer, ISqlRunner sqlRunner)
        {
            _log = logger;
            _encryptionLayer = encryptionLayer;
            _sqlRunner = sqlRunner;

            if (File.Exists(DatabaseFile))
                return;

            _log?.LogWarning($"Could not locate database file '{DatabaseFile}', creating a new one.");
            SQLiteConnection.CreateFile(DatabaseFile);
            CreateDatabaseTables();
        }

        public DatabasePerson GetDatabasePerson(Person person)
        {
            var encryptedPerson = person.ToEncryptedDatabasePerson(_encryptionLayer);
            var sql = "select Id, Name, Email, Mobile from Person where Name = @Name";
            var parameters = new DynamicParameters();
            parameters.Add(nameof(DatabasePerson.Name), encryptedPerson.Name);

            if (string.IsNullOrWhiteSpace(person.Email))
            {
                sql += " and Email is null";
            }
            else
            {
                sql += " and Email = @Email";
                parameters.Add(nameof(DatabasePerson.Email), encryptedPerson.Email);
            }
            if (string.IsNullOrWhiteSpace(person.Mobile))
            {
                sql += " and Mobile is null";
            }
            else
            {
                sql += " and Mobile = @Mobile";
                parameters.Add(nameof(DatabasePerson.Mobile), encryptedPerson.Mobile);
            }

            return _sqlRunner.EnqueueDatabaseCommand(
                con => con.Query<(long Id, string Name, string Email, string Mobile)>(
                    sql,
                    parameters))
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
                .FirstOrDefault();
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
                .Single();
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

        public IEnumerable<DatabaseBooking> GetActiveBookings(DateTime startTime, DateTime endTime) =>
            GetActiveBookings(GetTimeslot(startTime, endTime));

        public IEnumerable<DatabaseBooking> GetActiveBookings(long timeslotId) =>
            GetActiveBookings(GetTimeslot(timeslotId));

        public IEnumerable<DatabaseBooking> GetActiveBookings(DatabaseTimeslot timeslot)
        {
            const string sql = "select Id as BookingId, PersonId, TimeslotId, Timestamp, Cancelled from Booking where Cancelled = 0 and TimeslotId = @timeslotId order by Timestamp asc";

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
            return GetActiveBookings(startTime, endTime).Count() >= timeslot.Capacity;
        }

        public bool DeleteBookingByRowId(long bookingId)
        {
            const string sql = 
                @"delete from Booking where Id = @bookingId;
                  select changes()";

            return _sqlRunner.EnqueueDatabaseCommand(con => con.ExecuteScalar<int>(
                sql,
                new { bookingId })) > 0;
        }

        public bool CreateBooking(Person person, DateTime startTime, DateTime endTime)
        {
            var timeslot = GetTimeslot(startTime, endTime);
            var bookings = GetActiveBookings(startTime, endTime).ToList();
            if (bookings.Count > timeslot.Capacity)
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

            bookings = GetActiveBookings(startTime, endTime).ToList();
            if (bookings.FindIndex(o => o.Id == bookingId) + 1 > timeslot.Capacity)
            {
                DeleteBookingByRowId(bookingId);
                return false;
            }

            return true;
        }

        private void CreateDatabaseTables()
        {
            var sqls = new[]
            {
                // The columns of the Person table are defined to be
                // that long because they will be storing encrypted data.
                $@"create table Person
                (
	                Id integer not null primary key,
	                Name nvarchar({MaxNameLength}) not null,
	                Email nvarchar({MaxEmailLength}),
	                Mobile nvarchar({MaxMobileLength}),
                    constraint uc_Person unique (Name, Email, Mobile)
                )",
                @"create index ix_Person_Name on Person (Name)",
                @"create table Timeslot
                (
	                Id integer not null primary key,
	                StartTime datetime not null,
	                EndTime datetime not null,
	                Capacity int not null,
                    constraint uc_Timeslot unique (StartTime, EndTime)
                )",
                @"create index ix_Timeslot_StartTime on Timeslot (StartTime)",
                @"create table Booking
                (
	                Id integer not null primary key,
	                PersonId int not null constraint fk_Person,
	                TimeslotId int not null constraint fk_Timeslot,
	                Timestamp datetime not null,
	                Cancelled bit not null default 0,
	                foreign key (PersonId) references Person (Id),
	                foreign key (TimeslotId) references Timeslot (Id),
                    constraint uc_Booking unique (PersonId, TimeslotId)
                )",
                @"create index ix_Booking_PersonId on Booking (PersonId)",
                @"create index ix_Booking_TimeslotId on Booking (TimeslotId)",
            };

            foreach (var sql in sqls)
            {
                _sqlRunner.EnqueueDatabaseCommand(con => con.Execute(sql));
            }
        }
    }
}
