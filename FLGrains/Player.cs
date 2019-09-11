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
    //?? MoneySpentCustomizations
    class Player : SaveStateOnDeactivateGrain<PlayerState>, IPlayer
    {
        IConfigReader configReader;

        ISystemEndPoint systemEndPoint { get; set; }

        public Player(IConfigReader configReader) => this.configReader = configReader;

        public override Task OnActivateAsync()
        {
            systemEndPoint = GrainFactory.GetGrain<ISystemEndPoint>(0);
            return base.OnActivateAsync();
        }

        public Task<OwnPlayerInfo> PerformStartupTasksAndGetInfo()
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

        public Task<PlayerInfo> GetPlayerInfo() => Task.FromResult(GetPlayerInfoImpl());

        string GetName() => State.Name ?? $"Guest{this.GetPrimaryKey().ToString().Substring(0, 8)}";

        bool IsInfinitePlayActive => State.InfinitePlayEndTime > DateTime.Now;

        TimeSpan InfinitePlayTimeRemaining => State.InfinitePlayEndTime - DateTime.Now;

        static bool ShouldReplicateStatToClient(Statistics stat)
        {
            switch(stat)
            {
                case Statistics.BestGameScore:
                case Statistics.BestRoundScore:
                case Statistics.GamesEndedInDraw:
                case Statistics.GamesLost:
                case Statistics.GamesWon:
                    return true;

                default:
                    return false;
            }
        }

        public async Task<OwnPlayerInfo> GetOwnPlayerInfo()
        {
            var config = configReader.Config;
            var timeTillNextReward = config.ConfigValues.RoundWinRewardInterval - (DateTime.Now - State.LastRoundWinRewardTakeTime);

            return new OwnPlayerInfo
            (
                name: GetName(),
                level: State.Level,
                xp: State.XP,
                nextLevelXPThreshold: GetNextLevelRequiredXP(config),
                currentNumRoundsWonForReward: State.NumRoundsWonForReward,
                nextRoundWinRewardTimeRemaining: new TimeSpan(Math.Max(0L, timeTillNextReward.Ticks)),
                score: State.Score,
                rank: (uint)await LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score).GetRank(this.GetPrimaryKey()),
                gold: State.Gold,
                infinitePlayTimeRemaining: IsInfinitePlayActive ? InfinitePlayTimeRemaining : default(TimeSpan?),
                statisticsValues: State.StatisticsValues.Where(kv => ShouldReplicateStatToClient(kv.Key.stat))
                    .Select(kv => new StatisticValue(kv.Key.stat, kv.Key.parameter, kv.Value))
            );
        }

        public Task<PlayerLeaderBoardInfo> GetLeaderBoardInfo() => Task.FromResult(new PlayerLeaderBoardInfo(State.Name));

        PlayerInfo GetPlayerInfoImpl() => new PlayerInfo(id: this.GetPrimaryKey(), name: GetName(), level: State.Level);

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

            return systemEndPoint.SendXPUpdated(id, State.XP, State.Level);
        }

        ulong GetStat(Statistics stat, int parameter = 0) =>
            State.StatisticsValues.TryGetValue((stat, parameter), out var value) ? value : 0UL;

        void UpdateStatImpl(Statistics stat, int parameter, ulong value)
        {
            if (GetStat(stat) != value)
            {
                State.StatisticsValues[(stat, parameter)] = value;
                if (ShouldReplicateStatToClient(stat))
                    systemEndPoint.SendStatisticUpdated(this.GetPrimaryKey(), new StatisticValue(stat, parameter, value)).Ignore();
            }
        }

        void SetStat(ulong value, Statistics stat, int parameter = 0) => UpdateStatImpl(stat, parameter, value);

        void SetMaxStat(ulong value, Statistics stat, int parameter = 0) => UpdateStatImpl(stat, parameter, Math.Max(GetStat(stat), value));

        void AddStatImpl(ulong value, Statistics stat, int parameter = 0) => UpdateStatImpl(stat, parameter, GetStat(stat) + value);

        public Task AddStats(List<StatisticValue> values)
        {
            foreach (var val in values)
                AddStatImpl(val.Value, val.Statistic, val.Parameter);
            return Task.CompletedTask;
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

        public Task OnRoundResult(IGame game, CompetitionResult result, uint myScore)
        {
            //?? convert MyGames to a HashSet? Would mess with ordering, but we probably need custom ordering based on time of last interaction anyway
            if (!State.ActiveGames.Contains(game))
                return Task.CompletedTask;

            SetMaxStat(myScore, Statistics.BestRoundScore);

            switch (result)
            {
                case CompetitionResult.Win:
                    AddStatImpl(1, Statistics.RoundsWon);
                    ++State.NumRoundsWonForReward;
                    return systemEndPoint.SendNumRoundsWonForRewardUpdated(this.GetPrimaryKey(), State.NumRoundsWonForReward);

                case CompetitionResult.Loss:
                    AddStatImpl(1, Statistics.RoundsLost);
                    return Task.CompletedTask;

                case CompetitionResult.Draw:
                    AddStatImpl(1, Statistics.RoundsEndedInDraw);
                    return Task.CompletedTask;

                default:
                    return Task.CompletedTask;
            }
        }

        Task<ulong> AddScore(int delta)
        {
            var lb = LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score);

            if (delta == 0)
                return lb.GetRank(this.GetPrimaryKey());

            State.Score = (uint)Math.Max(0, State.Score + delta);
            return lb.SetAndGetRank(this.GetPrimaryKey(), State.Score);
        }


        public async Task<(uint score, uint rank)> OnGameResult(IGame game, CompetitionResult result, uint myScore)
        {
            //?? gold rewards?
            //?? AddStat RewardMoneyEarned

            SetMaxStat(myScore, Statistics.BestGameScore);

            State.ActiveGames.Remove(game);
            State.PastGames.Add(game);

            ulong rank = 0;

            switch (result)
            {
                case CompetitionResult.Draw:
                    rank = await AddScore(configReader.Config.ConfigValues.DrawDeltaScore);
                    AddStatImpl(1, Statistics.GamesEndedInDraw);
                    break;

                case CompetitionResult.Win:
                    rank = await AddScore(configReader.Config.ConfigValues.WinDeltaScore);
                    AddStatImpl(1, Statistics.GamesWon);
                    break;

                case CompetitionResult.Loss:
                    rank = await AddScore(configReader.Config.ConfigValues.LossDeltaScore);
                    AddStatImpl(1, Statistics.GamesLost);
                    break;
            }

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

            AddStatImpl(price, Statistics.MoneySpentTimePowerup);
            AddStatImpl(1, Statistics.TimePowerupUsed);

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

            AddStatImpl(price, Statistics.MoneySpentHelpPowerup);
            AddStatImpl(1, Statistics.HelpPowerupUsed);

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

            AddStatImpl(price, Statistics.MoneySpentGroupChange);
            AddStatImpl(1, Statistics.GroupChangeUsed);

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

            AddStatImpl(configValues.NumGoldRewardForWinningRounds, Statistics.RoundWinMoneyEarned);

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

            AddStatImpl(config.InfinitePlayPrice, Statistics.MoneySpentInfinitePlay);
            AddStatImpl(1, Statistics.InfinitePlayUsed);

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

                AddStatImpl(price, Statistics.MoneySpentRevealAnswers);
                AddStatImpl(1, Statistics.RevealAnswersUsed);

                State.Gold -= price;
                State.OwnedCategoryAnswers.Add(category);
                gold = State.Gold;
            }

            return Task.FromResult((words.AsEnumerable(), gold));
        }

        public Task<(PlayerInfo info, bool[] haveCategoryAnswers)> GetPlayerInfoAndOwnedCategories(IReadOnlyList<string> categories)
        {
            var ownedCategories = new bool[categories.Count];
            for (int i = 0; i < categories.Count; ++i)
                ownedCategories[i] = State.OwnedCategoryAnswers.Contains(categories[i]);

            return Task.FromResult((GetPlayerInfoImpl(), ownedCategories));
        }

        public Task<bool> HaveAnswersForCategory(string category) => Task.FromResult(State.OwnedCategoryAnswers.Contains(category));

        public Task<IReadOnlyList<bool>> HaveAnswersForCategories(IReadOnlyList<string> categories) =>
            Task.FromResult(categories.Select(c => State.OwnedCategoryAnswers.Contains(c)).ToList() as IReadOnlyList<bool>);
    }
}
