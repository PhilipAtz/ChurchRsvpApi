using System;
using System.IO;
using System.Security.Cryptography;

namespace ChurchWebApi.Services
{
    public class EncryptionLayer : IEncryptionLayer
	{
		private readonly byte[] _key;

		public EncryptionLayer(ISecureKeyRetriever secureKeyRetriever)
		{
			var key = secureKeyRetriever.RetrieveKey(nameof(EncryptionLayer));
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException();
			}
			_key = key.ToByteArray();
		}

		public string Encrypt(string plainText)
		{
			if (string.IsNullOrWhiteSpace(plainText))
			{
				return string.Empty;
			}

			ICryptoTransform encryptor;
			string initialisationVector;
			using (var algorithm = new AesManaged())
			{
				algorithm.Key = _key;
				initialisationVector = algorithm.IV.ToHexString();
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
					return $"{msEncrypt.ToArray().ToHexString()},{initialisationVector}";
				}
			}
		}

		public string Decrypt(string cipherText)
		{
			if (string.IsNullOrWhiteSpace(cipherText))
			{
				return string.Empty;
			}

			var splitValues = cipherText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (splitValues.Length != 2 ||
				string.IsNullOrWhiteSpace(splitValues[0]) ||
				string.IsNullOrWhiteSpace(splitValues[1]))
			{
				throw new ArgumentException($"Incorect {nameof(cipherText)} format: '{cipherText}'");
			}

			ICryptoTransform decryptor;
			using (var algorithm = new AesManaged())
			{
				algorithm.Key = _key;
				algorithm.IV = splitValues[1].ToByteArray();
				decryptor = algorithm.CreateDecryptor();
			}

			using (MemoryStream msDecrypt = new MemoryStream(splitValues[0].ToByteArray()))
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
