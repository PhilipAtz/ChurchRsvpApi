using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ChurchWebApi.Services
{
    public class EncryptionLayer : IEncryptionLayer
	{
		private readonly byte[] _key;
		private readonly byte[] _iv;

		public EncryptionLayer(ISecureKeyRetriever secureKeyRetriever)
		{
			var key = secureKeyRetriever.RetrieveKey("EncryptionKey");
			var iv = secureKeyRetriever.RetrieveKey("InitialisationVector");
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(iv))
			{
				throw new ArgumentException();
			}
			_key = key.ToByteArray();
			_iv = iv.ToByteArray();
		}

		public string Encrypt(string plainText)
		{
			if (string.IsNullOrWhiteSpace(plainText))
			{
				return string.Empty;
			}

			ICryptoTransform encryptor;
			using (var algorithm = new AesManaged())
			{
				algorithm.IV = _iv;

				algorithm.Key = _key;
				encryptor = algorithm.CreateEncryptor();
			}

			using (var msEncrypt = new MemoryStream())
			{
				using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
				{
					using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
					{
						swEncrypt.Write(plainText);
					}
					return msEncrypt.ToArray().ToHexString();
				}
			}
		}

		public string Decrypt(string cipherText)
		{
			if (string.IsNullOrWhiteSpace(cipherText))
			{
				return string.Empty;
			}

			ICryptoTransform decryptor;
			using (var algorithm = new AesManaged())
			{
				algorithm.Key = _key;
				algorithm.IV = _iv;
				decryptor = algorithm.CreateDecryptor();
			}

			using (MemoryStream msDecrypt = new MemoryStream(cipherText.ToByteArray()))
			{
				using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
				{
					using (StreamReader srDecrypt = new StreamReader(csDecrypt))
					{
						return srDecrypt.ReadToEnd();
					}
				}
			}
		}
	}
}
