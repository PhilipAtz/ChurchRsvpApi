namespace ChurchWebApi.Services
{
    public interface IDatabaseCreator
    {
        void CreateTables(ISqlRunner sqlRunner);
    }
}