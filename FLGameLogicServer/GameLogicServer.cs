using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGameLogic
{
    public class GameLogicServer : GameLogic
    {
        public delegate Task<byte> GetWordScoreDelegate(WordCategory category, string word);


        List<WordCategory> categories;


        public IReadOnlyList<WordCategory> Categories => categories;

        public override int NumRounds => Categories.Count;


        public GameLogicServer(IEnumerable<WordCategory> categories) : base(0)
        {
            this.categories = categories.ToList();
        }


        public StartRoundResult StartRound(int player, TimeSpan turnTime, out string category)
        {
            category = categories[RoundNumber].CategoryName;

            var result = base.StartRound(player, turnTime);

            if (!result.IsSuccess())
                category = "";

            return result;
        }

        public async Task<(PlayWordResult result, WordCategory category, string corrected, byte score)> PlayWord(int player, string word, GetWordScoreDelegate getWordScoreDelegate)
        {
            if (turnEndTimes[player] < DateTime.Now)
                return (PlayWordResult.Error_TurnOver, null, null, 0);

            var category = categories[RoundNumber];
            var corrected = category.GetCorrectedWord(word);
            if (corrected == null)
                return (PlayWordResult.Success, null, null, 0);

            var duplicate = playerAnswers[player][RoundNumber].Any(t => t.word == word);
            if (duplicate)
                return (PlayWordResult.Success, category, corrected, 0);

            var score = await getWordScoreDelegate(category, word);
            RegisterPlayedWordInternal(player, word, score);
            return (PlayWordResult.Success, category, corrected, score);
        }

        public void RestoreGameState(IEnumerable<WordCategory> categories, IEnumerable<IEnumerable<WordScorePair>>[] wordsPlayed, DateTime?[] turnEndTimes)
        {
            this.categories = categories.ToList();

            base.RestoreGameState(wordsPlayed, turnEndTimes);
        }
    }
}
