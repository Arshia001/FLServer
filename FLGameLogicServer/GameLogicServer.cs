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


        public GameLogicServer(int numRounds) : base(0)
        {
            categories = Enumerable.Repeat(default(WordCategory), numRounds).ToList();
        }

        public GameLogicServer(IEnumerable<WordCategory> categories) : base(0)
        {
            this.categories = categories.ToList();
        }


        public StartRoundResult StartRound(int player, TimeSpan turnTime, out string category)
        {
            if (Finished)
            {
                category = null;
                return StartRoundResult.Error_GameFinished;
            }

            category = categories[RoundNumber].CategoryName;
            if (category == null)
                return StartRoundResult.MustChooseCategory;

            var result = base.StartRound(player, turnTime);

            if (!result.IsSuccess())
                category = "";

            return result;
        }

        public async Task<(PlayWordResult result, WordCategory category, string corrected, byte score)>
            PlayWord(int player, string word, GetWordScoreDelegate getWordScoreDelegate, Func<int, int> getMaxEditDistance)
        {
            if (turnEndTimes[player] < DateTime.Now)
                return (PlayWordResult.Error_TurnOver, null, null, 0);

            var category = categories[RoundNumber];
            var corrected = category.GetCorrectedWord(word, getMaxEditDistance);
            if (corrected == null)
            {
                corrected = word;
                getWordScoreDelegate = null;
            }

            var (notDuplicate, score) = await RegisterPlayedWordInternal(player, corrected, category, getWordScoreDelegate);
            return (notDuplicate ? PlayWordResult.Success : PlayWordResult.Duplicate, category, corrected, score);
        }

        async Task<(bool notDuplicate, byte score)> RegisterPlayedWordInternal(int player, string word, WordCategory category, GetWordScoreDelegate getWordScoreDelegate)
        {
            if (!playerAnswers[player][RoundNumber].Any(w => w.word == word))
            {
                var score = getWordScoreDelegate == null ? (byte)0 : await getWordScoreDelegate(category, word);
                playerAnswers[player][RoundNumber].Add(new WordScorePair(word, score));
                if (score > 0)
                    playerScores[player][RoundNumber] += score;

                return (true, score);
            }

            return (false, 0);
        }

        public SetCategoryResult SetCategory(int roundIndex, WordCategory category)
        {
            if (roundIndex >= categories.Count)
                return SetCategoryResult.Error_IndexOutOfBounds;

            if (categories[roundIndex] != null)
                return SetCategoryResult.Error_AlreadySet;

            if (roundIndex > 0 && categories[roundIndex - 1] == null)
                return SetCategoryResult.Error_PreviousCategoryNotSet;

            categories[roundIndex] = category;
            return SetCategoryResult.Success;
        }

        public DateTime? ExtendRoundTime(int player, TimeSpan amount)
        {
            if (!IsTurnInProgress(player))
                return null;

            turnEndTimes[player] += amount;
            return turnEndTimes[player];
        }

        //public void RestoreGameState(IEnumerable<WordCategory> categories, IEnumerable<IEnumerable<WordScorePair>>[] wordsPlayed, DateTime?[] turnEndTimes)
        //{
        //    this.categories = categories.ToList();

        //    base.RestoreGameState(wordsPlayed, turnEndTimes);
        //}
    }
}
