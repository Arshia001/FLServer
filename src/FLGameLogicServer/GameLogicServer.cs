using FLGameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGameLogicServer
{
    public class GameLogicServer : GameLogic
    {
        public delegate Task<byte> GetWordScoreDelegate(WordCategory category, string word);

        public static GameLogicServer DeserializeFrom(SerializedGameData gameData, Func<string, WordCategory> getCategory) =>
            new GameLogicServer
            {
                categories = gameData.CategoryNames.Select(n => n == null ? null : getCategory(n)).ToList(),
                playerAnswers = gameData.PlayerAnswers.Select(l => l.Select(ll => ll.Select(ws => new WordScorePair(ws.word, ws.score)).ToList()).ToList()).ToArray(),
                playerScores = gameData.PlayerScores.Select(l => l.ToList()).ToArray(),
                turnEndTimes = gameData.TurnEndTimes.ToArray(),
                firstTurn = gameData.FirstTurn,
                expiryInterval = gameData.ExpiryInterval,
                ExpiryTime = gameData.ExpiryTime
            };

        List<WordCategory> categories;

        protected TimeSpan expiryInterval;
        
        public DateTime? ExpiryTime { get; private set; }

        public IReadOnlyList<WordCategory> Categories => categories;

        public override int NumRounds => Categories.Count;

        public HashSet<string> CategoryNames => new HashSet<string>(Categories.Where(c => c != null).Select(c => c.CategoryName));

        public override bool Expired => ExpiryTime.HasValue && DateTime.Now >= ExpiryTime.Value;

        public override int ExpiredFor => Turn;

        public GameLogicServer(int numRounds, TimeSpan expiryInterval) : base(0)
        {
            this.expiryInterval = expiryInterval;
            categories = Enumerable.Repeat(default(WordCategory), numRounds).ToList();
        }

        private GameLogicServer() : base() { }

        public StartRoundResult StartRound(int player, TimeSpan turnTime, out string category)
        {
            if (Finished || Expired)
            {
                category = null;
                return StartRoundResult.Error_GameFinished;
            }

            category = categories[RoundNumber]?.CategoryName;
            if (category == null)
                return StartRoundResult.MustChooseCategory;

            var result = base.StartRound(player, turnTime);

            if (!result.IsSuccess())
                category = "";

            if (!(player == 0 && NumTurnsTakenBy(player) == 0)) // No expiry time on first round, that's on us to find a match
                ExpiryTime = DateTime.Now + turnTime + expiryInterval;

            return result;
        }

        public void SecondPlayerJoined()
        {
            if (NumTurnsTakenBy(1) != 0)
                return;

            ExpiryTime = DateTime.Now + expiryInterval;
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

        public SerializedGameData Serialize() =>
            new SerializedGameData
            {
                CategoryNames = categories.Select(c => c?.CategoryName).ToList(),
                PlayerAnswers = playerAnswers.Select(l => l.Select(ll => ll.Select(ws => new SerializedGameData.WordScorePair(ws.word, ws.score)).ToList()).ToList()).ToArray(),
                PlayerScores = playerScores.Select(l => l.ToList()).ToArray(),
                TurnEndTimes = turnEndTimes.ToArray(),
                FirstTurn = firstTurn,
                ExpiryInterval = expiryInterval,
                ExpiryTime = ExpiryTime
            };

        public DateTime? ExtendRoundTime(int player, TimeSpan amount)
        {
            if (!IsTurnInProgress(player))
                return null;

            turnEndTimes[player] += amount;
            return turnEndTimes[player];
        }
    }
}
