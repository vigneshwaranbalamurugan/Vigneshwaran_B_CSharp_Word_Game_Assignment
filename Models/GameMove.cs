namespace GameApp.Models
{
    internal class GameMove
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int AttemptNumber { get; set; }
        public string GuessWord { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
    }
}