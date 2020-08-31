using Cassandra;
using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrainInterfaces.Utility;
using FLGrains.ServiceInterfaces;
using FLGrains.Utility;
using Google.Apis.Logging;
using LightMessage.Common.Connection;
using LightMessage.OrleansUtils.Grains;
using Microsoft.Extensions.Logging;
using MimeKit.Cryptography;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    //!! Reminders for notifications could be separated out into another grain
    static class PlayerReminderNames
    {
        public const string Day4Notification = "Day4";
        public const string RoundWinRewardNotification = "RoundWinReward";
        public const string CoinRewardVideoNotification = "GiftVideo";

        public static IEnumerable<string> All
        {
            get
            {
                yield return Day4Notification;
                yield return RoundWinRewardNotification;
                yield return CoinRewardVideoNotification;
            }
        }
    }

    //!! The StateWrapper allows us to know exactly when we need to persist. Now, do we need a timer for periodic persistance?
    // I think not, since we're likely to give player grains low lifetimes. If we don't, we should probably put the timer in.
    class Player : Grain, IPlayer, IRemindable
    {
        readonly IConfigReader configReader;
        readonly IFcmNotificationService fcmNotificationService;
        readonly ISystemSettingsProvider systemSettings;
        readonly IEmailService emailService;
        private readonly ILogger<Player> logger;
        readonly GrainStateWrapper<PlayerState> state;

        readonly VideoAdLimitTracker coinRewardAdTracker;
        readonly VideoAdLimitTracker getCategoryAnswersAdTracker;

        AvatarManager? avatarManager;

        HashSet<Guid> activeOpponents = new HashSet<Guid>();

        ISystemEndPoint? systemEndPoint;

        bool playerLoggedInDuringThisActivation = false;

        public Player(
            IConfigReader configReader,
            IFcmNotificationService fcmNotificationService,
            ISystemSettingsProvider systemSettings,
            IEmailService emailService,
            ILogger<Player> logger,
            [PersistentState("State")] IPersistentState<PlayerState> state)
        {
            this.configReader = configReader;
            this.fcmNotificationService = fcmNotificationService;
            this.systemSettings = systemSettings;
            this.emailService = emailService;
            this.logger = logger;

            this.state = new GrainStateWrapper<PlayerState>(state);
            this.state.Persist += State_Persist;

            coinRewardAdTracker = new VideoAdLimitTracker(() => configReader.Config.ConfigValues.CoinRewardVideo);
            getCategoryAnswersAdTracker = new VideoAdLimitTracker(() => configReader.Config.ConfigValues.GetCategoryAnswersVideo);
        }

        private void State_Persist(object sender, PersistStateEventArgs<PlayerState> e)
        {
            e.State.CoinRewardVideoTrackerState = coinRewardAdTracker.Serialize();
            e.State.GetCategoryAnswersVideoTrackerState = getCategoryAnswersAdTracker.Serialize();

            e.State.AvatarManagerState = avatarManager!.Serialize();
        }

        public override Task OnActivateAsync()
        {
            systemEndPoint = GrainFactory.GetGrain<ISystemEndPoint>(0);

            state.UseState(state =>
            {
                if (state.CoinRewardVideoTrackerState != null)
                    coinRewardAdTracker.Deserialize(state.CoinRewardVideoTrackerState);
                if (state.GetCategoryAnswersVideoTrackerState != null)
                    getCategoryAnswersAdTracker.Deserialize(state.GetCategoryAnswersVideoTrackerState);

                if (state.AvatarManagerState != null)
                    avatarManager = AvatarManager.Deserialize(state.AvatarManagerState);
                else
                    avatarManager = AvatarManager.InitializeNew();
            });

            return base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            try
            {
                await state.PerformLazyPersistIfPending();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to persist state during {nameof(OnDeactivateAsync)}");
            }
        }

        public async Task<(OwnPlayerInfoDTO info, VideoAdLimitTrackerInfo coinRewardVideo, VideoAdLimitTrackerInfo getCategoryAnswersVideo,
            IEnumerable<CoinGiftInfo> coinGifts)> PerformStartupTasksAndGetInfo()
        {
            var gifts = await state.UseStateAndMaybePersist(async state =>
            {
                var config = configReader.Config;
                var configValues = config.ConfigValues;
                var myID = this.GetPrimaryKey();

                await InitializeInviteCodeIfNeeded(state);

                if (IsNewPlayer(state))
                {
                    state.Gold = configValues.InitialGold;
                    state.Level = 1;

                    LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score).Set(myID, 0).Ignore();
                    LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.XP).Set(myID, 0).Ignore();

                    state.Name = "مهمان " + RandomHelper.GetInt32(100_000, 1_000_000).ToString();

                    InitializeAvatar(config);

                    return (true, state.CoinGifts);
                }

                InitializeAvatarIfNeeded(config);

                foreach (var game in state.ActiveGames)
                {
                    var opponentID =
                        (await GrainFactory.GetGrain<IGame>(game).GetPlayerIDs())
                        .Where(id => id != myID)
                        .FirstOrDefault();
                    if (opponentID != Guid.Empty)
                        activeOpponents.Add(opponentID);
                }

                if (state.PastGames.Count > configValues.MaxGameHistoryEntries)
                {
                    while (state.PastGames.Count > configValues.MaxGameHistoryEntries)
                        state.PastGames.RemoveAt(0);

                    return (true, state.CoinGifts);
                }

                return (false, state.CoinGifts);
            });

            playerLoggedInDuringThisActivation = true;

            await UnregisterOfflineReminders();

            return (await GetOwnPlayerInfo(), coinRewardAdTracker.GetInfo(), getCategoryAnswersAdTracker.GetInfo(), gifts);
        }

        async Task InitializeInviteCodeIfNeeded(PlayerState state)
        {
            if (string.IsNullOrEmpty(state.InviteCode))
            {
                while (true)
                {
                    var code = InviteCodeHelper.GenerateNewCode();
                    if (await PlayerIndex.SetInviteCode(GrainFactory, this.AsReference<IPlayer>(), code))
                    {
                        state.InviteCode = code;
                        return;
                    }
                }
            }
        }

        public async Task PlayerDisconnected()
        {
            // Reminders can't be registered during silo shutdown.
            // Also, player grains may be deactivated while the player is connected, due to inactivity.
            // Lastly, if a player is connected and we take the server down, in a hurry he'll likely connect
            // to another server and the reminders can be registered there.
            // Due to above reasons, this is the best compromise short of registering reminders when the 
            // player connects, which has its own set of problems (e.g. acting on invalid data that may change
            // until the player disconnects).
            await RegisterOfflineReminders();

            await state.PerformLazyPersistIfPending();
        }

        bool InitializeAvatarIfNeeded(ReadOnlyConfigData config)
        {
            if (avatarManager!.GetActiveParts().Count == 0)
            {
                InitializeAvatar(config);
                return true;
            }

            return false;
        }

        void InitializeAvatar(ReadOnlyConfigData config)
        {
            if (config.InitialAvatar.HeadShape != null)
                avatarManager!.ForceActivatePart(new AvatarPart(AvatarPartType.HeadShape, config.InitialAvatar.HeadShape.Value));
            if (config.InitialAvatar.Hair != null)
                avatarManager!.ForceActivatePart(new AvatarPart(AvatarPartType.Hair, config.InitialAvatar.Hair.Value));
            if (config.InitialAvatar.Eyes != null)
                avatarManager!.ForceActivatePart(new AvatarPart(AvatarPartType.Eyes, config.InitialAvatar.Eyes.Value));
            if (config.InitialAvatar.Mouth != null)
                avatarManager!.ForceActivatePart(new AvatarPart(AvatarPartType.Mouth, config.InitialAvatar.Mouth.Value));
            if (config.InitialAvatar.Glasses != null)
                avatarManager!.ForceActivatePart(new AvatarPart(AvatarPartType.Glasses, config.InitialAvatar.Glasses.Value));
        }

        static bool IsNewPlayer(PlayerState state) => state.Level == 0;

        public Task<Immutable<IReadOnlyList<IGame>>> GetGames() =>
            state.UseState(state =>
                Task.FromResult(
                    state.ActiveGames.Concat(state.PastGames)
                    .Select(id => GrainFactory.GetGrain<IGame>(id))
                    .ToList()
                    .AsImmutable<IReadOnlyList<IGame>>()
                ));

        public Task ClearGameHistory() => state.UseStateAndPersist(state => state.PastGames.Clear());

        public Task ClearFinishedGames()
        {
            state.UseStateAndLazyPersist(s => s.PastGames.Clear());
            return Task.CompletedTask;
        }

        public Task<PlayerInfoDTO> GetPlayerInfo() =>
            state.UseState(async state =>
                new PlayerInfoDTO(id: this.GetPrimaryKey(), name: state.Name, level: state.Level, avatar: await GetAvatar())
            );

        Task<string> IPlayer.GetName() => state.UseState(state => Task.FromResult(state.Name));

        bool IsUpgradedActiveGameLimitActive => state.UseState(state => state.UpgradedActiveGameLimitEndTime > DateTime.Now);

        TimeSpan UpgradedActiveGameLimitTimeRemaining => state.UseState(state => state.UpgradedActiveGameLimitEndTime - DateTime.Now);

        static bool ShouldReplicateStatToClient(Statistics stat) =>
            stat switch
            {
                Statistics.BestGameScore => true,
                Statistics.BestRoundScore => true,
                Statistics.GamesEndedInDraw => true,
                Statistics.GamesLost => true,
                Statistics.GamesWon => true,
                Statistics.RoundsCompleted => true,

                _ => false
            };

        Task<AvatarDTO> GetAvatar() =>
            state.UseStateAndMaybePersist(state =>
            {
                var needToSave = InitializeAvatarIfNeeded(configReader.Config);
                return (needToSave, avatarManager!.GetAvatar());
            });

        public Task<OwnPlayerInfoDTO> GetOwnPlayerInfo() =>
            state.UseState(async state =>
            {
                var config = configReader.Config;
                var rewardStatus = GetRoundWinRewardStatus();

                return new OwnPlayerInfoDTO
                (
                    name: state.Name,
                    email: state.Email,
                    level: state.Level,
                    xp: state.XP,
                    nextLevelXPThreshold: GetNextLevelRequiredXP(config),
                    currentNumRoundsWonForReward: rewardStatus.numRoundsWon,
                    nextRoundWinRewardTimeRemaining: rewardStatus.coolDownTimeRemaining,
                    score: state.Score,
                    rank: (uint)await LeaderBoardUtil.GetLeaderBoard(GrainFactory, LeaderBoardSubject.Score).GetRank(this.GetPrimaryKey()),
                    gold: state.Gold,
                    upgradedActiveGameLimitTimeRemaining: IsUpgradedActiveGameLimitActive ? UpgradedActiveGameLimitTimeRemaining : default(TimeSpan?),
                    statisticsValues: state.StatisticsValues.Where(kv => ShouldReplicateStatToClient(kv.Key.Statistic))
                        .Select(kv => new StatisticValueDTO(kv.Key.Statistic, kv.Key.Parameter, kv.Value)),
                    isRegistered: IsRegistered(),
                    notificationsEnabled: state.NotificationsEnabled,
                    coinRewardVideoNotificationsEnabled: state.CoinRewardVideoNotificationsEnabled,
                    tutorialProgress: state.TutorialProgress,
                    avatar: await GetAvatar(),
                    ownedAvatarParts: avatarManager!.GetOwnedPartsAsDTO(),
                    inviteCode: state.InviteCode ?? throw new InvalidOperationException($"Invite code not set before calling {nameof(Player)}.{nameof(GetOwnPlayerInfo)}")
                );
            });

        bool IsRegistered() => state.UseState(state => state.Email != null && state.PasswordHash != null);

        public Task<PlayerLeaderBoardInfoDTO> GetLeaderBoardInfo() =>
            state.UseState(async state =>
                new PlayerLeaderBoardInfoDTO(state.Name, await GetAvatar())
            );

        uint GetTotalGames(PlayerState state) => (uint)(
            GetStat(state, Statistics.GamesWon) +
            GetStat(state, Statistics.GamesLost) +
            GetStat(state, Statistics.GamesEndedInDraw) +
            (uint)state.ActiveGames.Count);

        public Task<(uint score, uint level, bool shouldJoinTutorialMatch)> GetMatchMakingInfo() =>
            state.UseState(state => 
                Task.FromResult((state.Score, state.Level, GetTotalGames(state) < configReader.Config.ConfigValues.TutorialGamesCount))
            );

        public Task<uint> GetScore() => state.UseState(state => Task.FromResult(state.Score));

        LevelConfig GetLevelConfig(ReadOnlyConfigData config) =>
            state.UseState(state =>
                config.PlayerLevels.TryGetValue(state.Level, out var result) ? result : config.PlayerLevels[0]
            );

        uint GetNextLevelRequiredXP(ReadOnlyConfigData config) => state.UseState(state => GetLevelConfig(config).GetRequiredXP(state));

        ulong GiveGold(uint delta) => state.UseStateAndLazyPersist(s => s.Gold += delta);

        ulong TakeGold(uint delta) => state.UseStateAndLazyPersist(s =>
        {
            if (s.Gold >= delta)
                s.Gold -= delta;
            else
                s.Gold = 0;
            return s.Gold;
        });

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

        public Task<(RegistrationResult result, ulong totalGold)> PerformRegistration(string username, string email, string password, string? inviteCode) =>
            state.UseStateAndMaybePersist(async state =>
            {
                if (IsRegistered())
                    return (false, (RegistrationResult.AlreadyRegistered, 0ul));

                if (!RegistrationInfoSpecification.IsEmailAddressValid(email))
                    return (false, (RegistrationResult.InvalidEmailAddress, 0ul));

                if (!RegistrationInfoSpecification.IsPasswordComplexEnough(password))
                    return (false, (RegistrationResult.PasswordNotComplexEnough, 0ul));

                if (!await PlayerIndex.UpdateEmailIfUnique(GrainFactory, this.AsReference<IPlayer>(), email))
                    return (false, (RegistrationResult.EmailAddressInUse, 0ul));

                if (!await PlayerIndex.UpdateUsernameIfUnique(GrainFactory, this.AsReference<IPlayer>(), username))
                    return (false, (RegistrationResult.UsernameInUse, 0ul));

                var inviter = default(IPlayer);
                if (inviteCode != null)
                {
                    inviter = await PlayerIndex.GetByInviteCode(GrainFactory, inviteCode);
                    if (inviter == null || inviter.GetPrimaryKey() == this.GetPrimaryKey())
                        return (false, (RegistrationResult.InvalidInviteCode, 0ul));
                }

                state.Name = username;
                state.Email = email;
                await UpdatePasswordImpl(password);

                if (inviter != null)
                {
                    var config = configReader.Config;

                    await inviter.ReceiveCoinGift(new CoinGiftInfo(CoinGiftSubject.FriendInvited, config.ConfigValues.InviterReward, null, null, username, null, null, null));

                    state.Inviter = inviter;
                    state.Gold += config.ConfigValues.InviteeReward;
                }

                return (true, (RegistrationResult.Success, inviter == null ? 0ul : state.Gold));
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

        public Task<(bool success, ulong totalGold)> BuyAvatarParts(IReadOnlyList<AvatarPartDTO> parts) =>
            state.UseStateAndMaybePersist(state =>
            {
                var config = configReader.Config;
                var partConfigs =
                    parts
                    .Where(p => !avatarManager!.HasPart(p))
                    .Select(p => GetPartConfig(p, config))
                    .ToList();

                if (partConfigs.Count == 0)
                    return (false, (false, 0ul));

                if (partConfigs.Any(p => p.Price == 0))
                    throw new VerbatimException("Cannot buy free parts");

                if (partConfigs.Any(p => p.MinimumLevel > state.Level))
                    throw new VerbatimException("Cannot buy part due to unmet level requirements");

                var price = (ulong)partConfigs.Sum(p => p.Price);

                if (state.Gold < price)
                    throw new VerbatimException("Not enough gold");

                foreach (var part in parts)
                    // This shouldn't fail, since we check for pre-owned parts above
                    avatarManager!.AddOwnedPart(part);

                state.Gold -= price;
                return (true, (true, state.Gold));
            });

        private static AvatarPartConfig GetPartConfig(AvatarPartDTO part, ReadOnlyConfigData config)
        {
            if (!config.AvatarParts.TryGetValue(part.PartType, out var dic) ||
                !dic.TryGetValue(part.ID, out var partConfig))
                throw new VerbatimException("No such part found");

            return partConfig;
        }

        public Task ActivateAvatar(AvatarDTO avatar) => state.UseStateAndPersist(_ =>
        {
            var config = configReader.Config;

            var parts = avatar.Parts.Select(p => ((AvatarPart)p, GetPartConfig(p, config))).ToList();

            var previous = avatarManager!.GetActiveParts();

            var failed = false;
            foreach (var (p, conf) in parts)
            {
                if (avatarManager!.ActivatePart(p))
                    continue;

                if (conf.Price == 0)
                {
                    avatarManager!.ForceActivatePart(p);
                    continue;
                }

                failed = true;
                break;
            }

            if (failed)
            {
                foreach (var p in previous)
                    avatarManager!.ForceActivatePart(new AvatarPart(p.Key, p.Value));

                // When we throw, the persistance routine will be skipped
                throw new VerbatimException("A part is not free and is not owned by player");
            }
        });

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
                        systemEndPoint?.SendStatisticUpdated(this.GetPrimaryKey(), new StatisticValueDTO(stat, parameter, value))?.Ignore();
                }
            });

        void SetStat(ulong value, Statistics stat, int parameter = 0) => UpdateStatImpl(stat, parameter, value);

        void SetMaxStat(ulong value, Statistics stat, int parameter = 0) => UpdateStatImpl(stat, parameter, Math.Max(state.UseState(state => GetStat(state, stat)), value));

        void AddStatImpl(ulong value, Statistics stat, int parameter = 0) => UpdateStatImpl(stat, parameter, state.UseState(state => GetStat(state, stat)) + value);

        public Task AddStats(List<StatisticValueDTO> values)
        {
            foreach (var val in values)
                AddStatImpl(val.Value, val.Statistic, val.Parameter);
            return Task.CompletedTask;
        }

        public Task<(bool canEnter, Immutable<ISet<Guid>> activeOpponents)> CheckCanEnterGameAndGetActiveOpponents() =>
            state.UseState(state =>
            {
                var canEnter = state.ActiveGames.Count < (
                    IsUpgradedActiveGameLimitActive ?
                    configReader.Config.ConfigValues.MaxActiveGamesWhenUpgraded :
                    configReader.Config.ConfigValues.MaxActiveGames
                );
                return Task.FromResult((canEnter, activeOpponents.AsImmutable<ISet<Guid>>()));
            });

        public Task<byte> JoinGameAsFirstPlayer(IGame game) =>
            state.UseStateAndPersist(async state =>
            {
                var result = await game.StartNew(this.GetPrimaryKey());
                state.ActiveGames.Add(game.GetPrimaryKey());
                return result;
            });

        public Task<(Guid opponentID, byte numRounds, TimeSpan? expiryTimeRemaining)> JoinGameAsSecondPlayer(IGame game) =>
            state.UseStateAndPersist(async state =>
            {
                var result = await game.AddSecondPlayer(await GetPlayerInfo());
                if (result.opponentID != Guid.Empty)
                {
                    activeOpponents.Add(result.opponentID);
                    state.ActiveGames.Add(game.GetPrimaryKey());
                }
                return result;
            });

        public Task SecondPlayerJoinedGame(IGame game, Guid playerID) =>
            state.UseState(state =>
            {
                if (state.ActiveGames.Contains(game.GetPrimaryKey()))
                    activeOpponents.Add(playerID);

                return Task.CompletedTask;
            });

        public Task OnRoundCompleted(IGame game, uint myScore)
        {
            AddStatImpl(1, Statistics.RoundsCompleted);
            SetMaxStat(myScore, Statistics.BestRoundScore);
            return Task.CompletedTask;
        }

        public Task OnRoundResult(IGame game, CompetitionResult result, ushort groupID) =>
            state.UseStateAndLazyPersist(state =>
            {
                if (!state.ActiveGames.Contains(game.GetPrimaryKey()))
                    return Task.CompletedTask;

                switch (result)
                {
                    case CompetitionResult.Win:
                        AddStatImpl(1, Statistics.RoundsWon);
                        AddStatImpl(1, Statistics.GroupWon_Param, groupID);
                        if (!GetRoundWinRewardStatus().inCoolDown)
                        {
                            ++state.NumRoundsWonForReward;
                            return systemEndPoint?.SendNumRoundsWonForRewardUpdated(this.GetPrimaryKey(), state.NumRoundsWonForReward) ?? Task.CompletedTask;
                        }
                        return Task.CompletedTask;

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

        public Task<(uint score, uint rank, uint level, uint xp, ulong gold)>
            OnGameResult(IGame game, CompetitionResult result, uint myScore, uint scoreGain, bool gameExpired, Guid opponentID) =>
            state.UseStateAndPersist(async state =>
            {
                var gameID = game.GetPrimaryKey();

                if (!state.ActiveGames.Contains(gameID))
                    return (0u, 0u, 0u, 0u, 0ul);

                activeOpponents.Remove(opponentID);

                var config = configReader.Config.ConfigValues;

                SetMaxStat(myScore, Statistics.BestGameScore);

                state.ActiveGames.Remove(gameID);
                state.PastGames.Add(gameID);

                state.MatchResultHistory.Insert(0, result);
                while (state.MatchResultHistory.Count > config.MaxMatchResultHistoryEntries)
                    state.MatchResultHistory.RemoveAt(state.MatchResultHistory.Count - 1);

                var rank = 0UL;
                var gold = 0UL;
                var xp = 0U;
                var level = 0U;

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

                    case CompetitionResult.Loss when !gameExpired:
                        rank = await AddScore(-(int)(scoreGain * config.LoserScoreLossRatio));
                        (level, xp) = AddXP(config.LoserXPGain);
                        gold = GiveGold(config.LoserGoldGain);

                        AddStatImpl(1, Statistics.GamesLost);
                        AddStatImpl(config.LoserGoldGain, Statistics.RewardMoneyEarned);

                        break;

                    case CompetitionResult.Loss when gameExpired:
                        rank = await AddScore(-(int)config.GameExpiryScorePenalty!.Value);
                        (level, xp) = AddXP(0);
                        gold = TakeGold(config.GameExpiryGoldPenalty!.Value);

                        AddStatImpl(1, Statistics.GamesLost);
                        AddStatImpl(1, Statistics.GameLostDueToExpiry);

                        break;
                }

                return (state.Score, (uint)rank, level, xp, gold);
            });

        public Task<IEnumerable<CompetitionResult>> GetMatchResultHistory() =>
            Task.FromResult<IEnumerable<CompetitionResult>>(state.UseState(state => state.MatchResultHistory));

        public Task<ulong?> IncreaseRoundTime(Guid gameID, uint price) =>
            state.UseStateAndLazyPersist(state =>
            {
                if (state.Gold < price || !state.ActiveGames.Contains(gameID))
                    return Task.FromResult(default(ulong?));

                AddStatImpl(price, Statistics.MoneySpentTimePowerup);
                AddStatImpl(1, Statistics.TimePowerupUsed);

                state.Gold -= price;
                return Task.FromResult((ulong?)state.Gold);
            });

        public Task<ulong?> RevealWord(Guid gameID, uint price) =>
            state.UseStateAndLazyPersist(state =>
            {
                if (state.Gold < price || !state.ActiveGames.Contains(gameID))
                    return Task.FromResult(default(ulong?));

                AddStatImpl(price, Statistics.MoneySpentHelpPowerup);
                AddStatImpl(1, Statistics.HelpPowerupUsed);

                state.Gold -= price;
                return Task.FromResult((ulong?)state.Gold);
            });

        public Task<(IEnumerable<GroupInfoDTO>? groups, ulong totalGold)> RefreshGroups(Guid gameID) =>
            state.UseStateAndLazyPersist(async state =>
            {
                var price = configReader.Config.ConfigValues.PriceToRefreshGroups;
                if (state.Gold < price)
                    throw new VerbatimException("Insufficient gold");

                var result = await GrainFactory.GetGrain<IGame>(gameID).RefreshGroups(this.GetPrimaryKey());
                if (result == null)
                    return (null, 0ul);

                AddStatImpl(price, Statistics.MoneySpentGroupChange);
                AddStatImpl(1, Statistics.GroupChangeUsed);

                state.Gold -= price;
                return (result.Select(g => (GroupInfoDTO)g).ToList().AsEnumerable().AsNullable(), state.Gold);
            });

        (bool inCoolDown, TimeSpan coolDownTimeRemaining, bool enoughRoundsWon, uint numRoundsWon) GetRoundWinRewardStatus() =>
            state.UseState(state =>
            {
                var configValues = configReader.Config.ConfigValues;

                var elapsedSinceLastTake = DateTime.Now - state.LastRoundWinRewardTakeTime;
                var remaining =
                    elapsedSinceLastTake >= configValues.RoundWinRewardInterval ?
                    TimeSpan.Zero :
                    configValues.RoundWinRewardInterval - elapsedSinceLastTake;
                var inCoolDown = remaining > TimeSpan.Zero;

                return (
                    inCoolDown,
                    remaining,
                    state.NumRoundsWonForReward >= configValues.NumRoundsToWinToGetReward,
                    state.NumRoundsWonForReward
                );
            });

        public Task<(ulong totalGold, TimeSpan timeUntilNextReward)> TakeRewardForWinningRounds() =>
            state.UseStateAndPersist(state =>
            {
                var status = GetRoundWinRewardStatus();

                if (status.inCoolDown)
                    throw new VerbatimException("Interval not elapsed yet");

                if (!status.enoughRoundsWon)
                    throw new VerbatimException("Insufficient rounds won");

                var configValues = configReader.Config.ConfigValues;

                state.NumRoundsWonForReward = 0;
                state.LastRoundWinRewardTakeTime = DateTime.Now;

                AddStatImpl(configValues.NumGoldRewardForWinningRounds, Statistics.RoundWinMoneyEarned);

                state.Gold += configValues.NumGoldRewardForWinningRounds;

                return Task.FromResult((state.Gold, configValues.RoundWinRewardInterval));
            });

        public Task<(bool success, ulong totalGold, TimeSpan duration)> ActivateUpgradedActiveGameLimit() =>
            state.UseStateAndMaybePersist(state =>
            {
                if (IsUpgradedActiveGameLimitActive)
                    return (false, (false, state.Gold, state.UpgradedActiveGameLimitEndTime - DateTime.Now));

                var config = configReader.Config.ConfigValues;
                if (state.Gold < config.UpgradedActiveGameLimitPrice)
                    return (false, (false, 0UL, TimeSpan.Zero));

                state.Gold -= config.UpgradedActiveGameLimitPrice;

                AddStatImpl(config.UpgradedActiveGameLimitPrice, Statistics.MoneySpentUpgradeActiveGameLimit);
                AddStatImpl(1, Statistics.UpgradeActiveGameLimitUsed);

                var duration = config.UpgradedActiveGameLimitTime;
                state.UpgradedActiveGameLimitEndTime = DateTime.Now + duration;
                return (true, (true, state.Gold, duration));
            });

        public Task<(IEnumerable<string> words, ulong? totalGold)> GetAnswers(string categoryName) =>
            state.UseStateAndMaybePersist(state =>
            {
                var config = configReader.Config;

                var category = config.GetCategory(categoryName);
                if (category == null)
                    return (false, (Array.Empty<string>().AsEnumerable(), default(ulong?)));

                var words = category.Answers;
                var gold = default(ulong?);

                if (!state.OwnedCategoryAnswers.Contains(category.CategoryName))
                {
                    var price = config.ConfigValues.GetAnswersPrice;
                    if (state.Gold < price)
                        throw new VerbatimException("Insufficient gold");

                    AddStatImpl(price, Statistics.MoneySpentRevealAnswers);
                    AddStatImpl(1, Statistics.RevealAnswersUsed);

                    state.Gold -= price;
                    state.OwnedCategoryAnswers.Add(category.CategoryName);
                    gold = state.Gold;
                }

                return (true, (words.AsEnumerable(), gold));
            });

        public Task<IEnumerable<string>> GetAnswersByVideoAd(string categoryName) =>
            state.UseStateAndMaybePersist(state =>
            {
                var config = configReader.Config;

                var category = config.GetCategory(categoryName);
                if (category == null)
                    return (false, Array.Empty<string>().AsEnumerable());

                var words = category.Answers;

                if (!state.OwnedCategoryAnswers.Contains(category.CategoryName))
                {
                    if (!getCategoryAnswersAdTracker.GetCanWatchAndIncrement())
                        return (false, Array.Empty<string>().AsEnumerable());

                    AddStatImpl(1, Statistics.VideoAdsWatched);
                    AddStatImpl(1, Statistics.GetCategoryAnswersVideoAdsWatched);
                    AddStatImpl(1, Statistics.RevealAnswersUsed);

                    state.OwnedCategoryAnswers.Add(category.CategoryName);
                }

                return (true, words.AsEnumerable());
            });

        public Task<(PlayerInfoDTO info, bool[] haveCategoryAnswers)> GetPlayerInfoAndOwnedCategories(IReadOnlyList<string> categories) =>
            state.UseState(async state =>
            {
                var ownedCategories = new bool[categories.Count];
                for (int i = 0; i < categories.Count; ++i)
                    ownedCategories[i] = state.OwnedCategoryAnswers.Contains(categories[i]);

                return (await GetPlayerInfo(), ownedCategories);
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

                var verifyResult = await GrainFactory.GetGrain<IBazaarIabVerifier>(0).VerifyBazaarPurchase(sku, purchaseToken);

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

        public Task UnsetFcmToken() => state.UseStateAndPersist(state => { state.FcmToken = null; });

        public Task SetNotificationsEnabled(bool enable) => state.UseStateAndPersist(state => { state.NotificationsEnabled = enable; });

        public Task SetCoinRewardVideoNotificationsEnabled(bool enable) => state.UseStateAndPersist(state => { state.CoinRewardVideoNotificationsEnabled = enable; });

        bool CanSendNotification(PlayerState state) => state.NotificationsEnabled && !string.IsNullOrEmpty(state.FcmToken);

        Task SendNotificationIfPossible(Func<Guid, string, Task> sendNotification) =>
            state.UseState(state =>
            {
                if (CanSendNotification(state))
                    return sendNotification(this.GetPrimaryKey(), state.FcmToken!);

                return Task.CompletedTask;
            });

        void SendNotificationIfPossible(Action<Guid, string> sendNotification) =>
            state.UseState(state =>
            {
                if (CanSendNotification(state))
                    sendNotification(this.GetPrimaryKey(), state.FcmToken!);
            });

        public Task SendMyTurnStartedNotification(Guid opponentID)
            => SendNotificationIfPossible(async (id, token) =>
            {
                var opName = await PlayerInfoHelper.GetName(GrainFactory, opponentID);
                fcmNotificationService.SendMyTurnStarted(id, token, opName);
            });

        public Task SendGameEndedNotification(Guid opponentID)
            => SendNotificationIfPossible(async (id, token) =>
            {
                var opName = await PlayerInfoHelper.GetName(GrainFactory, opponentID);
                fcmNotificationService.SendGameEnded(id, token, opName);
            });

        public Task SetTutorialProgress(ulong progress) => state.UseStateAndPersist(s => s.TutorialProgress = progress);

        public Task<ulong> GiveVideoAdReward() => state.UseStateAndMaybePersist(state =>
        {
            if (coinRewardAdTracker.GetCanWatchAndIncrement())
            {
                AddStatImpl(1, Statistics.VideoAdsWatched);
                AddStatImpl(1, Statistics.CoinRewardVideoAdsWatched);

                state.Gold += configReader.Config.ConfigValues.VideoAdGold;
                return (true, state.Gold);
            }

            return (false, state.Gold);
        });

        public Task<bool> ReceiveCoinGift(CoinGiftInfo gift) => state.UseStateAndMaybePersist(state =>
        {
            if (IsNewPlayer(state))
            {
                // We mistakenly attempted to gift a non-existing player
                DeactivateOnIdle();
                return (false, false);
            }

            state.CoinGifts.Add(gift);

            systemEndPoint?.SendCoinGiftReceived(this.GetPrimaryKey(), gift).Ignore();

            return (true, true);
        });

        public Task<ulong?> ClaimCoinGift(Guid giftID) => state.UseStateAndMaybePersist(state =>
        {
            var index = state.CoinGifts.FindIndex(g => g.GiftID == giftID);

            if (index < 0)
                return (false, default(ulong?));

            var gift = state.CoinGifts[index];
            state.CoinGifts.RemoveAt(index);

            if (gift.ExpiryTime < DateTime.Now)
                return (false, default(ulong?));

            state.Gold += gift.Count;
            return (true, state.Gold);
        });

        async Task UnregisterReminderIfExists(IEnumerable<IGrainReminder> reminders, string name)
        {
            var reminder = reminders.FirstOrDefault(r => r.ReminderName == name);
            if (reminder != null)
                await UnregisterReminder(reminder);
        }

        async Task UnregisterReminderIfExists(string name)
        {
            var reminder = await GetReminder(name);
            if (reminder != null)
                await UnregisterReminder(reminder);
        }

        async Task UnregisterOfflineReminders()
        {
            var reminders = await GetReminders();

            await UnregisterReminderIfExists(reminders, PlayerReminderNames.Day4Notification);
            await UnregisterReminderIfExists(reminders, PlayerReminderNames.RoundWinRewardNotification);
            await UnregisterReminderIfExists(reminders, PlayerReminderNames.CoinRewardVideoNotification);
        }

        Task RegisterOfflineReminders() =>
            state.UseState(async state =>
            {
                // If the player didn't log in, any previously registered reminders are still registered and perfectly valid
                if (!playerLoggedInDuringThisActivation || !CanSendNotification(state))
                    return;

                var timeFrames = configReader.Config.ConfigValues.NotificationTimeFrames!;

                var day4Time = TimeFrame.GetClosestInterval(DateTime.Now.AddDays(4), timeFrames, new[] { DateTime.Now }).GetRandomTimeInside();
                await RegisterOrUpdateReminder(PlayerReminderNames.Day4Notification, day4Time - DateTime.Now, TimeSpan.FromMinutes(5));

                var rewardStatus = GetRoundWinRewardStatus();
                if (rewardStatus.inCoolDown)
                {
                    var rewardTime = TimeFrame.GetClosestInterval(DateTime.Now + rewardStatus.coolDownTimeRemaining, timeFrames, new[] { DateTime.Now + rewardStatus.coolDownTimeRemaining }).GetRandomTimeInside();
                    await RegisterOrUpdateReminder(PlayerReminderNames.RoundWinRewardNotification, rewardTime - DateTime.Now, TimeSpan.FromMinutes(5));
                }

                if (state.CoinRewardVideoNotificationsEnabled == true)
                {
                    var coolDown = coinRewardAdTracker.GetCoolDownTimeRemaining();
                    if (coolDown > TimeSpan.Zero)
                        await RegisterOrUpdateReminder(PlayerReminderNames.CoinRewardVideoNotification, coolDown, TimeSpan.FromMinutes(2));
                }
            });

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case PlayerReminderNames.Day4Notification:
                    await UnregisterReminderIfExists(reminderName);
                    SendNotificationIfPossible((id, token) => fcmNotificationService.SendDay4Reminder(id, token));
                    break;

                case PlayerReminderNames.RoundWinRewardNotification:
                    await UnregisterReminderIfExists(reminderName);
                    SendNotificationIfPossible((id, token) => fcmNotificationService.SendRoundWinRewardAvailableReminder(id, token));
                    break;

                case PlayerReminderNames.CoinRewardVideoNotification:
                    await UnregisterReminderIfExists(reminderName);
                    SendNotificationIfPossible((id, token) => fcmNotificationService.SendCoinRewardVideoReadyReminder(id, token));
                    break;

                default:
                    logger.LogWarning($"Unknown reminder name {reminderName} received in Player grain with ID {this.GetPrimaryKey()}");
                    await UnregisterReminderIfExists(reminderName);
                    break;
            }
        }
    }
}
