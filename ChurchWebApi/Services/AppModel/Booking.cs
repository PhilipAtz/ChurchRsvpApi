using System;

namespace ChurchWebApi.Services.AppModel
{
    public class Booking
    {
        public Person Person { get; set; }
        public Timeslot Timeslot { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Cancelled { get; set; }
    }
}
