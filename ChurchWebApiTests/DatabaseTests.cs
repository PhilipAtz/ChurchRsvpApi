using System;
using System.IO;
using System.Linq;
using ChurchWebApi.Services;
using ChurchWebApi.Services.AppModel;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ChurchWebApiTests
{
    [TestClass]
    public class DatabaseTests
    {
        private static IDatabaseConnector _databaseConnector;
        private static IEncryptionLayer _encryptionLayer;
        private static ISqlRunner _sqlRunner;
        private static bool _theDatabaseWasDeleted;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            try
            {
                File.Delete("database.sqlite");
            }
            catch (Exception ex)
            { }
            _theDatabaseWasDeleted = !File.Exists("database.sqlite");

            var log = new Mock<ILogger<DatabaseConnector>>();
            _encryptionLayer = new EncryptionLayer(new SampleKeyRetriever());
            var databaseCreator = new DatabaseCreator();
            _sqlRunner = new SqliteRunner(log.Object, databaseCreator);
            _databaseConnector = new DatabaseConnector(log.Object, _encryptionLayer, _sqlRunner);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _sqlRunner.Dispose();
        }

        [TestMethod]
        public void PersonDatabaseTest()
        {
            long? result1 = null;
            if (_theDatabaseWasDeleted)
            {
                result1 = _databaseConnector.CreatePerson(new Person
                {
                    Name = "John Doe",
                    Email = "fake@email.com",
                });
                Assert.IsTrue(result1.HasValue);
            }

            var result2 = _databaseConnector.CreatePerson(new Person
            {
                Name = "John Doe",
                Email = "fake@email.com",
            });

            if (_theDatabaseWasDeleted)
            {
                Assert.AreEqual(result1, result2);
            }

            var person1 = _databaseConnector.GetDatabasePerson(new Person
            {
                Name = "John Doe",
            });
            Assert.IsNull(person1);

            var person2 = _databaseConnector.GetDatabasePerson(new Person
            {
                Name = "John Doe",
                Email = "fake@email.com",
                Mobile = "5555555",
            });
            Assert.IsNull(person2);

            var person3 = _databaseConnector.GetDatabasePerson(new Person
            {
                Name = "John Doe",
                Email = "fake@email.com",
            });
            Assert.AreEqual(result2, person3.Id);

            Assert.AreEqual("John Doe", person3.Name);
            Assert.AreEqual("fake@email.com", person3.Email);
            Assert.IsNull(person3.Mobile);

            var person4 = _databaseConnector.GetDatabasePerson(result2);
            Assert.AreEqual(person3.Id, person4.Id);
            Assert.AreEqual(person3.Name, person4.Name);
            Assert.AreEqual(person3.Email, person4.Email);
            Assert.IsNull(person4.Mobile);
        }

        [TestMethod]
        public void TimeslotDatabaseTest()
        {
            var startTime = DateTime.Now.AddDays(1);
            var endTime = startTime.AddHours(1);
            var timeslot1 = new Timeslot
            {
                StartTime = startTime,
                EndTime = endTime,
                Capacity = 10,
            };

            var id1 = _databaseConnector.CreateTimeslot(timeslot1);
            var id2 = _databaseConnector.CreateTimeslot(timeslot1);
            Assert.AreEqual(id1, id2);

            var timeslot2 = _databaseConnector.GetTimeslot(timeslot1.EndTime, timeslot1.EndTime.AddHours(1));
            Assert.IsNull(timeslot2);

            var timeslot3 = _databaseConnector.GetTimeslot(timeslot1.StartTime, timeslot1.EndTime);
            Assert.AreEqual(id1, timeslot3.Id);
            Assert.AreEqual(timeslot1.StartTime, timeslot3.StartTime);
            Assert.AreEqual(timeslot1.EndTime, timeslot3.EndTime);
            Assert.AreEqual(timeslot1.Capacity, timeslot3.Capacity);

            var timeslot4 = _databaseConnector.GetTimeslot(timeslot3.Id);
            Assert.AreEqual(timeslot3.Id, timeslot4.Id);
            Assert.AreEqual(timeslot3.StartTime, timeslot4.StartTime);
            Assert.AreEqual(timeslot3.EndTime, timeslot4.EndTime);
            Assert.AreEqual(timeslot3.Capacity, timeslot4.Capacity);
        }

        [TestMethod]
        public void BookingDatabaseTest()
        {
            var people = Enumerable.Range(1, 10)
                .Select(i => new Person { Name = $"Person #{i}" })
                .ToArray();
            
            var personIds = people.AsParallel().Select(person => _databaseConnector.CreatePerson(person)).ToArray();
            Assert.AreEqual(people.Length, personIds.Length);

            var startTime = DateTime.Now.AddDays(1);
            var endTime = startTime.AddHours(1);
            var timeslot = new Timeslot
            {
                StartTime = startTime,
                EndTime = endTime,
                Capacity = 10,
            };
            var id1 = _databaseConnector.CreateTimeslot(timeslot);

            var peopleWithBooking1 = people.AsParallel()
                .Select(person => new
                {
                    Person = person,
                    Booked = _databaseConnector.CreateBooking(person, timeslot.StartTime, timeslot.EndTime),
                })
                .Where(o => o.Booked)
                .Select(o => o.Person)
                .ToArray();

            Assert.AreEqual(timeslot.Capacity, peopleWithBooking1.Length);

            var peopleWithBooking2 = _databaseConnector.GetActiveBookings(timeslot.StartTime, timeslot.EndTime)
                .Select(o => _databaseConnector.GetDatabasePerson(o.PersonId))
                .ToArray();

            Assert.AreEqual(timeslot.Capacity, peopleWithBooking2.Length);

            foreach (var person in peopleWithBooking2)
            {
                Assert.IsTrue(peopleWithBooking1.Any(o => 
                    o.Name == person.Name &&
                    o.Email == person.Email &&
                    o.Mobile == person.Mobile));
            }
        }

        [TestMethod]
        public void TestEncryptDecryptPerson()
        {
            var person1 = new Person
            {
                Name = "John Doe",
                Email = "fake@email.com",
                Mobile = "5555555",
            };

            var encryptedPerson1 = person1.ToEncryptedDatabasePerson(_encryptionLayer);
            var person2 = encryptedPerson1.ToDatabasePerson(_encryptionLayer);
            var encryptedPerson2 = person2.ToPerson().ToEncryptedDatabasePerson(_encryptionLayer);

            Assert.AreEqual(person1.Name, person2.Name);
            Assert.AreEqual(person1.Email, person2.Email);
            Assert.AreEqual(person1.Mobile, person2.Mobile);

            Assert.AreEqual(encryptedPerson1.Name, encryptedPerson2.Name);
            Assert.AreEqual(encryptedPerson1.Email, encryptedPerson2.Email);
            Assert.AreEqual(encryptedPerson1.Mobile, encryptedPerson2.Mobile);
        }

        [TestMethod]
        public void TestMaximumFieldSize()
        {
            var person = new Person
            {
                Name = "1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
                Email = "12345678901234567890123456789012345678901234567890",
                Mobile = "123456789012345",
            };
            var encryptedPerson = person.ToEncryptedDatabasePerson(_encryptionLayer);
            Assert.IsTrue(encryptedPerson.Name.Length < DatabaseCreator.MaxNameLength);
            Assert.IsTrue(encryptedPerson.Email.Length < DatabaseCreator.MaxEmailLength);
            Assert.IsTrue(encryptedPerson.Mobile.Length < DatabaseCreator.MaxMobileLength);
        }
    }
}
