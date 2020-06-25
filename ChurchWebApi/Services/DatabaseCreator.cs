using Dapper;

namespace ChurchWebApi.Services
{
    public class DatabaseCreator : IDatabaseCreator
    {
        public static int MaxNameLength = 350;
        public static int MaxEmailLength = 200;
        public static int MaxMobileLength = 50;

        public void CreateTables(ISqlRunner sqlRunner)
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
                sqlRunner.EnqueueDatabaseCommand(con => con.Execute(sql));
            }
        }
    }
}