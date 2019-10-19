using System;
using System.Collections.Generic;
using System.Linq;

namespace FLGameLogic
{
    public abstract class GameLogic
    {
        protected List<List<WordScorePair>>[] playerAnswers; // player no. -> turn no. -> (answer, score)*
        protected List<uint>[] playerScores;
        protected DateTime[] turnEndTimes;
        protected int firstTurn;


        public abstract int NumRounds { get; }

        public int RoundNumber => playerScores == null ? 0 :
            playerScores[0].Count == playerScores[1].Count ? playerScores[0].Count + (turnEndTimes.Any(t => t > DateTime.Now) ? -1 : 0) :
            Math.Min(playerScores[0].Count, playerScores[1].Count);

        public int FirstTurn => firstTurn;

        public int FirstTurnThisRound => RoundNumber % 2 == 0 ? firstTurn : (1 - firstTurn); // players take the first turn playing each round alternatively

        public int Turn => PlayerFinishedTurn(FirstTurnThisRound, RoundNumber) ? 1 - FirstTurnThisRound : FirstTurnThisRound;

        public bool Finished => NumRounds <= RoundNumber;


        protected GameLogic(int firstTurn)
        {
            playerAnswers = new[] { new List<List<WordScorePair>>(), new List<List<WordScorePair>>() };
            playerScores = new[] { new List<uint>(), new List<uint>() };
            turnEndTimes = new DateTime[2];
            this.firstTurn = firstTurn;
        }

        protected GameLogic() { } // For use in deserialization scenarios


        public IReadOnlyList<uint> GetPlayerScores(int player) => playerScores[player];

        public IReadOnlyList<WordScorePair> GetPlayerAnswers(int player, int round) => playerAnswers[player].Count > round ? playerAnswers[player][round] : new List<WordScorePair>();

        public IReadOnlyList<IReadOnlyList<WordScorePair>> GetPlayerAnswers(int player) => playerAnswers[player];

        public DateTime GetTurnEndTime(int player) => turnEndTimes[player];

        public bool IsTurnInProgress(int player) => turnEndTimes[player] > DateTime.Now;

        public int NumTurnsTakenBy(int player) => playerScores[player].Count + (turnEndTimes[player] > DateTime.Now ? -1 : 0);

        public int NumTurnsTakenByIncludingCurrent(int player) => playerScores[player].Count;

        public bool PlayerStartedTurn(int index, int round) => playerScores[index].Count > round;

        public bool PlayerFinishedTurn(int index, int round) => playerScores[index].Count > round + 1 || playerScores[index].Count > round && turnEndTimes[index] < DateTime.Now;

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

        public void ForceEndTurn(int player)
        {
            var now = DateTime.Now;
            if (turnEndTimes[player] > now)
                turnEndTimes[player] = now;
        }

        protected void RestoreGameState(IEnumerable<IEnumerable<WordScorePair>>[] wordsPlayed, DateTime?[] turnEndTimes)
        {
            playerAnswers[0] = wordsPlayed[0].Select(r => r.ToList()).ToList();
            playerScores[0] = wordsPlayed[0].Select(r => (uint)r.Sum(t => (int)t.score)).ToList();

            playerAnswers[1] = wordsPlayed[1].Select(r => r.ToList()).ToList();
            playerScores[1] = wordsPlayed[1].Select(r => (uint)r.Sum(t => (int)t.score)).ToList();

            if (turnEndTimes != null)
            {
                if (turnEndTimes[0] != null)
                    this.turnEndTimes[0] = turnEndTimes[0].Value;

                if (turnEndTimes[1] != null)
                    this.turnEndTimes[1] = turnEndTimes[1].Value;
            }
        }

        protected StartRoundResult StartRound(int player, TimeSpan turnTime)
        {
            if (Finished)
                return StartRoundResult.Error_GameFinished;

            if (PlayerStartedTurn(player, RoundNumber))
                return StartRoundResult.Error_PlayerAlreadyTookTurn;

            if (Turn != player)
                return StartRoundResult.Error_NotThisPlayersTurn;

            turnEndTimes[player] = DateTime.Now + turnTime;
            playerScores[player].Add(0);
            playerAnswers[player].Add(new List<WordScorePair>());

            return StartRoundResult.Success;
        }
    }
}
