using ChurchWebApi.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ChurchWebApiTests
{
    [TestClass]
    public class EncryptionLayerTests
    {
        private const string SampleTestKey = "BD-E3-BF-45-28-47-5A-17-E4-FE-81-0F-C3-83-D2-C0-47-9B-50-24-C0-F2-3B-8A-81-04-FA-FE-96-69-1A-22";
        private const string SampleTestIv = "2C-0C-F5-C7-47-6B-B0-CA-CC-61-85-A1-19-1A-D1-3A";
        private static EncryptionLayer _encryptor;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            var keyRetriever = new Mock<ISecureKeyRetriever>();
            keyRetriever.Setup(x => x.RetrieveKey("EncryptionKey")).Returns(SampleTestKey);
            keyRetriever.Setup(x => x.RetrieveKey("InitialisationVector")).Returns(SampleTestIv);
            _encryptor = new EncryptionLayer(keyRetriever.Object);
        }

        [TestMethod]
        public void EncryptDecrypt()
        {
            var cipher = _encryptor.Encrypt("supercalifragilisticexpialidocious");
            var plaintext = _encryptor.Decrypt(cipher);

            Assert.AreEqual("supercalifragilisticexpialidocious", plaintext);
        }

        [TestMethod]
        public void EncryptDecryptEncrypt()
        {
            var plaintext1 = "supercalifragilisticexpialidocious";
            var cipher1 = _encryptor.Encrypt(plaintext1);
            var plaintext2 = _encryptor.Decrypt(cipher1);
            var cipher2 = _encryptor.Encrypt(plaintext2);

            Assert.AreEqual(cipher1, cipher2);
        }

        [TestMethod]
        public void EncryptionOfDifferentStrings()
        {
            var plaintext1 = "Test1";
            var plaintext2 = "Test2";
            var cipher1 = _encryptor.Encrypt(plaintext1);
            var cipher2 = _encryptor.Encrypt(plaintext2);

            Assert.AreNotEqual(plaintext1, plaintext2);
            Assert.AreNotEqual(cipher1, cipher2);
        }

        [TestMethod]
        public void EncryptionOfTheSameString()
        {
            var plaintext = "supercalifragilisticexpialidocious";
            var cipher1 = _encryptor.Encrypt(plaintext);
            var cipher2 = _encryptor.Encrypt(plaintext);

            Assert.AreEqual(cipher1, cipher2);
        }
    }
}
