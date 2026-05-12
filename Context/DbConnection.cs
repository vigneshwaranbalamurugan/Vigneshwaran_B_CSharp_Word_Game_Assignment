using System;
using Npgsql;

namespace GameApp.Context
{
    public sealed class DbConnection : IDisposable
    {
        private readonly string _connectionString;

        public DbConnection(string? connectionString = null)
        {
            _connectionString = !string.IsNullOrWhiteSpace(connectionString)
                ? connectionString
                : Environment.GetEnvironmentVariable("GAMEAPP_CONNECTION_STRING")
                  ?? "Host=localhost;Port=5432;Username=postgres;Password=978681;Database=gameapp";
        }

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public NpgsqlConnection OpenConnection()
        {
            var connection = CreateConnection();
            connection.Open();
            return connection;
        }

        public void Dispose()
        {
        }
    }
}