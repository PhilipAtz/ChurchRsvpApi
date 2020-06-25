using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace ChurchWebApi.Services
{
    public class SqliteRunner : ISqlRunner
    {
        private const string DatabaseFile = "database.sqlite";
        private readonly ConcurrentQueue<Task<object>> _commandQueue = new ConcurrentQueue<Task<object>>();
        private readonly Task _queueConsumer;
        private bool _disposing;

        public SqliteRunner()
        {
            _queueConsumer = Task.Factory.StartNew(() =>
                {
                    while (!_disposing)
                    {
                        ConsumeFromQueue();
                    }
                },
                TaskCreationOptions.LongRunning);
        }

        public void Dispose()
        {
            _disposing = true;
            _queueConsumer.Dispose();
            while (!_commandQueue.IsEmpty)
            {
                ConsumeFromQueue();
            }
        }

        public T EnqueueDatabaseCommand<T>(Func<IDbConnection, T> query)
        {
            if (_disposing)
            {
                throw new ObjectDisposedException(nameof(SqliteRunner));
            }

            var task = new Task<object>(() => RunDatabaseCommand(query));
            _commandQueue.Enqueue(task);
            return (T)task.Result;
        }

        private void ConsumeFromQueue()
        {
            if (!_commandQueue.TryDequeue(out var task))
                return;

            task.Start();
            task.Wait();
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