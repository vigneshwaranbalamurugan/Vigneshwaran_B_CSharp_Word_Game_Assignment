using System;

namespace GameApp.Models
{
    internal class GameHistory
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string HiddenWord { get; set; } = string.Empty;
        public int AttemptsUsed { get; set; }
        public int Score { get; set; }
        public bool IsWin { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
    }
}