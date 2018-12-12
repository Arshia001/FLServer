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

        public bool RegisterFullTurn(int player, uint round, IEnumerable<WordScorePair> wordsPlayed)
        {
            if (playerAnswers[player].Count != round)
                return false;

            ForceEndTurn(player);
            playerAnswers[player].Add(wordsPlayed.ToList());
            playerScores[player].Add((uint)wordsPlayed.Sum(t => t.score));
            return true;
        }

        public bool SetCategory(int round, string category)
        {
            if (categories[round] != null)
                return false;

            categories[round] = category;
            return true;
        }

        public void RestoreGameState(int numRounds, IEnumerable<string> categories, IEnumerable<IEnumerable<WordScorePair>>[] wordsPlayed, DateTime?[] turnEndTimes)
        {
            this.categories = categories.ToList();
            if (this.categories.Count < numRounds)
                this.categories.AddRange(Enumerable.Repeat(default(string), numRounds - this.categories.Count));

            base.RestoreGameState(wordsPlayed, turnEndTimes);
        }

        public bool ForceEndTurn(int player, uint roundNumber)
        {
            if (PlayerStartedTurn(player, RoundNumber) && !PlayerStartedTurn(player, RoundNumber + 1))
            {
                ForceEndTurn(player);
                return true;
            }

            return false;
        }
    }
}
