using System;
using System.Collections.Generic;
using System.Data;
using GameApp.Context;
using GameApp.Models;
using Npgsql;

namespace GameApp.Repositories
{
    internal class GameHistoryRepository
    {
        private readonly DbConnection dbConnection;

        public GameHistoryRepository(DbConnection? dbConnection = null)
        {
            this.dbConnection = dbConnection ?? new DbConnection();
        }

        public int SaveGame(GameHistory gameHistory, IReadOnlyList<GameMove> gameMoves)
        {
            using var connection = dbConnection.OpenConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var gameCommand = new NpgsqlCommand(@"
INSERT INTO games (player_id, hidden_word, attempts_used, score, is_win, started_at, finished_at)
VALUES (@player_id, @hidden_word, @attempts_used, @score, @is_win, @started_at, @finished_at)
RETURNING id;", connection, transaction);
                gameCommand.Parameters.AddWithValue("player_id", gameHistory.PlayerId);
                gameCommand.Parameters.AddWithValue("hidden_word", gameHistory.HiddenWord);
                gameCommand.Parameters.AddWithValue("attempts_used", gameHistory.AttemptsUsed);
                gameCommand.Parameters.AddWithValue("score", gameHistory.Score);
                gameCommand.Parameters.AddWithValue("is_win", gameHistory.IsWin);
                gameCommand.Parameters.AddWithValue("started_at", gameHistory.StartedAt);
                gameCommand.Parameters.AddWithValue("finished_at", gameHistory.FinishedAt);

                int gameId = Convert.ToInt32(gameCommand.ExecuteScalar());

                foreach (GameMove move in gameMoves)
                {
                    using var moveCommand = new NpgsqlCommand(@"
INSERT INTO game_moves (game_id, attempt_number, guess_word, feedback)
VALUES (@game_id, @attempt_number, @guess_word, @feedback);", connection, transaction);
                    moveCommand.Parameters.AddWithValue("game_id", gameId);
                    moveCommand.Parameters.AddWithValue("attempt_number", move.AttemptNumber);
                    moveCommand.Parameters.AddWithValue("guess_word", move.GuessWord);
                    moveCommand.Parameters.AddWithValue("feedback", move.Feedback);
                    moveCommand.ExecuteNonQuery();
                }

                transaction.Commit();
                return gameId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<GameHistory> GetGamesForPlayer(int playerId)
        {
            using var connection = dbConnection.OpenConnection();
            using var command = new NpgsqlCommand(@"
SELECT g.id, g.player_id, p.username, g.hidden_word, g.attempts_used, g.score, g.is_win, g.started_at, g.finished_at
FROM games g
INNER JOIN players p ON p.id = g.player_id
WHERE g.player_id = @player_id
ORDER BY g.finished_at DESC, g.id DESC;", connection);
            command.Parameters.AddWithValue("player_id", playerId);

            using var adapter = new NpgsqlDataAdapter(command);
            var table = new DataTable();
            adapter.Fill(table);

            var items = new List<GameHistory>();
            foreach (DataRow row in table.Rows)
            {
                items.Add(MapGameHistory(row));
            }

            return items;
        }

        public List<GameMove> GetMovesForGame(int gameId)
        {
            using var connection = dbConnection.OpenConnection();
            using var command = new NpgsqlCommand(@"
SELECT id, game_id, attempt_number, guess_word, feedback
FROM game_moves
WHERE game_id = @game_id
ORDER BY attempt_number ASC, id ASC;", connection);
            command.Parameters.AddWithValue("game_id", gameId);

            using var adapter = new NpgsqlDataAdapter(command);
            var table = new DataTable();
            adapter.Fill(table);

            var items = new List<GameMove>();
            foreach (DataRow row in table.Rows)
            {
                items.Add(MapGameMove(row));
            }

            return items;
        }

        private static GameHistory MapGameHistory(DataRow row)
        {
            return new GameHistory
            {
                Id = Convert.ToInt32(row["id"]),
                PlayerId = Convert.ToInt32(row["player_id"]),
                Username = Convert.ToString(row["username"]) ?? string.Empty,
                HiddenWord = Convert.ToString(row["hidden_word"]) ?? string.Empty,
                AttemptsUsed = Convert.ToInt32(row["attempts_used"]),
                Score = Convert.ToInt32(row["score"]),
                IsWin = Convert.ToBoolean(row["is_win"]),
                StartedAt = Convert.ToDateTime(row["started_at"]),
                FinishedAt = Convert.ToDateTime(row["finished_at"])
            };
        }

        private static GameMove MapGameMove(DataRow row)
        {
            return new GameMove
            {
                Id = Convert.ToInt32(row["id"]),
                GameId = Convert.ToInt32(row["game_id"]),
                AttemptNumber = Convert.ToInt32(row["attempt_number"]),
                GuessWord = Convert.ToString(row["guess_word"]) ?? string.Empty,
                Feedback = Convert.ToString(row["feedback"]) ?? string.Empty
            };
        }
    }
}