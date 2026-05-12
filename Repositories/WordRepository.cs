using System;
using GameApp.Context;
using Npgsql;

namespace GameApp.Repositories
{
    internal class WordRepository
    {
        private readonly DbConnection dbConnection;

        public WordRepository(DbConnection? dbConnection = null)
        {
            this.dbConnection = dbConnection ?? new DbConnection();
        }

        public string GetRandomWord()
        {
            using var connection = dbConnection.OpenConnection();
            using var command = new NpgsqlCommand(@"
SELECT word_value
FROM words
WHERE is_active = TRUE
ORDER BY RANDOM()
LIMIT 1;", connection);

            object? result = command.ExecuteScalar();
            if (result == null)
            {
                throw new InvalidOperationException("No active words were found in the database.");
            }

            return Convert.ToString(result)!.Trim().ToUpperInvariant();
        }
    }
}