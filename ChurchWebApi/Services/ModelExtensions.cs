using System;
using ChurchWebApi.Services.AppModel;
using ChurchWebApi.Services.DatabaseModel;

namespace ChurchWebApi.Services
{
    public static class ModelExtensions
    {
        public static EncryptedDatabasePerson ToEncryptedDatabasePerson(
            this Person person,
            IEncryptionLayer encryptionLayer)
        {
            if (encryptionLayer == null)
            {
                throw new ArgumentNullException(nameof(encryptionLayer));
            }

            return new EncryptedDatabasePerson
            {
                Name = encryptionLayer.Encrypt(person.Name),
                Email = string.IsNullOrWhiteSpace(person.Email)
                    ? null
                    : encryptionLayer.Encrypt(person.Email),
                Mobile = string.IsNullOrWhiteSpace(person.Mobile)
                    ? null
                    : encryptionLayer.Encrypt(person.Mobile),
            };
        }

        public static DatabasePerson ToDatabasePerson(
            this EncryptedDatabasePerson person,
            IEncryptionLayer encryptionLayer)
        {
            if (encryptionLayer == null)
            {
                throw new ArgumentNullException(nameof(encryptionLayer));
            }

            return new DatabasePerson
            {
                Id = person.Id,
                Name = encryptionLayer.Decrypt(person.Name),
                Email = string.IsNullOrWhiteSpace(person.Email)
                    ? null
                    : encryptionLayer.Decrypt(person.Email),
                Mobile = string.IsNullOrWhiteSpace(person.Mobile)
                    ? null
                    : encryptionLayer.Decrypt(person.Mobile),
            };
        }

        public static Person ToPerson(this DatabasePerson person)
        {
            return new Person
            {
                Name = person.Name,
                Email = string.IsNullOrWhiteSpace(person.Email)
                    ? null
                    : person.Email,
                Mobile = string.IsNullOrWhiteSpace(person.Mobile)
                    ? null
                    : person.Mobile,
            };
        }

        public static DatabaseTimeslot ToDatabaseTimeslot(this Timeslot timeslot)
        {
            return new DatabaseTimeslot
            {
                StartTime = timeslot.StartTime,
                EndTime = timeslot.EndTime,
                Capacity = timeslot.Capacity,
            };
        }
    }
}