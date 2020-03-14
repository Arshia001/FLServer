﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGameLogic
{
    public class GameLogicClient : GameLogic
    {
        static readonly List<WordScorePair> unknownRoundAnswers = new List<WordScorePair>();


        public static GameLogicClient CreateFromState(int numRounds, IEnumerable<string> categories,
            IEnumerable<IEnumerable<WordScorePair>>[] wordsPlayed, DateTime?[] turnEndTimes, int firstTurn, bool expired,
            int expiredFor)
        {
            var result = new GameLogicClient(firstTurn);
            result.RestoreGameState(numRounds, categories, wordsPlayed, turnEndTimes, expired, expiredFor);
            return result;
        }

        List<string> categories;
        bool expired;
        int expiredFor;

        public IReadOnlyList<string> Categories => categories;

        public override int NumRounds => Categories.Count;

        public override bool Expired => expired;

        public override int ExpiredFor => expiredFor;

        private GameLogicClient(int firstTurn) : base(firstTurn) { }

        public GameLogicClient(int numRounds, int firstTurn) : base(firstTurn)
        {
            categories = new List<string>(Enumerable.Repeat(default(string), numRounds));
        }


        public bool RegisterFullTurn(int player, uint round, IEnumerable<WordScorePair> wordsPlayed)
        {
            if (!(playerAnswers[player].Count == round || playerAnswers[player].Count == round + 1 && playerAnswers[player][(int)round] == unknownRoundAnswers))
                return false;

            ForceEndTurn(player);

            if (playerAnswers[player].Count == round)
            {
                playerAnswers[player].Add(wordsPlayed.ToList());
                playerScores[player].Add((uint)wordsPlayed.Sum(t => t.score));
            }
            else
            {
                playerAnswers[player][(int)round] = wordsPlayed.ToList();
                playerScores[player][(int)round] = (uint)wordsPlayed.Sum(t => t.score);
            }

            return true;
        }

        public bool RegisterTurnTakenWithUnknownPlays(int player, uint round)
        {
            if (playerAnswers[player].Count != round)
                return false;

            ForceEndTurn(player);
            playerAnswers[player].Add(unknownRoundAnswers);
            playerScores[player].Add(0);
            return true;
        }

        public bool SetCategory(int round, string category)
        {
            if (categories[round] != null)
                return false;

            categories[round] = category;
            return true;
        }

        public void RestoreGameState(int numRounds, IEnumerable<string> categories,
            IEnumerable<IEnumerable<WordScorePair>>[] wordsPlayed, DateTime?[] turnEndTimes, bool expired,
            int expiredFor)
        {
            this.categories = categories.ToList();
            if (this.categories.Count < numRounds)
                this.categories.AddRange(Enumerable.Repeat(default(string), numRounds - this.categories.Count));

            this.expired = expired;
            this.expiredFor = expiredFor;

            base.RestoreGameState(wordsPlayed, turnEndTimes);
        }

        public new StartRoundResult StartRound(int player, TimeSpan turnTime) => base.StartRound(player, turnTime);

        public bool ForceEndTurn(int player, int roundNumber)
        {
            if (PlayerStartedTurn(player, roundNumber) && !PlayerStartedTurn(player, roundNumber + 1))
            {
                ForceEndTurn(player);
                return true;
            }

            return false;
        }

        public bool RegisterPlayedWord(int player, string word, byte score)
        {
            if (!playerAnswers[player][RoundNumber].Any(w => w.word == word))
            {
                playerAnswers[player][RoundNumber].Add(new WordScorePair(word, score));
                if (score > 0)
                    playerScores[player][RoundNumber] += score;

                return true;
            }

            return false;
        }
    }
}