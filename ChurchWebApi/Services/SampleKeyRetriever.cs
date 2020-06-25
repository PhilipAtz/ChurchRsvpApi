using System;

namespace ChurchWebApi.Services
{
    public class SampleKeyRetriever : ISecureKeyRetriever
	{
		public string RetrieveKey(string name)
        {
			// TODO: These are a meaningless sample key that will be later replaced with access to an Azure Key Vault.
			const string key = "BD-E3-BF-45-28-47-5A-17-E4-FE-81-0F-C3-83-D2-C0-47-9B-50-24-C0-F2-3B-8A-81-04-FA-FE-96-69-1A-22";
			const string initialisationVector = "2C-0C-F5-C7-47-6B-B0-CA-CC-61-85-A1-19-1A-D1-3A";

			if (string.Equals(name, "EncryptionKey", StringComparison.OrdinalIgnoreCase))
			{
				return key;
			}

			if (string.Equals(name, "InitialisationVector", StringComparison.OrdinalIgnoreCase))
			{
				return initialisationVector;
			}

			return null;
		}
	}
}
