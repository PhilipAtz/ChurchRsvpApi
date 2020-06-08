using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace ChurchWebApi.Services
{
    public class DatabaseConnector : IDatabaseConnector
    {
        private const string DatabaseFile = "database.sqlite";
        private readonly ILogger<DatabaseConnector> _log;

        public DatabaseConnector(ILogger<DatabaseConnector> logger)
        {
            _log = logger;

            if (File.Exists(DatabaseFile))
                return;

            _log.LogWarning($"Could not locate database file '{DatabaseFile}', creating a new one.");
            SQLiteConnection.CreateFile(DatabaseFile);
            CreateDatabaseTables();
        }

        private void CreateDatabaseTables()
        {
            RunDatabaseCommand(con => con.Execute("create table Registration (Name varchar(20), Value int)"));
        }

        private T RunDatabaseCommand<T>(Func<IDbConnection, T> query)
        {
            using (IDbConnection connection = new SQLiteConnection(@$"Data Source={DatabaseFile}"))
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                return query(connection);
            }
        }
    }
}
