using GameApp.Repositories;

namespace GameApp.Data{
    internal class WordProvider{
        private readonly WordRepository wordRepository;

        public WordProvider(){
            wordRepository = new WordRepository();
        }

        public string GetRandomWord(){
            return wordRepository.GetRandomWord();
        }
    }
}