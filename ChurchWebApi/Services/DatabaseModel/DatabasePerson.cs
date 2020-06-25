using ChurchWebApi.Services.AppModel;
using Dapper.Contrib.Extensions;

namespace ChurchWebApi.Services.DatabaseModel
{
    [Table("Person")]
    public class DatabasePerson
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }

        public DatabasePerson() { }

        public DatabasePerson(
            Person person,
            IEncryptionLayer encryptionLayer)
        {
            if (encryptionLayer == null)
            {
                Name = person.Name;
                Email = string.IsNullOrWhiteSpace(person.Email)
                    ? null
                    : person.Email;
                Mobile = string.IsNullOrWhiteSpace(person.Mobile)
                    ? null
                    : person.Mobile;
                return;
            }

            Name = encryptionLayer.Encrypt(person.Name);
            Email = string.IsNullOrWhiteSpace(person.Email)
                ? null
                : encryptionLayer.Encrypt(person.Email);
            Mobile = string.IsNullOrWhiteSpace(person.Mobile)
                ? null
                : encryptionLayer.Encrypt(person.Mobile);
        }

        public Person Decrypt(
            IEncryptionLayer encryptionLayer)
        {
            if (encryptionLayer == null)
            {
                return new Person
                {
                    Name = Name,
                    Email = string.IsNullOrWhiteSpace(Email)
                        ? null
                        : Email,
                    Mobile = string.IsNullOrWhiteSpace(Mobile)
                        ? null
                        : Mobile,
                };
            }

            return new Person
            {
                Name = encryptionLayer.Decrypt(Name),
                Email = string.IsNullOrWhiteSpace(Email)
                    ? null
                    : encryptionLayer.Decrypt(Email),
                Mobile = string.IsNullOrWhiteSpace(Mobile)
                    ? null
                    : encryptionLayer.Decrypt(Mobile),
            };
        }
    }
}
