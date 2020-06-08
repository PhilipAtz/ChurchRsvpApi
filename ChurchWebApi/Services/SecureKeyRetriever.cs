using System;

namespace ChurchWebApi.Services
{
    public class SecureKeyRetriever : ISecureKeyRetriever
	{
		public string RetrieveKey(string name)
        {
			const string key = "BD-E3-BF-45-28-47-5A-17-E4-FE-81-0F-C3-83-D2-C0-47-9B-50-24-C0-F2-3B-8A-81-04-FA-FE-96-69-1A-22";

			if (string.Equals(name, nameof(EncryptionLayer), StringComparison.OrdinalIgnoreCase))
			{
				return key;
			}

			return null;
		}
	}
}
