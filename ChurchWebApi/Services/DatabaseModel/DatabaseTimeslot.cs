using ChurchWebApi.Services.AppModel;
using Dapper.Contrib.Extensions;
using System;

namespace ChurchWebApi.Services.DatabaseModel
{
    [Table("Timeslot")]
    public class DatabaseTimeslot
    {
        [Key]
        public long Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }

        public DatabaseTimeslot() { }

        public DatabaseTimeslot(Timeslot timeslot)
        {
            StartTime = timeslot.StartTime;
            EndTime = timeslot.EndTime;
            Capacity = timeslot.Capacity;
        }

        public Timeslot ToTimeslot()
        {
            return new Timeslot
            {
                StartTime = StartTime,
                EndTime = EndTime,
                Capacity = Capacity,
            };
        }
    }
}
