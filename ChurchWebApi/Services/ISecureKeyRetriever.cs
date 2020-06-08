namespace ChurchWebApi.Services
{
    public interface ISecureKeyRetriever
    {
		string RetrieveKey(string name);

	}
}
