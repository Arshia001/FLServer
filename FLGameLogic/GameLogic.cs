using System;
using System.Collections.Generic;
using System.Linq;

namespace FLGameLogic
{
    public class GameLogic
    {
        //?? does not support immediate lookup of round details
        List<WordCategory> categories;
        List<List<Tuple<string, byte>>>[] playerAnswers; // player no. -> turn no. -> (answer, score)*
        List<uint>[] playerScores;
        DateTime[] turnEndTimes;


        public IReadOnlyList<WordCategory> Categories => categories;

        public int RoundNumber => playerScores == null ? 0 :
            playerScores[0].Count == playerScores[1].Count ? playerScores[0].Count + (turnEndTimes.Any(t => t > DateTime.Now) ? -1 : 0) :
            Math.Min(playerScores[0].Count, playerScores[1].Count);

        public int FirstTurnThisRound => RoundNumber % 2; // player 0 gets round zero, player 1 gets round 1, etc.

        public int Turn => PlayerTookTurn(FirstTurnThisRound, RoundNumber) ? 1 - FirstTurnThisRound : FirstTurnThisRound;

        public bool Finished => categories.Count <= RoundNumber;


        public GameLogic(IEnumerable<WordCategory> categories)
        {
            this.categories = categories.ToList();
            playerAnswers = new[] { new List<List<Tuple<string, byte>>>(), new List<List<Tuple<string, byte>>>() };
            playerScores = new[] { new List<uint>(), new List<uint>() };
            turnEndTimes = new DateTime[2];
        }


        public IReadOnlyList<uint> GetPlayerScores(int player) => playerScores[player];

        public IReadOnlyList<Tuple<string, byte>> GetPlayerAnswers(int player, int round) => playerAnswers[player][round];

        public IReadOnlyList<IReadOnlyList<Tuple<string, byte>>> GetPlayerAnswers(int player) => playerAnswers[player];

        public DateTime GetTurnEndTime(int player) => turnEndTimes[player];

        public int NumTurnsTakenBy(int playerIndex) => playerScores[playerIndex].Count;

        public bool PlayerTookTurn(int index, int round) => playerScores[index].Count > round;

        public byte GetNumRoundsWon(int player)
        {
            var result = (byte)0;

            var numRoundsPlayed = Math.Min(playerScores[0].Count, playerScores[1].Count);
            for (int i = 0; i < numRoundsPlayed; ++i)
            {
                if (playerScores[player][i] >= playerScores[1 - player][i])
                    ++result;
            }

            return result;
        }

        public StartRoundResult StartRound(int player, TimeSpan turnTime, out string category)
        {
            category = "";

            if (Finished)
                return StartRoundResult.Error_GameFinished;

            if (PlayerTookTurn(player, RoundNumber))
                return StartRoundResult.Error_PlayerAlreadyTookTurn;

            if (Turn != player)
                return StartRoundResult.Error_NotThisPlayersTurn;

            category = categories[RoundNumber].CategoryName;

            turnEndTimes[player] = DateTime.Now + turnTime;
            playerScores[player].Add(0);
            playerAnswers[player].Add(new List<Tuple<string, byte>>());

            return StartRoundResult.Success;
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

            var duplicate = playerAnswers[player][RoundNumber].Any(t => t.Item1 == word);
            byte score = 0;
            if (!duplicate)
                categories[RoundNumber].WordsAndScores.TryGetValue(word, out score);

            playerAnswers[player][RoundNumber].Add(Tuple.Create(word, score));
            if (score > 0)
                playerScores[player][RoundNumber] += score;

            totalScore = playerScores[player][RoundNumber];
            thisWordScore = duplicate ? (sbyte)-1 : (sbyte)score;

            return PlayWordResult.Success;
        }
    }
}
