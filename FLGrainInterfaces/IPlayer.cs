using Bond;
using LightMessage.Common.Messages;
using Orleans;
using Orleans.Concurrency;
using OrleansBondUtils;
using OrleansIndexingGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Text;
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
        public static Task<PlayerInfo> GetInfo(IGrainFactory grainFactory, Guid playerID) => grainFactory.GetGrain<IPlayer>(playerID).GetPlayerInfo();
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

        public override bool Equals(object obj)
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
        public List<IGame> ActiveGames { get; private set; } = new List<IGame>();

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
        public List<IGame> PastGames { get; private set; } = new List<IGame>();

        [Id(9)]
        public DateTime InfinitePlayEndTime { get; set; }

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
        public bool NotificationsEnabled { get; set; }

        [Id(19)]
        public string? PasswordRecoveryToken { get; set; }

        [Id(20)]
        public DateTime? PasswordRecoveryTokenExpirationTime { get; set;}
    }

    [BondSerializationTag("@p")]
    public interface IPlayer : IGrainWithGuidKey
    {
        Task<OwnPlayerInfo> PerformStartupTasksAndGetInfo();

        Task<string> GetName();
        Task<PlayerInfo> GetPlayerInfo();
        Task<OwnPlayerInfo> GetOwnPlayerInfo();
        Task<PlayerLeaderBoardInfo> GetLeaderBoardInfo();
        Task<(uint score, uint level)> GetMatchMakingInfo();
        Task<uint> GetScore();
        Task<(PlayerInfo info, bool[] haveCategoryAnswers)> GetPlayerInfoAndOwnedCategories(IReadOnlyList<string> categories);

        Task<bool> SetUsername(string username);
        Task<RegistrationResult> PerformRegistration(string username, string email, string password);
        Task<SetEmailResult> SetEmail(string email);
        Task<SetPasswordResult> UpdatePassword(string newPassword);
        Task<bool> ValidatePassword(string password);
        Task SendPasswordRecoveryLink();

        Task<bool> ValidatePasswordRecoveryToken(string token);
        Task<UpdatePasswordViaRecoveryTokenResult> UpdatePasswordViaRecoveryToken(string token, string newPassword);

        Task AddStats(List<StatisticValue> values);

        Task<Immutable<IReadOnlyList<IGame>>> GetGames();
        Task ClearFinishedGames();
        Task<bool> CanEnterGame();
        Task<byte> JoinGameAsFirstPlayer(IGame game);
        Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game);
        Task OnRoundCompleted(IGame game, uint myScore);
        Task OnRoundResult(IGame game, CompetitionResult result, ushort groupID);
        Task<(uint score, uint rank, uint level, uint xp, ulong gold)> OnGameResult(IGame game, CompetitionResult result, uint myScore, uint scoreGain);

        Task<(bool success, ulong totalGold, TimeSpan duration)> ActivateInfinitePlay();

        Task<(ulong? gold, TimeSpan? remainingTime)> IncreaseRoundTime(Guid gameID);
        Task<(ulong? gold, string? word, byte? wordScore)> RevealWord(Guid gameID);

        Task<(IEnumerable<string> words, ulong? totalGold)> GetAnswers(string category);
        Task<bool> HaveAnswersForCategory(string category);
        Task<IReadOnlyList<bool>> HaveAnswersForCategories(IReadOnlyList<string> categories);

        Task<IEnumerable<GroupInfoDTO>?> RefreshGroups(Guid gameID);

        Task<(ulong totalGold, TimeSpan timeUntilNextReward)> TakeRewardForWinningRounds();

        Task<(IabPurchaseResult result, ulong totalGold)> ProcessGoldPackPurchase(string sku, string purchaseToken);

        Task SetFcmToken(string token);
        Task SetNotificationsEnabled(bool enable);
        Task SendMyTurnStartedNotification(Guid opponentID);
        Task SendGameEndedNotification(Guid opponentID);
    }

    public static class PlayerIndex
    {
        static readonly GrainIndexManager_Unique<string, IPlayer> byUsername =
            new GrainIndexManager_Unique<string, IPlayer>("p_un", 16384, new StringHashGenerator());

        static readonly GrainIndexManager_Unique<string, IPlayer> byEmail =
            new GrainIndexManager_Unique<string, IPlayer>("p_e", 16384, new StringHashGenerator());

        static readonly GrainIndexManager_Unique<string, IPlayer> byPasswordRecoveryToken =
            new GrainIndexManager_Unique<string, IPlayer>("p_prt", 16384, new StringHashGenerator());

        public static Task<bool> UpdateUsernameIfUnique(IGrainFactory grainFactory, IPlayer player, string name) =>
            byUsername.UpdateIndexIfUnique(grainFactory, name.ToLower(), player);

        public static Task<bool> UpdateEmailIfUnique(IGrainFactory grainFactory, IPlayer player, string email) =>
            byEmail.UpdateIndexIfUnique(grainFactory, email.ToLower(), player);

        //!! update indexer grains with nullability annotations
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        public static Task<IPlayer?> GetByEmail(IGrainFactory grainFactory, string email) => byEmail.GetGrain(grainFactory, email.ToLower());
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

        public static Task SetPasswordRecoveryToken(IGrainFactory grainFactory, IPlayer player, string token) =>
            byPasswordRecoveryToken.UpdateIndex(grainFactory, token, player);

        public static Task RemovePasswordRecoveryToken(IGrainFactory grainFactory, string token) =>
            byPasswordRecoveryToken.RemoveIndex(grainFactory, token);

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        public static Task<IPlayer?> GetByRecoveryToken(IGrainFactory grainFactory, string token) => byPasswordRecoveryToken.GetGrain(grainFactory, token);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }
}
