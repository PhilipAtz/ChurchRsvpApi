using System;
using System.Data;

namespace ChurchWebApi.Services
{
    public interface ISqlRunner : IDisposable
    {
        T EnqueueDatabaseCommand<T>(Func<IDbConnection, T> query);
    }
}