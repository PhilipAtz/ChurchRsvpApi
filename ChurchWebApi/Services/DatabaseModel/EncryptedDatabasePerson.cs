using Dapper.Contrib.Extensions;

namespace ChurchWebApi.Services.DatabaseModel
{
    [Table("Person")]
    public class EncryptedDatabasePerson
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
    }
}