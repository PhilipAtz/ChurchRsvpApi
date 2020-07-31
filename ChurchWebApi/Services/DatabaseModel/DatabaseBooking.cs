using System;
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
    }
}
