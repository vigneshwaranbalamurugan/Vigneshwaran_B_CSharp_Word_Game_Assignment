using System;
using System.Collections.Generic;

namespace GameApp.Models{
    internal class Game{
        public string HiddenWord { get; set; }
        public int MaxAttempts { get; } = 6;
        public int CurrAttempt { get; set; }
        public List<string> PreviousGuesses { get; set;} = new List<string>();

        public Game(string hiddenWord){
            HiddenWord = hiddenWord;
            CurrAttempt=0;
        }

        public bool IsCorrectGuess(string guessWord){
            return guessWord.Equals(HiddenWord);
        }

        public void Reset(string hiddenWord){
            HiddenWord = hiddenWord;
            CurrAttempt = 0;
            PreviousGuesses.Clear();
        }

        public void AddGuess(string guessWord){
            PreviousGuesses.Add(guessWord);
        }

        public override string ToString(){
            return $"----Word Game----\n Word: {HiddenWord} \n MaxAttempts: {MaxAttempts}\n CurrentAttempt: {CurrAttempt}\n PreviousGuesses: {string.Join(", ",PreviousGuesses)}\n ----Bye----";
        }
    }
}