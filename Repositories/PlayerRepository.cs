using System;
using GameApp.Context;
using GameApp.Models;
using Npgsql;

namespace GameApp.Repositories
{
    internal class PlayerRepository
    {
        private readonly DbConnection dbConnection;

        public PlayerRepository(DbConnection? dbConnection = null)
        {
            this.dbConnection = dbConnection ?? new DbConnection();
        }

        public Player? GetByUsername(string username)
        {
            using var connection = dbConnection.OpenConnection();
            using var command = new NpgsqlCommand(@"
SELECT id, username, password_hash, created_at
FROM players
WHERE LOWER(username) = LOWER(@username)
LIMIT 1;", connection);
            command.Parameters.AddWithValue("username", username.Trim());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapPlayer(reader);
        }

        public Player Create(string username, string passwordHash)
        {
            using var connection = dbConnection.OpenConnection();
            using var command = new NpgsqlCommand(@"
INSERT INTO players (username, password_hash)
VALUES (@username, @password_hash)
RETURNING id, username, password_hash, created_at;", connection);
            command.Parameters.AddWithValue("username", username.Trim());
            command.Parameters.AddWithValue("password_hash", passwordHash);

            using var reader = command.ExecuteReader();
            reader.Read();
            return MapPlayer(reader);
        }

        private static Player MapPlayer(NpgsqlDataReader reader)
        {
            return new Player
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Username = reader.GetString(reader.GetOrdinal("username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }
    }
}