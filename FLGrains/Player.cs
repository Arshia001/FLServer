using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using FLGrains.Utility;
using LightMessage.OrleansUtils.Grains;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    //!! The StateWrapper allows us to know exactly when we need to persist. Now, do we need a timer for periodic persistance?
    // I think not, since we're likely to give player grains low lifetimes. If we don't, we should probably put the timer in.
    class Player : Grain, IPlayer
    {
        readonly IConfigReader configReader;
        readonly IFcmNotificationService fcmNotificationService;
        readonly ISystemSettingsProvider systemSettings;
        readonly IEmailService emailService;
        readonly GrainStateWrapper<PlayerState> state;
        ISystemEndPoint? systemEndPoint;

        public Player(
            IConfigReader configReader,
            IFcmNotificationService fcmNotificationService,
            ISystemSettingsProvider systemSettings,
            IEmailService emailService,
            [PersistentState("State")] IPersistentState<PlayerState> state)
        {
            this.configReader = configReader;
            this.fcmNotificationService = fcmNotificationService;
            this.systemSettings = systemSettings;
            this.emailService = emailService;
            this.state = new GrainStateWrapper<PlayerState>(state);
        }

        public override Task OnActivateAsync()
        {
            systemEndPoint = GrainFactory.GetGrain<ISystemEndPoint>(0);
            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync() => state.PerformLazyPersistIfPending();

        public async Task<OwnPlayerInfo> PerformStartupTasksAndGetInfo()
        {
            await state.UseStateAndMaybePersist(state =>
            {
                if (state.Level == 0)
                {
                    state.Gold = 1_000_000;
                    state.Level = 1;

                    var id = this.GetPrimaryKey();
                    LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score).Set(id, 0).Ignore();
                    LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.XP).Set(id, 0).Ignore();

                    state.Name = "Guest " + (new Random().Next(100_000, 1_000_000).ToString());

                    return FLTaskExtensions.True;
                }

                return FLTaskExtensions.False;
            });

            return await GetOwnPlayerInfo();
        }

        public Task<Immutable<IReadOnlyList<IGame>>> GetGames() =>
            state.UseState(state =>
                Task.FromResult(state.ActiveGames.Concat(state.PastGames).ToList().AsImmutable<IReadOnlyList<IGame>>())
            );

        public Task ClearFinishedGames()
        {
            state.UseStateAndLazyPersist(s => s.PastGames.Clear());
            return Task.CompletedTask;
        }

        public Task<PlayerInfo> GetPlayerInfo() => Task.FromResult(GetPlayerInfoImpl());

        Task<string> IPlayer.GetName() => state.UseState(state => Task.FromResult(state.Name));

        bool IsInfinitePlayActive => state.UseState(state => state.InfinitePlayEndTime > DateTime.Now);

        TimeSpan InfinitePlayTimeRemaining => state.UseState(state => state.InfinitePlayEndTime - DateTime.Now);

        static bool ShouldReplicateStatToClient(Statistics stat) =>
            stat switch
            {
                Statistics.BestGameScore => true,
                Statistics.BestRoundScore => true,
                Statistics.GamesEndedInDraw => true,
                Statistics.GamesLost => true,
                Statistics.GamesWon => true,

                _ => false
            };

        public Task<OwnPlayerInfo> GetOwnPlayerInfo() =>
            state.UseState(async state =>
            {
                var config = configReader.Config;
                var timeTillNextReward = config.ConfigValues.RoundWinRewardInterval - (DateTime.Now - state.LastRoundWinRewardTakeTime);

                return new OwnPlayerInfo
                (
                    name: state.Name,
                    email: state.Email,
                    level: state.Level,
                    xp: state.XP,
                    nextLevelXPThreshold: GetNextLevelRequiredXP(config),
                    currentNumRoundsWonForReward: state.NumRoundsWonForReward,
                    nextRoundWinRewardTimeRemaining: new TimeSpan(Math.Max(0L, timeTillNextReward.Ticks)),
                    score: state.Score,
                    rank: (uint)await LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score).GetRank(this.GetPrimaryKey()),
                    gold: state.Gold,
                    infinitePlayTimeRemaining: IsInfinitePlayActive ? InfinitePlayTimeRemaining : default(TimeSpan?),
                    statisticsValues: state.StatisticsValues.Where(kv => ShouldReplicateStatToClient(kv.Key.Statistic))
                        .Select(kv => new StatisticValue(kv.Key.Statistic, kv.Key.Parameter, kv.Value)),
                    isRegistered: IsRegistered()
                );
            });

        bool IsRegistered() => state.UseState(state => state.Email != null && state.PasswordHash != null);

        public Task<PlayerLeaderBoardInfo> GetLeaderBoardInfo() => state.UseState(state => Task.FromResult(new PlayerLeaderBoardInfo(state.Name)));

        public Task<(uint score, uint level)> GetMatchMakingInfo() => state.UseState(state => Task.FromResult((state.Score, state.Level)));

        public Task<uint> GetScore() => state.UseState(state => Task.FromResult(state.Score));

        PlayerInfo GetPlayerInfoImpl() => state.UseState(state => new PlayerInfo(id: this.GetPrimaryKey(), name: state.Name, level: state.Level));

        LevelConfig GetLevelConfig(ReadOnlyConfigData config) =>
            state.UseState(state =>
                config.PlayerLevels.TryGetValue(state.Level, out var result) ? result : config.PlayerLevels[0]
            );

        uint GetNextLevelRequiredXP(ReadOnlyConfigData config) => state.UseState(state => GetLevelConfig(config).GetRequiredXP(state));

        ulong GiveGold(uint delta) => state.UseStateAndLazyPersist(s => s.Gold += delta);

        (uint level, uint xp) AddXP(uint delta) =>
            state.UseStateAndLazyPersist(state =>
            {
                if (delta == 0)
                    return (state.Level, state.XP);

                var reqXP = GetNextLevelRequiredXP(configReader.Config);

                state.XP += delta;
                if (state.XP >= reqXP)
                {
                    ++state.Level;
                    state.XP -= reqXP;
                }

                var id = this.GetPrimaryKey();

                LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.XP).AddDelta(id, delta).Ignore();

                return (state.Level, state.XP);
            });

        public Task<bool> SetUsername(string username) =>
            state.UseStateAndMaybePersist(async state =>
            {
                if (state.Name == username)
                    return (shouldPersist: false, result: true);

                if (await PlayerIndex.UpdateUsernameIfUnique(GrainFactory, this.AsReference<IPlayer>(), username))
                {
                    state.Name = username;
                    return (shouldPersist: true, result: true);
                }

                return (shouldPersist: false, result: false);
            });

        public Task<RegistrationResult> PerformRegistration(string username, string email, string password) =>
            state.UseStateAndMaybePersist(async state =>
            {
                if (IsRegistered())
                    return (false, RegistrationResult.AlreadyRegistered);

                if (!RegistrationInfoSpecification.IsEmailAddressValid(email))
                    return (false, RegistrationResult.InvalidEmailAddress);

                if (!RegistrationInfoSpecification.IsPasswordComplexEnough(password))
                    return (false, RegistrationResult.PasswordNotComplexEnough);

                if (!await PlayerIndex.UpdateEmailIfUnique(GrainFactory, this.AsReference<IPlayer>(), email))
                    return (false, RegistrationResult.EmailAddressInUse);

                if (!await PlayerIndex.UpdateUsernameIfUnique(GrainFactory, this.AsReference<IPlayer>(), username))
                    return (false, RegistrationResult.UsernameInUse);

                state.Name = username;
                state.Email = email;
                await UpdatePasswordImpl(password);
                return (true, RegistrationResult.Success);
            });

        private Task<bool> UpdatePasswordImpl(string password)
        {
            if (!RegistrationInfoSpecification.IsPasswordComplexEnough(password))
                return FLTaskExtensions.False;

            return state.UseStateAndPersist(state =>
            {
                state.PasswordHash = CryptographyHelper.HashPassword(state.PasswordSalt, password);
                return true;
            });
        }

        public Task<SetEmailResult> SetEmail(string email) =>
            state.UseStateAndMaybePersist(async state =>
            {
                if (!IsRegistered())
                    return (false, SetEmailResult.NotRegistered);

                if (!RegistrationInfoSpecification.IsEmailAddressValid(email))
                    return (false, SetEmailResult.InvalidEmailAddress);

                if (!await PlayerIndex.UpdateEmailIfUnique(GrainFactory, this.AsReference<IPlayer>(), email))
                    return (false, SetEmailResult.EmailAddressInUse);

                state.Email = email;
                return (true, SetEmailResult.Success);
            });

        public async Task<SetPasswordResult> UpdatePassword(string newPassword)
        {
            if (!IsRegistered())
                return SetPasswordResult.NotRegistered;

            return await UpdatePasswordImpl(newPassword) ? SetPasswordResult.Success : SetPasswordResult.PasswordNotComplexEnough;
        }

        bool ValidatePasswordImpl(string password) => state.UseState(state => CryptographyHelper.HashPassword(state.PasswordSalt, password).SequenceEqual(state.PasswordHash));

        public Task<bool> ValidatePassword(string password) => Task.FromResult(ValidatePasswordImpl(password));

        public Task SendPasswordRecoveryLink() => state.UseStateAndPersist(async state =>
        {
            if (state.Email == null)
                throw new VerbatimException("Cannot send password recovery link because email address is not set");

            await ClearPasswordRecoveryToken();

            var token = PasswordRecoveryHelper.GenerateNewToken();

            await emailService.SendPasswordRecovery(state.Email, state.Name, token);

            state.PasswordRecoveryToken = token;
            state.PasswordRecoveryTokenExpirationTime = DateTime.Now + systemSettings.Settings.Values.PasswordRecoveryTokenExpirationInterval;

            await PlayerIndex.SetPasswordRecoveryToken(GrainFactory, this.AsReference<IPlayer>(), token);
        });

        Task ClearPasswordRecoveryToken() => state.UseStateAndLazyPersist(async state =>
        {
            if (state.PasswordRecoveryToken != null)
                await PlayerIndex.RemovePasswordRecoveryToken(GrainFactory, state.PasswordRecoveryToken);

            state.PasswordRecoveryToken = null;
            state.PasswordRecoveryTokenExpirationTime = null;
        });

        bool IsPasswordRecoveryTokenValid(string token, PlayerState state) =>
            state.PasswordRecoveryToken == token && state.PasswordRecoveryTokenExpirationTime.HasValue && state.PasswordRecoveryTokenExpirationTime.Value >= DateTime.Now;

        public Task<bool> ValidatePasswordRecoveryToken(string token) => state.UseState(async state =>
        {
            var result = IsPasswordRecoveryTokenValid(token, state);
            if (!result)
                await ClearPasswordRecoveryToken();
            return result;
        });

        public Task<UpdatePasswordViaRecoveryTokenResult> UpdatePasswordViaRecoveryToken(string token, string newPassword) => state.UseState(async state =>
        {
            if (!IsPasswordRecoveryTokenValid(token, state))
            {
                await ClearPasswordRecoveryToken();
                return UpdatePasswordViaRecoveryTokenResult.InvalidOrExpiredToken;
            }

            var result = await UpdatePasswordImpl(newPassword);

            if (result)
            {
                await ClearPasswordRecoveryToken();
                return UpdatePasswordViaRecoveryTokenResult.Success;
            }
            else
                return UpdatePasswordViaRecoveryTokenResult.PasswordNotComplexEnough;
        });

        ulong GetStat(PlayerState state, Statistics stat, int parameter = 0) =>
            state.StatisticsValues.TryGetValue(new StatisticWithParameter(stat, parameter), out var value) ? value : 0UL;

        void UpdateStatImpl(Statistics stat, int parameter, ulong value)
            => state.UseStateAndLazyPersist(state =>
            {
                if (GetStat(state, stat) != value)
                {
                    state.StatisticsValues[new StatisticWithParameter(stat, parameter)] = value;
                    if (ShouldReplicateStatToClient(stat))
                        systemEndPoint?.SendStatisticUpdated(this.GetPrimaryKey(), new StatisticValue(stat, parameter, value))?.Ignore();
                }
            });

        void SetStat(ulong value, Statistics stat, int parameter = 0) => UpdateStatImpl(stat, parameter, value);

        void SetMaxStat(ulong value, Statistics stat, int parameter = 0) => UpdateStatImpl(stat, parameter, Math.Max(state.UseState(state => GetStat(state, stat)), value));

        void AddStatImpl(ulong value, Statistics stat, int parameter = 0) => UpdateStatImpl(stat, parameter, state.UseState(state => GetStat(state, stat)) + value);

        public Task AddStats(List<StatisticValue> values)
        {
            foreach (var val in values)
                AddStatImpl(val.Value, val.Statistic, val.Parameter);
            return Task.CompletedTask;
        }

        public Task<bool> CanEnterGame() => state.UseState(state => Task.FromResult(IsInfinitePlayActive || state.ActiveGames.Count < configReader.Config.ConfigValues.MaxActiveGames));

        public Task<byte> JoinGameAsFirstPlayer(IGame game) =>
            state.UseStateAndPersist(async state =>
            {
                var result = await game.StartNew(this.GetPrimaryKey());
                state.ActiveGames.Add(game);
                return result;
            });

        public Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game) =>
            state.UseStateAndPersist(async state =>
            {
                var result = await game.AddSecondPlayer(GetPlayerInfoImpl());
                state.ActiveGames.Add(game);
                return result;
            });

        public Task OnRoundCompleted(IGame game, uint myScore)
        {
            SetMaxStat(myScore, Statistics.BestRoundScore);
            return Task.CompletedTask;
        }

        public Task OnRoundResult(IGame game, CompetitionResult result, ushort groupID) =>
            state.UseStateAndLazyPersist(state =>
            {
                if (!state.ActiveGames.Contains(game))
                    return Task.CompletedTask;

                switch (result)
                {
                    case CompetitionResult.Win:
                        AddStatImpl(1, Statistics.RoundsWon);
                        AddStatImpl(1, Statistics.GroupWon_Param, groupID);
                        ++state.NumRoundsWonForReward;
                        return systemEndPoint?.SendNumRoundsWonForRewardUpdated(this.GetPrimaryKey(), state.NumRoundsWonForReward) ?? Task.CompletedTask;

                    case CompetitionResult.Loss:
                        AddStatImpl(1, Statistics.RoundsLost);
                        AddStatImpl(1, Statistics.GroupLost_Param, groupID);
                        return Task.CompletedTask;

                    case CompetitionResult.Draw:
                        AddStatImpl(1, Statistics.RoundsEndedInDraw);
                        AddStatImpl(1, Statistics.GroupEndedInDraw_Param, groupID);
                        return Task.CompletedTask;

                    default:
                        return Task.CompletedTask;
                }
            });

        Task<ulong> AddScore(int delta) =>
            state.UseStateAndLazyPersist(state =>
            {
                var lb = LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score);

                if (delta == 0)
                    return lb.GetRank(this.GetPrimaryKey());

                state.Score = (uint)Math.Max(0, state.Score + delta);
                return lb.SetAndGetRank(this.GetPrimaryKey(), state.Score);
            });

        Task<ulong> GetRank() => LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score).GetRank(this.GetPrimaryKey());

        public Task<(uint score, uint rank, uint level, uint xp, ulong gold)> OnGameResult(IGame game, CompetitionResult result, uint myScore, uint scoreGain) =>
            state.UseStateAndPersist(async state =>
            {
                SetMaxStat(myScore, Statistics.BestGameScore);

                state.ActiveGames.Remove(game);
                state.PastGames.Add(game);

                var rank = 0UL;
                var gold = 0UL;
                var xp = 0U;
                var level = 0U;

                var config = configReader.Config.ConfigValues;

                switch (result)
                {
                    case CompetitionResult.Draw:
                        rank = await GetRank();
                        (level, xp) = AddXP(config.DrawXPGain);
                        gold = GiveGold(config.DrawGoldGain);

                        AddStatImpl(1, Statistics.GamesEndedInDraw);
                        AddStatImpl(config.DrawGoldGain, Statistics.RewardMoneyEarned);

                        break;

                    case CompetitionResult.Win:
                        rank = await AddScore((int)scoreGain);
                        (level, xp) = AddXP(config.WinnerXPGain);
                        gold = GiveGold(config.WinnerGoldGain);

                        AddStatImpl(1, Statistics.GamesWon);
                        AddStatImpl(config.WinnerGoldGain, Statistics.RewardMoneyEarned);

                        break;

                    case CompetitionResult.Loss:
                        rank = await AddScore(-(int)(scoreGain * config.LoserScoreLossRatio));
                        (level, xp) = AddXP(config.LoserXPGain);
                        gold = GiveGold(config.LoserGoldGain);

                        AddStatImpl(1, Statistics.GamesLost);
                        AddStatImpl(config.LoserGoldGain, Statistics.RewardMoneyEarned);

                        break;
                }

                return (state.Score, (uint)rank, level, xp, gold);
            });

        public Task<(ulong? gold, TimeSpan? remainingTime)> IncreaseRoundTime(Guid gameID) =>
            state.UseStateAndLazyPersist<Task<(ulong? gold, TimeSpan? remainingTime)>>(async state =>
            {
                var price = configReader.Config.ConfigValues.RoundTimeExtensionPrice;
                if (state.Gold < price)
                    throw new VerbatimException("Insufficient gold");

                var time = await GrainFactory.GetGrain<IGame>(gameID).IncreaseRoundTime(this.GetPrimaryKey());
                if (time == null)
                    return (null, null);

                AddStatImpl(price, Statistics.MoneySpentTimePowerup);
                AddStatImpl(1, Statistics.TimePowerupUsed);

                state.Gold -= price;
                return (state.Gold, time.Value);
            });

        public Task<(ulong? gold, string? word, byte? wordScore)> RevealWord(Guid gameID) =>
            state.UseStateAndLazyPersist<Task<(ulong? gold, string? word, byte? wordScore)>>(async state =>
            {
                var price = configReader.Config.ConfigValues.RevealWordPrice;
                if (state.Gold < price)
                    throw new VerbatimException("Insufficient gold");

                var result = await GrainFactory.GetGrain<IGame>(gameID).RevealWord(this.GetPrimaryKey());
                if (result == null)
                    return (null, null, null);

                AddStatImpl(price, Statistics.MoneySpentHelpPowerup);
                AddStatImpl(1, Statistics.HelpPowerupUsed);

                state.Gold -= price;
                return (state.Gold, result.Value.word, result.Value.wordScore);
            });

        public Task<IEnumerable<GroupInfoDTO>?> RefreshGroups(Guid gameID) =>
            state.UseStateAndLazyPersist(async state =>
            {
                var price = configReader.Config.ConfigValues.PriceToRefreshGroups;
                if (state.Gold < price)
                    throw new VerbatimException("Insufficient gold");

                var result = await GrainFactory.GetGrain<IGame>(gameID).RefreshGroups(this.GetPrimaryKey());
                if (result == null)
                    return null;

                AddStatImpl(price, Statistics.MoneySpentGroupChange);
                AddStatImpl(1, Statistics.GroupChangeUsed);

                state.Gold -= price;
                return result.Select(g => (GroupInfoDTO)g).ToList().AsEnumerable();
            });

        public Task<(ulong totalGold, TimeSpan timeUntilNextReward)> TakeRewardForWinningRounds() =>
            state.UseStateAndLazyPersist(state =>
            {
                var configValues = configReader.Config.ConfigValues;

                if (DateTime.Now - state.LastRoundWinRewardTakeTime < configValues.RoundWinRewardInterval)
                    throw new VerbatimException("Interval not elapsed yet");

                if (state.NumRoundsWonForReward < configReader.Config.ConfigValues.NumRoundsToWinToGetReward)
                    throw new VerbatimException("Insufficient rounds won");

                state.NumRoundsWonForReward = 0;
                state.LastRoundWinRewardTakeTime = DateTime.Now;

                AddStatImpl(configValues.NumGoldRewardForWinningRounds, Statistics.RoundWinMoneyEarned);

                state.Gold += configValues.NumGoldRewardForWinningRounds;

                return Task.FromResult((state.Gold, configValues.RoundWinRewardInterval));
            });

        public Task<(bool success, ulong totalGold, TimeSpan duration)> ActivateInfinitePlay() =>
            state.UseStateAndLazyPersist(state =>
            {
                if (IsInfinitePlayActive)
                    return Task.FromResult((false, state.Gold, state.InfinitePlayEndTime - DateTime.Now));

                var config = configReader.Config.ConfigValues;
                if (state.Gold < config.InfinitePlayPrice)
                    return Task.FromResult((false, 0UL, TimeSpan.Zero));

                state.Gold -= config.InfinitePlayPrice;

                AddStatImpl(config.InfinitePlayPrice, Statistics.MoneySpentInfinitePlay);
                AddStatImpl(1, Statistics.InfinitePlayUsed);

                var duration = config.InfinitePlayTime;
                state.InfinitePlayEndTime = DateTime.Now + duration;
                return Task.FromResult((true, state.Gold, duration));
            });

        public Task<(IEnumerable<string> words, ulong? totalGold)> GetAnswers(string category) =>
            state.UseStateAndLazyPersist(state =>
            {
                var config = configReader.Config;

                var words = config.CategoriesAsGameLogicFormatByName[category].Answers;
                var gold = default(ulong?);

                if (!state.OwnedCategoryAnswers.Contains(category))
                {
                    var price = config.ConfigValues.GetAnswersPrice;
                    if (state.Gold < price)
                        throw new VerbatimException("Insufficient gold");

                    AddStatImpl(price, Statistics.MoneySpentRevealAnswers);
                    AddStatImpl(1, Statistics.RevealAnswersUsed);

                    state.Gold -= price;
                    state.OwnedCategoryAnswers.Add(category);
                    gold = state.Gold;
                }

                return Task.FromResult((words.AsEnumerable(), gold));
            });

        public Task<(PlayerInfo info, bool[] haveCategoryAnswers)> GetPlayerInfoAndOwnedCategories(IReadOnlyList<string> categories) =>
            state.UseState(state =>
            {
                var ownedCategories = new bool[categories.Count];
                for (int i = 0; i < categories.Count; ++i)
                    ownedCategories[i] = state.OwnedCategoryAnswers.Contains(categories[i]);

                return Task.FromResult((GetPlayerInfoImpl(), ownedCategories));
            });

        public Task<bool> HaveAnswersForCategory(string category) => state.UseState(state => Task.FromResult(state.OwnedCategoryAnswers.Contains(category)));

        public Task<IReadOnlyList<bool>> HaveAnswersForCategories(IReadOnlyList<string> categories) =>
            state.UseState(state => Task.FromResult(categories.Select(c => state.OwnedCategoryAnswers.Contains(c)).ToList() as IReadOnlyList<bool>));

        public Task<(IabPurchaseResult result, ulong totalGold)> ProcessGoldPackPurchase(string sku, string purchaseToken) =>
            state.UseStateAndMaybePersist(async state =>
            {
                if (state.ProcessedIabTokens.Contains(purchaseToken))
                    return (false, (IabPurchaseResult.AlreadyProcessed, state.Gold));

                var config = configReader.Config;
                if (!config.GoldPacks.TryGetValue(sku, out var packConfig))
                    throw new VerbatimException("Unknown SKU");

                var verifyResult = await GrainFactory.GetGrain<IBazaarIabVerifier>(0).VerifyBazaarPurchase(sku,purchaseToken);

                switch (verifyResult)
                {
                    case IabPurchaseResult.Success:
                        state.Gold += packConfig.NumGold;
                        state.ProcessedIabTokens.Add(purchaseToken);
                        return (true, (IabPurchaseResult.Success, state.Gold));

                    case IabPurchaseResult.Invalid:
                    case IabPurchaseResult.FailedToContactValidationService:
                        return (false, (verifyResult, 0ul));

                    default:
                        return (false, (IabPurchaseResult.UnknownError, 0ul));
                }
            });

        public Task SetFcmToken(string token) => state.UseStateAndPersist(state => { state.FcmToken = token; });

        public Task SetNotificationsEnabled(bool enable) => state.UseStateAndPersist(state => { state.NotificationsEnabled = enable; });

        bool CanSendNotification(PlayerState state) => state.NotificationsEnabled && !string.IsNullOrEmpty(state.FcmToken);

        public Task SendMyTurnStartedNotification(Guid opponentID)
        {
            return state.UseState(async state =>
            {
                if (CanSendNotification(state) && state.FcmToken != null)
                {
                    var opName = await PlayerInfoHelper.GetName(GrainFactory, opponentID);
                    fcmNotificationService.SendMyTurnStarted(state.FcmToken, opName);
                }
            });
        }

        public Task SendGameEndedNotification(Guid opponentID)
        {
            return state.UseState(async state =>
            {
                if (CanSendNotification(state) && state.FcmToken != null)
                {
                    var opName = await PlayerInfoHelper.GetName(GrainFactory, opponentID);
                    fcmNotificationService.SendGameEnded(state.FcmToken, opName);
                }
            });
        }
    }
}
