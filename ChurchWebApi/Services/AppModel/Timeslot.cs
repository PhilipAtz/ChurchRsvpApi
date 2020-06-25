using System;

namespace ChurchWebApi.Services.AppModel
{
    public class Timeslot
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Capacity { get; set; }
    }
}
