using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGameLogic
{
    public class GameLogicClient : GameLogic
    {
        List<string> categories;


        public IReadOnlyList<string> Categories => categories;

        public override int NumRounds => Categories.Count;


        public GameLogicClient(int numRounds)
        {
            categories = new List<string>(Enumerable.Repeat(default(string), numRounds));
        }


        public void RegisterPlayedWord(int player, string word, byte score) => RegisterPlayedWordInternal(player, word, score);

        public void RegisterFullTurn(int player, int round, IEnumerable<WordScorePair> wordsPlayed)
        {
            if (playerAnswers[player].Count != round)
                throw new Exception("Incorrect round number");

            ForceEndTurn(player);
            playerAnswers[player].Add(wordsPlayed.ToList());
            playerScores[player].Add((uint)wordsPlayed.Sum(t => t.score));
        }

        public void SetCategory(int round, string category)
        {
            if (categories[round] != null)
                throw new Exception($"Category for round {round} is already known");

            categories[round] = category;
        }

        public void RestoreGameState(int numRounds, IEnumerable<string> categories, IEnumerable<IEnumerable<WordScorePair>>[] wordsPlayed, DateTime?[] turnEndTimes)
        {
            this.categories = categories.ToList();
            if (this.categories.Count < numRounds)
                this.categories.AddRange(Enumerable.Repeat(default(string), numRounds - this.categories.Count));

            base.RestoreGameState(wordsPlayed, turnEndTimes);
        }
    }
}
