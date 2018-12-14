#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGameLogic
{
    public class GameLogicServer : GameLogic
    {
        List<WordCategory> categories;


        public IReadOnlyList<WordCategory> Categories => categories;

        public override int NumRounds => Categories.Count;


        public GameLogicServer(IEnumerable<WordCategory> categories)
        {
            this.categories = categories.ToList();
        }


        public new StartRoundResult StartRound(int player, TimeSpan turnTime, out string category)
        {
            category = categories[RoundNumber].CategoryName;

            var result = base.StartRound(player, turnTime);

            if (!result.IsSuccess())
                category = "";

            return result;
        }

        public PlayWordResult PlayWord(int player, string word, out uint totalScore, out sbyte thisWordScore, out string corrected)
        {
            totalScore = 0;
            thisWordScore = 0;
            corrected = null;

            if (turnEndTimes[player] < DateTime.Now)
                return PlayWordResult.Error_TurnOver;

            if (categories[RoundNumber].WordCorrections.TryGetValue(word, out corrected))
                word = corrected;

            var duplicate = playerAnswers[player][RoundNumber].Any(t => t.word == word);
            byte score = 0;
            if (!duplicate)
                categories[RoundNumber].WordsAndScores.TryGetValue(word, out score);

            RegisterPlayedWordInternal(player, word, score);

            totalScore = playerScores[player][RoundNumber];
            thisWordScore = duplicate ? (sbyte)-1 : (sbyte)score;

            return PlayWordResult.Success;
        }

        public void RestoreGameState(IEnumerable<WordCategory> categories, IEnumerable<IEnumerable<WordScorePair>>[] wordsPlayed, DateTime?[] turnEndTimes)
        {
            this.categories = categories.ToList();

            base.RestoreGameState(wordsPlayed, turnEndTimes);
        }
    }
}

#endif