namespace ChurchWebApi.Services
{
    public interface IEncryptionLayer
    {
		string Encrypt(string plainText);
		string Decrypt(string cipherText);
	}
}
