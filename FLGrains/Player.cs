using FLGrainInterfaces;
using LightMessage.OrleansUtils.Grains;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    class Player : SaveStateOnDeactivateGrain<PlayerState>, IPlayer
    {
        IConfigReader configReader;

        public Player(IConfigReader configReader) => this.configReader = configReader;

        public Task<Immutable<OwnPlayerInfo>> PerformStartupTasksAndGetInfo()
        {
            if (State.Level == 0)
            {
                //?? initialize stuff

                State.Gold = 1_000_000;
                State.Level = 1;

                var id = this.GetPrimaryKey();
                LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score).Set(id, 0).Ignore();
                LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.XP).Set(id, 0).Ignore();
            }

            return GetOwnPlayerInfo();
        }

        public Task<Immutable<IReadOnlyList<IGame>>> GetGames() =>
            Task.FromResult(State.ActiveGames.Concat(State.PastGames).ToList().AsImmutable<IReadOnlyList<IGame>>());

        public Task<Immutable<PlayerInfo>> GetPlayerInfo() => Task.FromResult(GetPlayerInfoImpl().AsImmutable());

        string GetName() => State.Name ?? $"Guest{this.GetPrimaryKey().ToString().Substring(0, 8)}";

        bool IsInfinitePlayActive => State.InfinitePlayEndTime > DateTime.Now;

        TimeSpan InfinitePlayTimeRemaining => State.InfinitePlayEndTime - DateTime.Now;

        public async Task<Immutable<OwnPlayerInfo>> GetOwnPlayerInfo()
        {
            var config = configReader.Config;
            var timeTillNextReward = config.ConfigValues.RoundWinRewardInterval - (DateTime.Now - State.LastRoundWinRewardTakeTime);

            return new OwnPlayerInfo
            {
                Name = GetName(),
                Level = State.Level,
                XP = State.XP,
                NextLevelXPThreshold = GetNextLevelRequiredXP(config),
                CurrentNumRoundsWonForReward = State.NumRoundsWonForReward,
                NextRoundWinRewardTimeRemaining = new TimeSpan(Math.Max(0L, timeTillNextReward.Ticks)),
                Score = State.Score,
                Rank = (uint)await LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score).GetRank(this.GetPrimaryKey()),
                Gold = State.Gold,
                InfinitePlayTimeRemaining = IsInfinitePlayActive ? InfinitePlayTimeRemaining : default(TimeSpan?)
            }.AsImmutable();
        }

        public Task<PlayerLeaderBoardInfo> GetLeaderBoardInfo() => Task.FromResult(new PlayerLeaderBoardInfo(State.Name));

        PlayerInfo GetPlayerInfoImpl() => new PlayerInfo { ID = this.GetPrimaryKey(), Name = GetName(), Level = State.Level }; //?? other info

        LevelConfig GetLevelConfig(ReadOnlyConfigData config) =>
            config.PlayerLevels.TryGetValue(State.Level, out var result) ? result : config.PlayerLevels[0];

        uint GetNextLevelRequiredXP(ReadOnlyConfigData config) => GetLevelConfig(config).GetRequiredXP(State);

        Task AddXP(uint delta)
        {
            if (delta == 0)
                return Task.CompletedTask;

            var reqXP = GetNextLevelRequiredXP(configReader.Config);

            State.XP += delta;
            if (State.XP >= reqXP)
            {
                ++State.Level;
                State.XP -= reqXP;
            }

            var id = this.GetPrimaryKey();

            LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.XP).AddDelta(id, delta).Ignore();

            return GrainFactory.GetGrain<ISystemEndPoint>(0).SendXPUpdated(id, State.XP, State.Level);
        }

        public Task<bool> CanEnterGame() => Task.FromResult(IsInfinitePlayActive || State.ActiveGames.Count < configReader.Config.ConfigValues.MaxActiveGames);

        public async Task<byte> JoinGameAsFirstPlayer(IGame game)
        {
            var result = await game.StartNew(this.GetPrimaryKey());
            State.ActiveGames.Add(game);
            return result;
        }

        public async Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game)
        {
            var result = await game.AddSecondPlayer(GetPlayerInfoImpl());
            State.ActiveGames.Add(game);
            return result;
        }

        public Task OnRoundWon(IGame game)
        {
            //?? convert MyGames to a HashSet? Would mess with ordering, but we probably need custom ordering based on time of last interaction anyway
            if (!State.ActiveGames.Contains(game))
                return Task.CompletedTask;

            ++State.NumRoundsWonForReward;
            return GrainFactory.GetGrain<ISystemEndPoint>(0).SendNumRoundsWonForRewardUpdated(this.GetPrimaryKey(), State.NumRoundsWonForReward);
        }

        Task<ulong> AddScore(int delta)
        {
            var lb = LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score);

            if (delta == 0)
                return lb.GetRank(this.GetPrimaryKey());

            State.Score = (uint)Math.Max(0, State.Score + delta);
            return lb.SetAndGetRank(this.GetPrimaryKey(), State.Score);
        }


        public async Task<(uint score, uint rank)> OnGameResult(IGame game, Guid? winnerID)
        {
            //?? gold rewards?

            State.ActiveGames.Remove(game);
            State.PastGames.Add(game);

            ulong rank;

            if (!winnerID.HasValue)
                rank = await AddScore(configReader.Config.ConfigValues.DrawDeltaScore);
            else if (winnerID.Value == this.GetPrimaryKey())
                rank = await AddScore(configReader.Config.ConfigValues.WinDeltaScore);
            else
                rank = await AddScore(configReader.Config.ConfigValues.LossDeltaScore);

            return (State.Score, (uint)rank);
        }

        public async Task<(ulong? gold, TimeSpan? remainingTime)> IncreaseRoundTime(Guid gameID)
        {
            var price = configReader.Config.ConfigValues.RoundTimeExtensionPrice;
            if (State.Gold < price)
                throw new VerbatimException("Insufficient gold");

            var time = await GrainFactory.GetGrain<IGame>(gameID).IncreaseRoundTime(this.GetPrimaryKey());
            if (time == null)
                return (null, null);

            State.Gold -= price;
            return (State.Gold, time.Value);
        }

        public async Task<(ulong? gold, string word, byte? wordScore)> RevealWord(Guid gameID)
        {
            var price = configReader.Config.ConfigValues.RevealWordPrice;
            if (State.Gold < price)
                throw new VerbatimException("Insufficient gold");

            var result = await GrainFactory.GetGrain<IGame>(gameID).RevealWord(this.GetPrimaryKey());
            if (result == null)
                return (null, null, null);

            State.Gold -= price;
            return (State.Gold, result.Value.word, result.Value.wordScore);
        }

        public async Task<IEnumerable<GroupInfoDTO>> RefreshGroups(Guid gameID)
        {
            var price = configReader.Config.ConfigValues.PriceToRefreshGroups;
            if (State.Gold < price)
                throw new VerbatimException("Insufficient gold");

            var result = await GrainFactory.GetGrain<IGame>(gameID).RefreshGroups(this.GetPrimaryKey());
            if (result == null)
                return null;

            State.Gold -= price;
            return result.Select(g => (GroupInfoDTO)g).ToList();
        }

        public Task<(ulong totalGold, TimeSpan nextRewardTime)> TakeRewardForWinningRounds()
        {
            var configValues = configReader.Config.ConfigValues;

            if (DateTime.Now - State.LastRoundWinRewardTakeTime < configValues.RoundWinRewardInterval)
                throw new VerbatimException("Interval not elapsed yet");

            if (State.NumRoundsWonForReward < configReader.Config.ConfigValues.NumRoundsToWinToGetReward)
                throw new VerbatimException("Insufficient rounds won");

            State.NumRoundsWonForReward = 0;
            State.LastRoundWinRewardTakeTime = DateTime.Now;

            State.Gold += configValues.NumGoldRewardForWinningRounds;

            return Task.FromResult((State.Gold, configValues.RoundWinRewardInterval));
        }

        public Task<(bool success, ulong totalGold, TimeSpan duration)> ActivateInfinitePlay()
        {
            if (IsInfinitePlayActive)
                return Task.FromResult((false, State.Gold, State.InfinitePlayEndTime - DateTime.Now));

            var config = configReader.Config.ConfigValues;
            if (State.Gold < config.InfinitePlayPrice)
                return Task.FromResult((false, 0UL, TimeSpan.Zero));

            State.Gold -= config.InfinitePlayPrice;

            var duration = config.InfinitePlayTime;
            State.InfinitePlayEndTime = DateTime.Now + duration;
            return Task.FromResult((true, State.Gold, duration));
        }

        public Task<(IEnumerable<string> words, ulong? totalGold)> GetAnswers(string category)
        {
            var config = configReader.Config;

            var words = config.CategoriesAsGameLogicFormatByName[category].Answers;
            var gold = default(ulong?);

            if (!State.OwnedCategoryAnswers.Contains(category))
            {
                var price = config.ConfigValues.GetAnswersPrice;
                if (State.Gold < price)
                    throw new VerbatimException("Insufficient gold");

                State.Gold -= price;
                State.OwnedCategoryAnswers.Add(category);
                gold = State.Gold;
            }

            return Task.FromResult((words.AsEnumerable(), gold));
        }

        public Task<Immutable<(PlayerInfo info, bool[] haveCategoryAnswers)>> GetPlayerInfoAndOwnedCategories(IReadOnlyList<string> categories)
        {
            var ownedCategories = new bool[categories.Count];
            for (int i = 0; i < categories.Count; ++i)
                ownedCategories[i] = State.OwnedCategoryAnswers.Contains(categories[i]);

            return Task.FromResult((GetPlayerInfoImpl(), ownedCategories).AsImmutable());
        }

        public Task<bool> HaveAnswersForCategory(string category) => Task.FromResult(State.OwnedCategoryAnswers.Contains(category));

        public Task<IReadOnlyList<bool>> HaveAnswersForCategories(IReadOnlyList<string> categories) =>
            Task.FromResult(categories.Select(c => State.OwnedCategoryAnswers.Contains(c)).ToList() as IReadOnlyList<bool>);
    }
}
