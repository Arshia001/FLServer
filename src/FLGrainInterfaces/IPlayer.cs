using Bond;
using FLGrainInterfaces.Utility;
using Orleans;
using Orleans.Concurrency;
using OrleansBondUtils;
using OrleansIndexingGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public enum UpdatePasswordViaRecoveryTokenResult
    {
        Success,
        InvalidOrExpiredToken,
        PasswordNotComplexEnough
    }

    public class PlayerInfoHelper
    {
        public static Task<PlayerInfoDTO> GetInfo(IGrainFactory grainFactory, Guid playerID) => grainFactory.GetGrain<IPlayer>(playerID).GetPlayerInfo();
        public static Task<string> GetName(IGrainFactory grainFactory, Guid playerID) => grainFactory.GetGrain<IPlayer>(playerID).GetName();
    }

    [Schema]
    public struct StatisticWithParameter : IEquatable<StatisticWithParameter>
    {
        public StatisticWithParameter(Statistics statistic, int parameter)
        {
            Statistic = statistic;
            Parameter = parameter;
        }

        [Id(0)] public Statistics Statistic { get; set; }
        [Id(1)] public int Parameter { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is StatisticWithParameter parameter &&
                   Statistic == parameter.Statistic &&
                   Parameter == parameter.Parameter;
        }

        public bool Equals(StatisticWithParameter other) => Equals(other as object);

        public override int GetHashCode()
        {
            var hashCode = 1808521813;
            hashCode = hashCode * -1521134295 + Statistic.GetHashCode();
            hashCode = hashCode * -1521134295 + Parameter.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(StatisticWithParameter left, StatisticWithParameter right) => left.Equals(right);

        public static bool operator !=(StatisticWithParameter left, StatisticWithParameter right) => !(left == right);
    }

    //!! keep full history of past games somewhere else if needed, limit history here to a few items
    [Schema, BondSerializationTag("#p")]
    public class PlayerState
    {
        [Id(0)]
        public List<Guid> ActiveGames { get; private set; } = new List<Guid>();

        [Id(1)]
        public string Name { get; set; } = "";

        [Id(2)]
        public uint Level { get; set; }

        [Id(3)]
        public uint XP { get; set; }

        [Id(4)]
        public uint NumRoundsWonForReward { get; set; }

        [Id(5)]
        public DateTime LastRoundWinRewardTakeTime { get; set; }

        [Id(6)]
        public uint Score { get; set; }

        [Id(7)]
        public ulong Gold { get; set; }

        [Id(8)]
        public List<Guid> PastGames { get; private set; } = new List<Guid>();

        [Id(9)]
        public DateTime UpgradedActiveGameLimitEndTime { get; set; }

        [Id(10)]
        public HashSet<string> OwnedCategoryAnswers { get; private set; } = new HashSet<string>();

        [Id(11)]
        public Dictionary<StatisticWithParameter, ulong> StatisticsValues { get; private set; } = new Dictionary<StatisticWithParameter, ulong>();

        [Id(12)]
        public byte[] PasswordSalt { get; private set; } = CryptographyHelper.GeneratePasswordSalt();

        [Id(13)]
        public byte[]? PasswordHash { get; set; }

        [Id(14)]
        public string? Email { get; set; }

        [Id(16)]
        public HashSet<string> ProcessedIabTokens { get; private set; } = new HashSet<string>();

        [Id(17)]
        public string? FcmToken { get; set; }

        [Id(18)]
        public bool NotificationsEnabled { get; set; } = true;

        [Id(19)]
        public string? PasswordRecoveryToken { get; set; }

        [Id(20)]
        public DateTime? PasswordRecoveryTokenExpirationTime { get; set; }

        [Id(21)]
        public ulong TutorialProgress { get; set; }

        [Id(24)]
        public VideoAdLimitTrackerState? CoinRewardVideoTrackerState { get; set; }

        [Id(25)]
        public VideoAdLimitTrackerState? GetCategoryAnswersVideoTrackerState { get; set; }

        [Id(26)]
        public List<CoinGiftInfo> CoinGifts { get; set; } = new List<CoinGiftInfo>();

        [Id(27)]
        public bool? CoinRewardVideoNotificationsEnabled { get; set; } = null;

        [Id(28)]
        public AvatarManagerState? AvatarManagerState { get; set; }

        [Id(29)]
        public string? InviteCode { get; set; }

        [Id(30)]
        public IPlayer? Inviter { get; set; }

        // In order from newest to oldest, that is, index 0 is the most recent match
        [Id(31)]
        public List<CompetitionResult> MatchResultHistory { get; set; } = new List<CompetitionResult>();

        [Id(32)]
        public int NextInactivityReminderIndex { get; set; }

        [Id(33)]
        public Dictionary<Guid, uint> UnclaimedGameRewards { get; set; } = new Dictionary<Guid, uint>();

        [Id(34)]
        public string? BazaarToken { get; set; }

        [Id(35)]
        public uint NotifiedLevel { get; set; }
    }

    [BondSerializationTag("@p")]
    public interface IPlayer : IGrainWithGuidKey
    {
        Task<(OwnPlayerInfoDTO info, VideoAdLimitTrackerInfo coinRewardVideo, VideoAdLimitTrackerInfo getCategoryAnswersVideo,
            IEnumerable<CoinGiftInfo> coinGifts)> PerformStartupTasksAndGetInfo();
        Task PlayerDisconnected();

        Task<string> GetName();
        Task<PlayerInfoDTO> GetPlayerInfo();
        Task<OwnPlayerInfoDTO> GetOwnPlayerInfo();
        Task<PlayerLeaderBoardInfoDTO> GetLeaderBoardInfo();
        Task<(uint score, uint level, bool shouldJoinTutorialMatch)> GetMatchMakingInfo();
        Task<uint> GetScore();
        Task<(PlayerInfoDTO info, bool[] haveCategoryAnswers)> GetPlayerInfoAndOwnedCategories(IReadOnlyList<string> categories);
        Task SetNotifiedLevel(uint level);

        Task<bool> SetUsername(string username);
        Task<(RegistrationResult result, ulong totalGold)> PerformRegistration(string username, string email, string password, string? inviteCode);
        Task<SetEmailResult> SetEmail(string email);
        Task<SetPasswordResult> UpdatePassword(string newPassword);
        Task<bool> ValidatePassword(string password);
        Task SendPasswordRecoveryLink();
        Task<BazaarRegistrationResult> PerformBazaarTokenRegistration(string bazaarToken);
        Task<ulong?> RegisterInviteCode(string code);

        Task<(bool success, ulong totalGold)> BuyAvatarParts(IReadOnlyList<AvatarPartDTO> part);
        Task ActivateAvatar(AvatarDTO avatar);

        Task<bool> ValidatePasswordRecoveryToken(string token);
        Task<UpdatePasswordViaRecoveryTokenResult> UpdatePasswordViaRecoveryToken(string token, string newPassword);

        Task AddStats(List<StatisticValueDTO> values);

        Task<Immutable<IReadOnlyList<IGame>>> GetGames();
        Task<ulong?> ClaimGameReward(Guid gameID);
        Task<ulong?> ClearGameHistory();
        Task<(bool canEnter, Immutable<IEnumerable<Guid>> activeGames)> CheckCanEnterGameAndGetActiveGames();
        Task JoinGameAsFirstPlayer(IGame game);
        Task<PlayerInfoDTO> JoinGameAsSecondPlayer(IGame game);
        Task SecondPlayerJoinedGame(IGame game, Guid playerID);
        Task OnRoundCompleted(IGame game, uint myScore);
        Task OnRoundResult(IGame game, CompetitionResult result, ushort groupID);
        Task<(uint score, uint rank, uint level, uint xp, ulong gold, bool hasReward)>
            OnGameResult(IGame game, CompetitionResult result, uint myScore, uint scoreGain, bool gameExpired, Guid opponentID);

        Task<IEnumerable<CompetitionResult>> GetMatchResultHistory(); // In order from newest to oldest, that is, index 0 is the most recent match

        Task<(bool success, ulong totalGold, TimeSpan duration)> ActivateUpgradedActiveGameLimit();

        Task<ulong?> IncreaseRoundTime(Guid gameID, uint price);
        Task<ulong?> RevealWord(Guid gameID, uint price);

        Task<(IEnumerable<string> words, ulong? totalGold)> GetAnswers(string categoryName);
        Task<IEnumerable<string>> GetAnswersByVideoAd(string categoryName);
        Task<bool> HaveAnswersForCategory(string category);
        Task<(IReadOnlyList<bool> haveCategoryAnswers, bool rewardClaimed)> GetCompleteGameRelatedData(Guid gameID, IReadOnlyList<string> categories);
        Task<bool> GetSimplifiedGameRelatedData(Guid gameID); // Only one piece of data: reward claimed

        Task<ulong?> OnRefreshGroups(Guid gameID);

        Task<(ulong totalGold, TimeSpan timeUntilNextReward)> TakeRewardForWinningRounds();

        Task<(IabPurchaseResult result, ulong totalGold)> ProcessGoldPackPurchase(string sku, string purchaseToken);

        Task SetFcmToken(string token);
        Task UnsetFcmToken();
        Task SetNotificationsEnabled(bool enable);
        Task SetCoinRewardVideoNotificationsEnabled(bool enable);
        Task SendMyTurnStartedNotification(Guid opponentID);
        Task SendGameEndedNotification(Guid opponentID);

        Task SetTutorialProgress(ulong progress);

        Task<ulong> GiveVideoAdReward();

        Task<bool> ReceiveCoinGift(CoinGiftInfo gift);
        Task<ulong?> ClaimCoinGift(Guid giftID);
    }

    public static class PlayerIndex
    {
        //!! update indexer grains with nullability annotations
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        static readonly GrainIndexManager_Unique<string, IPlayer> byUsername =
            new GrainIndexManager_Unique<string, IPlayer>("p_un", 16384, new StringHashGenerator());

        static readonly GrainIndexManager_Unique<string, IPlayer> byEmail =
            new GrainIndexManager_Unique<string, IPlayer>("p_e", 16384, new StringHashGenerator());

        static readonly GrainIndexManager_Unique<string, IPlayer> byPasswordRecoveryToken =
            new GrainIndexManager_Unique<string, IPlayer>("p_prt", 16384, new StringHashGenerator());

        static readonly GrainIndexManager_Unique<string, IPlayer> byInviteCode =
            new GrainIndexManager_Unique<string, IPlayer>("p_ic", 16384, new StringHashGenerator());

        static readonly GrainIndexManager_Unique<string, IPlayer> byBazaarToken =
            new GrainIndexManager_Unique<string, IPlayer>("p_bt", 16384, new StringHashGenerator());

        public static async Task<bool> UpdateUsernameIfUnique(IGrainFactory grainFactory, IPlayer player, string? oldName, string name)
        {
            var result = await byUsername.UpdateIndexIfUnique(grainFactory, name.ToLower(), player);
            if (result && oldName != null)
                await byUsername.RemoveIndex(grainFactory, oldName.ToLower());
            return result;
        }

        public static async Task<bool> UpdateEmailIfUnique(IGrainFactory grainFactory, IPlayer player, string? oldEmail, string email)
        {
            var result = await byEmail.UpdateIndexIfUnique(grainFactory, email.ToLower(), player);
            if (result && oldEmail != null)
                await byEmail.RemoveIndex(grainFactory, oldEmail.ToLower());
            return result;
        }

        public static Task<IPlayer?> GetByEmail(IGrainFactory grainFactory, string email) => byEmail.GetGrain(grainFactory, email.ToLower());

        public static Task SetPasswordRecoveryToken(IGrainFactory grainFactory, IPlayer player, string token) =>
            byPasswordRecoveryToken.UpdateIndex(grainFactory, token, player);

        public static Task RemovePasswordRecoveryToken(IGrainFactory grainFactory, string token) =>
            byPasswordRecoveryToken.RemoveIndex(grainFactory, token);

        public static Task<IPlayer?> GetByRecoveryToken(IGrainFactory grainFactory, string token) => byPasswordRecoveryToken.GetGrain(grainFactory, token);

        public static Task<bool> SetInviteCode(IGrainFactory grainFactory, IPlayer player, string inviteCode) =>
            byInviteCode.UpdateIndexIfUnique(grainFactory, inviteCode, player);

        public static Task<IPlayer?> GetByInviteCode(IGrainFactory grainFactory, string inviteCode) => byInviteCode.GetGrain(grainFactory, inviteCode);

        public static Task<bool> SetBazaarToken(IGrainFactory grainFactory, IPlayer player, string bazaarToken) =>
            byBazaarToken.UpdateIndexIfUnique(grainFactory, bazaarToken, player);

        public static Task<IPlayer?> GetByBazaarToken(IGrainFactory grainFactory, string bazaarToken) => byBazaarToken.GetGrain(grainFactory, bazaarToken);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }
}
