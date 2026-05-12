using System;
using System.Collections.Generic;
using System.Linq;
using GameApp.Data;
using GameApp.Models;
using GameApp.Interfaces;
using GameApp.Exceptions.GuessWord;
using GameApp.Inputs;
using GameApp.Repositories;

namespace GameApp.Services{
    internal class GameService:IGameInteract{
        private readonly GuessInputValidator guessValidator;
        private readonly FeedbackGenerator feedbackGenerator;
        private readonly WordProvider wordProvider;
        private readonly PlayerRepository playerRepository;
        private readonly GameHistoryRepository gameHistoryRepository;
        private readonly PasswordHasher passwordHasher;
        private Player? currentPlayer;
        private Game? game;
        private readonly List<GameMove> currentMoves;

        public GameService(){
            guessValidator = new GuessInputValidator();
            feedbackGenerator = new FeedbackGenerator();
            wordProvider = new WordProvider();
            playerRepository = new PlayerRepository();
            gameHistoryRepository = new GameHistoryRepository();
            passwordHasher = new PasswordHasher();
            currentMoves = new List<GameMove>();
        }

        public void StartGame(){
            AuthenticatePlayer();
            ShowMenu();
        }

        private void AuthenticatePlayer(){
            while(currentPlayer == null){
                Console.WriteLine("------------------------------------------------");
                Console.WriteLine("1. Login");
                Console.WriteLine("2. Create Account");
                Console.WriteLine("3. Exit");
                Console.Write("Choose an option: ");
                string choice = (Console.ReadLine() ?? "").Trim();

                switch(choice){
                    case "1":
                        LoginPlayer();
                        break;
                    case "2":
                        CreateAccount();
                        break;
                    case "3":
                        Console.WriteLine("Goodbye!");
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private void LoginPlayer(){
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Login");
            Console.Write("Username: ");
            string username = Console.ReadLine() ?? "";

            if(string.IsNullOrWhiteSpace(username)){
                Console.WriteLine("Username cannot be empty.");
                return;
            }

            Player? existingPlayer = playerRepository.GetByUsername(username);
            if(existingPlayer == null){
                Console.WriteLine("Account not found. Please create a new account.");
                return;
            }

            string password = ReadPassword();
            if(passwordHasher.VerifyPassword(password, existingPlayer.PasswordHash)){
                currentPlayer = existingPlayer;
                Console.WriteLine($"Welcome back, {currentPlayer.Username}.");
                return;
            }

            Console.WriteLine("Invalid password. Try again.");
        }

        private void CreateAccount(){
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Create Account");
            Console.Write("Username: ");
            string username = Console.ReadLine() ?? "";

            if(string.IsNullOrWhiteSpace(username)){
                Console.WriteLine("Username cannot be empty.");
                return;
            }

            Player? existingPlayer = playerRepository.GetByUsername(username);
            if(existingPlayer != null){
                Console.WriteLine("Username already exists. Please login or choose a different username.");
                return;
            }

            string password = ReadPassword();
            if(string.IsNullOrWhiteSpace(password)){
                Console.WriteLine("Password cannot be empty.");
                return;
            }

            try{
                currentPlayer = playerRepository.Create(username, passwordHasher.HashPassword(password));
                Console.WriteLine($"Account created successfully. Welcome, {currentPlayer.Username}!");
            }catch(Exception ex){
                Console.WriteLine($"Error creating account: {ex.Message}");
            }
        }

        private void ShowMenu(){
            while(true){
                Console.WriteLine("------------------------------------------------");
                Console.WriteLine("1. Play new game");
                Console.WriteLine("2. View previous games");
                Console.WriteLine("3. Replay a previous game");
                Console.WriteLine("4. Exit");
                Console.Write("Choose an option: ");
                
                string choice = (Console.ReadLine() ?? "").Trim();

                switch(choice){
                    case "1":
                        StartNewGame();
                        break;
                    case "2":
                        ShowPreviousGames();
                        break;
                    case "3":
                        Replay();
                        break;
                    case "4":
                        Console.WriteLine("Thank you for playing! Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private void StartNewGame(){
            game = new Game(wordProvider.GetRandomWord());
            currentMoves.Clear();

            Console.WriteLine("------------------------------------------------");
            Console.WriteLine($"Welcome to the Word Guessing Game, {currentPlayer!.Username}!");
            Console.WriteLine($"You have {game.MaxAttempts} attempts to guess the hidden 5-letter word.\nGood luck!\n");
            Console.WriteLine("Instructions:");
            Console.WriteLine("G - Correct letter in the correct position (Green)");
            Console.WriteLine("Y - Correct letter in the wrong position (Yellow)");
            Console.WriteLine("X - Incorrect letter (Gray)\n");
            Console.WriteLine("Let's start the game!\n------------------------------------------------");

            bool isWin = false;
            DateTime startedAt = DateTime.UtcNow;

            while(game.CurrAttempt < game.MaxAttempts){
                Console.WriteLine($"Attempt {game.CurrAttempt + 1} of {game.MaxAttempts}");
                string guessWord = GetUserInput();
                string feedbackValue = feedbackGenerator.GenerateFeedback(guessWord, game.HiddenWord);

                game.AddGuess(guessWord);
                currentMoves.Add(new GameMove{
                    AttemptNumber = game.CurrAttempt + 1,
                    GuessWord = guessWord,
                    Feedback = feedbackValue
                });
                game.CurrAttempt = currentMoves.Count;

                DisplayFeedback(feedbackValue);

                if(game.IsCorrectGuess(guessWord)){
                    isWin = true;
                    Console.WriteLine($"{GetWinMessage(game.CurrAttempt)} Congratulations!\nYou guessed the word '{game.HiddenWord}' in {game.CurrAttempt} attempts!");
                    break;
                }

                if(game.CurrAttempt == game.MaxAttempts){
                    Console.WriteLine($"Game Over! The hidden word was '{game.HiddenWord}'.");
                }
            }

            int score = isWin ? (100 - (game.CurrAttempt - 1) * 15) : 0;
            var gameHistory = new GameHistory{
                PlayerId = currentPlayer!.Id,
                Username = currentPlayer.Username,
                HiddenWord = game.HiddenWord,
                AttemptsUsed = game.CurrAttempt,
                Score = score,
                IsWin = isWin,
                StartedAt = startedAt,
                FinishedAt = DateTime.UtcNow
            };

            int savedGameId = gameHistoryRepository.SaveGame(gameHistory, currentMoves);
            Console.WriteLine($"Game saved with score {score} (Game ID: {savedGameId}).");
        }

        private void ShowPreviousGames(){
            EnsureLoggedIn();
            List<GameHistory> games = gameHistoryRepository.GetGamesForPlayer(currentPlayer!.Id);

            if(games.Count == 0){
                Console.WriteLine("No previous games found.");
                return;
            }

            Console.WriteLine("Previous games:");
            foreach(GameHistory gameHistory in games){
                string result = gameHistory.IsWin ? "Win" : "Loss";
                Console.WriteLine($"#{gameHistory.Id} | {gameHistory.FinishedAt:g} | {result} | Score: {gameHistory.Score} | Attempts: {gameHistory.AttemptsUsed} | Word: {gameHistory.HiddenWord}");
            }
        }

        public void Replay(){
            EnsureLoggedIn();
            List<GameHistory> games = gameHistoryRepository.GetGamesForPlayer(currentPlayer!.Id);

            if(games.Count == 0){
                Console.WriteLine("No previous games to replay.");
                return;
            }

            Console.WriteLine("Enter the game ID to replay:");
            foreach(GameHistory gameHistory in games){
                string result = gameHistory.IsWin ? "Win" : "Loss";
                Console.WriteLine($"#{gameHistory.Id} | {gameHistory.FinishedAt:g} | {result} | Score: {gameHistory.Score}");
            }

            Console.Write("Game ID: ");
            if(!int.TryParse(Console.ReadLine(), out int selectedGameId)){
                Console.WriteLine("Invalid game ID.");
                return;
            }

            GameHistory? selectedGame = games.FirstOrDefault(gameHistory => gameHistory.Id == selectedGameId);
            if(selectedGame == null){
                Console.WriteLine("Game not found.");
                return;
            }

            List<GameMove> moves = gameHistoryRepository.GetMovesForGame(selectedGameId);
            Console.WriteLine($"Replay for game #{selectedGame.Id} against word '{selectedGame.HiddenWord}':");
            foreach(GameMove move in moves){
                Console.Write($"Attempt {move.AttemptNumber} ({move.GuessWord}): ");
                DisplayFeedback(move.Feedback);
            }
        }

        private void EnsureLoggedIn(){
            if(currentPlayer == null){
                throw new InvalidOperationException("Player session was not initialized.");
            }
        }
    
        private string GetUserInput()
        {
            try
            {
                Console.Write("Enter your guess: ");
                string guessWord = Console.ReadLine() ?? "";
                guessValidator.ValidateGuessInput(guessWord);

                return guessWord.ToUpper();
            }catch (EmptyInputException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
            catch (LessThanFiveLetterException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
            catch (MoreThanFiveLetterException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
            catch (InputContainingNumbersException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
            catch (InputContainingSpecialException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
    
        }

        private static string GetWinMessage(int attempts){
            return attempts switch
            {
                1 => "Genius!",
                2 => "Excellent!",
                3 => "Great job!",
                4 => "Good work!",
                5 => "Nice try!",
                6 => "Well done!",
                _ => "Congratulations!"
            };
        }

        private static void DisplayFeedback(string feedbackValue){
            for(int i = 0; i < feedbackValue.Length; i++){
                char c = feedbackValue[i];
                switch(char.ToUpper(c)){
                    case 'G':
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case 'Y':
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }
                Console.Write(c);
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        private string ReadPassword(){
            Console.Write("Password: ");
            var passwordCharacters = new List<char>();

            while(true){
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if(keyInfo.Key == ConsoleKey.Enter){
                    Console.WriteLine();
                    break;
                }

                if(keyInfo.Key == ConsoleKey.Backspace){
                    if(passwordCharacters.Count > 0){
                        passwordCharacters.RemoveAt(passwordCharacters.Count - 1);
                        if(Console.CursorLeft > 0){
                            Console.Write("\b \b");
                        }
                    }

                    continue;
                }

                passwordCharacters.Add(keyInfo.KeyChar);
                Console.Write("*");
            }

            return new string(passwordCharacters.ToArray());
        }
    }
}