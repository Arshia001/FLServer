using System.Linq;
using FLGrainInterfaces;
#nullable enable annotations 

namespace FLGrainInterfaces
{
    public interface ISystemEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
        System.Threading.Tasks.Task<bool> SendNumRoundsWonForRewardUpdated(System.Guid clientID, uint totalRoundsWon);
        System.Threading.Tasks.Task<bool> SendStatisticUpdated(System.Guid clientID, StatisticValueDTO stat);
        System.Threading.Tasks.Task<bool> SendCoinGiftReceived(System.Guid clientID, CoinGiftInfoDTO gift);
    }

    public interface ISuggestionEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
    }

    public interface IGameEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
        System.Threading.Tasks.Task<bool> SendOpponentJoined(System.Guid clientID, System.Guid gameID, PlayerInfoDTO opponentInfo, System.TimeSpan? expiryTimeRemaining);
        System.Threading.Tasks.Task<bool> SendOpponentTurnEnded(System.Guid clientID, System.Guid gameID, byte roundNumber, System.Collections.Generic.IEnumerable<WordScorePairDTO>? wordsPlayed, System.TimeSpan? expiryTimeRemaining);
        System.Threading.Tasks.Task<bool> SendGameEnded(System.Guid clientID, System.Guid gameID, uint myScore, uint theirScore, uint myPlayerScore, uint myPlayerRank, uint myLevel, uint myXP, ulong myGold, bool hasReward);
        System.Threading.Tasks.Task<bool> SendGameExpired(System.Guid clientID, System.Guid gameID, bool myWin, uint myPlayerScore, uint myPlayerRank, uint myLevel, uint myXP, ulong myGold, bool hasReward);
    }
}
#nullable enable annotations 

namespace FLGrainInterfaces
{
    public enum HandShakeMode
    {
        ClientID,
        EmailAndPassword,
        RecoveryEmailRequest,
        BazaarToken
    }

    public enum GameState
    {
        New,
        WaitingForSecondPlayer,
        InProgress,
        Finished,
        Expired
    }

    public enum RegistrationStatus
    {
        Unregistered,
        EmailAndPassword,
        BazaarToken
    }

    public enum BazaarRegistrationResult
    {
        Success,
        AlreadyHaveSameBazaarToken,
        AlreadyHaveOtherBazaarToken,
        AlreadyRegisteredWithOtherMethod,
        AccountWithTokenExists
    }

    public enum Statistics
    {
        GamesWon,
        GamesLost,
        GamesEndedInDraw,
        RoundsWon,
        RoundsLost,
        RoundsEndedInDraw,
        BestGameScore,
        BestRoundScore,
        GroupChosen_Param,
        GroupWon_Param,
        GroupLost_Param,
        GroupEndedInDraw_Param,
        WordsPlayedScore_Param,
        WordsPlayedDuplicate,
        WordsCorrected,
        RewardMoneyEarned,
        RoundWinMoneyEarned,
        MoneySpentCustomizations,
        MoneySpentTimePowerup,
        TimePowerupUsed,
        MoneySpentHelpPowerup,
        HelpPowerupUsed,
        MoneySpentGroupChange,
        GroupChangeUsed,
        MoneySpentRevealAnswers,
        RevealAnswersUsed,
        UNUSED_MoneySpentInfinitePlay,
        UNUSED_InfinitePlayUsed,
        GameLostDueToExpiry,
        RoundsCompleted,
        VideoAdsWatched,
        CoinRewardVideoAdsWatched,
        GetCategoryAnswersVideoAdsWatched,
        MoneySpentUpgradeActiveGameLimit,
        UpgradeActiveGameLimitUsed
    }

    public enum LeaderBoardSubject
    {
        Score,
        XP
    }

    public enum LeaderBoardGroup
    {
        All,
        Friends,
        Clan
    }

    public enum GoldPackTag
    {
        None,
        BestValue,
        BestSelling
    }

    public enum IabPurchaseResult
    {
        Success,
        AlreadyProcessed,
        Invalid,
        FailedToContactValidationService,
        UnknownError
    }

    public enum RegistrationResult
    {
        Success,
        EmailAddressInUse,
        InvalidEmailAddress,
        PasswordNotComplexEnough,
        UsernameInUse,
        AlreadyRegistered,
        InvalidInviteCode
    }

    public enum SetEmailResult
    {
        Success,
        NotRegistered,
        EmailAddressInUse,
        InvalidEmailAddress
    }

    public enum SetPasswordResult
    {
        Success,
        NotRegistered,
        PasswordNotComplexEnough
    }

    public enum CoinGiftSubject
    {
        GiftToAll,
        SuggestedWords,
        SuggestedCategories,
        FriendInvited
    }

    public enum AvatarPartType
    {
        HeadShape,
        Hair,
        Eyes,
        Mouth,
        Glasses
    }

    [Orleans.Concurrency.Immutable]
    public class AvatarPartDTO
    {
        public AvatarPartDTO(AvatarPartType partType, ushort id)
        {
            this.PartType = partType;
            this.ID = id;
        }

        public AvatarPartType PartType { get; }
        public ushort ID { get; }

        public static implicit operator AvatarPartDTO(FLGrainInterfaces.Utility.AvatarPart obj) => new AvatarPartDTO(obj.PartType, obj.ID);
        public static implicit operator FLGrainInterfaces.Utility.AvatarPart(AvatarPartDTO obj) => new FLGrainInterfaces.Utility.AvatarPart { PartType = obj.PartType, ID = obj.ID };

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.UEnum(PartType), LightMessage.Common.WireProtocol.Param.UInt(ID));

        public static AvatarPartDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new AvatarPartDTO(array[0].AsUEnum<AvatarPartType>().Value, (ushort)array[1].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class AvatarDTO
    {
        public AvatarDTO(System.Collections.Generic.IEnumerable<AvatarPartDTO> parts)
        {
            this.Parts = parts.ToList();
        }

        public System.Collections.Generic.IReadOnlyList<AvatarPartDTO> Parts { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.Array(Parts.Select(a => a?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null())));

        public static AvatarDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new AvatarDTO(array[0].AsArray.Select(a => AvatarPartDTO.FromParam(a)).ToList());
        }
    }

    [Orleans.Concurrency.Immutable]
    public class AvatarPartConfigDTO
    {
        public AvatarPartConfigDTO(AvatarPartType partType, ushort id, uint price, ushort minimumLevel)
        {
            this.PartType = partType;
            this.ID = id;
            this.Price = price;
            this.MinimumLevel = minimumLevel;
        }

        public AvatarPartType PartType { get; }
        public ushort ID { get; }
        public uint Price { get; }
        public ushort MinimumLevel { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.UEnum(PartType), LightMessage.Common.WireProtocol.Param.UInt(ID), LightMessage.Common.WireProtocol.Param.UInt(Price), LightMessage.Common.WireProtocol.Param.UInt(MinimumLevel));

        public static AvatarPartConfigDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new AvatarPartConfigDTO(array[0].AsUEnum<AvatarPartType>().Value, (ushort)array[1].AsUInt.Value, (uint)array[2].AsUInt.Value, (ushort)array[3].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class PlayerLeaderBoardInfoDTO
    {
        public PlayerLeaderBoardInfoDTO(string name, AvatarDTO avatar)
        {
            this.Name = name;
            this.Avatar = avatar;
        }

        public string Name { get; }
        public AvatarDTO Avatar { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.String(Name), Avatar?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null());

        public static PlayerLeaderBoardInfoDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new PlayerLeaderBoardInfoDTO(array[0].AsString, AvatarDTO.FromParam(array[1]));
        }
    }

    [Orleans.Concurrency.Immutable]
    public class LeaderBoardEntryDTO
    {
        public LeaderBoardEntryDTO(PlayerLeaderBoardInfoDTO? info, ulong rank, ulong score)
        {
            this.Info = info;
            this.Rank = rank;
            this.Score = score;
        }

        public PlayerLeaderBoardInfoDTO? Info { get; }
        public ulong Rank { get; }
        public ulong Score { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(Info?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null(), LightMessage.Common.WireProtocol.Param.UInt(Rank), LightMessage.Common.WireProtocol.Param.UInt(Score));

        public static LeaderBoardEntryDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new LeaderBoardEntryDTO(PlayerLeaderBoardInfoDTO.FromParam(array[0]), array[1].AsUInt.Value, array[2].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class PlayerInfoDTO
    {
        public PlayerInfoDTO(System.Guid id, string name, uint level, AvatarDTO avatar)
        {
            this.ID = id;
            this.Name = name;
            this.Level = level;
            this.Avatar = avatar;
        }

        public System.Guid ID { get; }
        public string Name { get; }
        public uint Level { get; }
        public AvatarDTO Avatar { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.Guid(ID), LightMessage.Common.WireProtocol.Param.String(Name), LightMessage.Common.WireProtocol.Param.UInt(Level), Avatar?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null());

        public static PlayerInfoDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new PlayerInfoDTO(array[0].AsGuid.Value, array[1].AsString, (uint)array[2].AsUInt.Value, AvatarDTO.FromParam(array[3]));
        }
    }

    [Orleans.Concurrency.Immutable]
    public class StatisticValueDTO
    {
        public StatisticValueDTO(Statistics statistic, int parameter, ulong value)
        {
            this.Statistic = statistic;
            this.Parameter = parameter;
            this.Value = value;
        }

        public Statistics Statistic { get; }
        public int Parameter { get; }
        public ulong Value { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.UEnum(Statistic), LightMessage.Common.WireProtocol.Param.Int(Parameter), LightMessage.Common.WireProtocol.Param.UInt(Value));

        public static StatisticValueDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new StatisticValueDTO(array[0].AsUEnum<Statistics>().Value, (int)array[1].AsInt.Value, array[2].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class OwnPlayerInfoDTO
    {
        public OwnPlayerInfoDTO(string name, string? email, uint xp, uint level, uint notifiedLevel, uint nextLevelXPThreshold, uint score, uint rank, ulong gold, uint currentNumRoundsWonForReward, System.TimeSpan nextRoundWinRewardTimeRemaining, System.TimeSpan? upgradedActiveGameLimitTimeRemaining, System.Collections.Generic.IEnumerable<StatisticValueDTO> statisticsValues, RegistrationStatus registrationStatus, bool notificationsEnabled, ulong tutorialProgress, bool? coinRewardVideoNotificationsEnabled, AvatarDTO avatar, System.Collections.Generic.IEnumerable<AvatarPartDTO> ownedAvatarParts, string inviteCode, bool inviteCodeEntered)
        {
            this.Name = name;
            this.Email = email;
            this.XP = xp;
            this.Level = level;
            this.NotifiedLevel = notifiedLevel;
            this.NextLevelXPThreshold = nextLevelXPThreshold;
            this.Score = score;
            this.Rank = rank;
            this.Gold = gold;
            this.CurrentNumRoundsWonForReward = currentNumRoundsWonForReward;
            this.NextRoundWinRewardTimeRemaining = nextRoundWinRewardTimeRemaining;
            this.UpgradedActiveGameLimitTimeRemaining = upgradedActiveGameLimitTimeRemaining;
            this.StatisticsValues = statisticsValues.ToList();
            this.RegistrationStatus = registrationStatus;
            this.NotificationsEnabled = notificationsEnabled;
            this.TutorialProgress = tutorialProgress;
            this.CoinRewardVideoNotificationsEnabled = coinRewardVideoNotificationsEnabled;
            this.Avatar = avatar;
            this.OwnedAvatarParts = ownedAvatarParts.ToList();
            this.InviteCode = inviteCode;
            this.InviteCodeEntered = inviteCodeEntered;
        }

        public string Name { get; }
        public string? Email { get; }
        public uint XP { get; }
        public uint Level { get; }
        public uint NotifiedLevel { get; }
        public uint NextLevelXPThreshold { get; }
        public uint Score { get; }
        public uint Rank { get; }
        public ulong Gold { get; }
        public uint CurrentNumRoundsWonForReward { get; }
        public System.TimeSpan NextRoundWinRewardTimeRemaining { get; }
        public System.TimeSpan? UpgradedActiveGameLimitTimeRemaining { get; }
        public System.Collections.Generic.IReadOnlyList<StatisticValueDTO> StatisticsValues { get; }
        public RegistrationStatus RegistrationStatus { get; }
        public bool NotificationsEnabled { get; }
        public ulong TutorialProgress { get; }
        public bool? CoinRewardVideoNotificationsEnabled { get; }
        public AvatarDTO Avatar { get; }
        public System.Collections.Generic.IReadOnlyList<AvatarPartDTO> OwnedAvatarParts { get; }
        public string InviteCode { get; }
        public bool InviteCodeEntered { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.String(Name), LightMessage.Common.WireProtocol.Param.String(Email), LightMessage.Common.WireProtocol.Param.UInt(XP), LightMessage.Common.WireProtocol.Param.UInt(Level), LightMessage.Common.WireProtocol.Param.UInt(NotifiedLevel), LightMessage.Common.WireProtocol.Param.UInt(NextLevelXPThreshold), LightMessage.Common.WireProtocol.Param.UInt(Score), LightMessage.Common.WireProtocol.Param.UInt(Rank), LightMessage.Common.WireProtocol.Param.UInt(Gold), LightMessage.Common.WireProtocol.Param.UInt(CurrentNumRoundsWonForReward), LightMessage.Common.WireProtocol.Param.TimeSpan(NextRoundWinRewardTimeRemaining), LightMessage.Common.WireProtocol.Param.TimeSpan(UpgradedActiveGameLimitTimeRemaining), LightMessage.Common.WireProtocol.Param.Array(StatisticsValues.Select(a => a?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null())), LightMessage.Common.WireProtocol.Param.UEnum(RegistrationStatus), LightMessage.Common.WireProtocol.Param.Boolean(NotificationsEnabled), LightMessage.Common.WireProtocol.Param.UInt(TutorialProgress), LightMessage.Common.WireProtocol.Param.Boolean(CoinRewardVideoNotificationsEnabled), Avatar?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null(), LightMessage.Common.WireProtocol.Param.Array(OwnedAvatarParts.Select(a => a?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null())), LightMessage.Common.WireProtocol.Param.String(InviteCode), LightMessage.Common.WireProtocol.Param.Boolean(InviteCodeEntered));

        public static OwnPlayerInfoDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new OwnPlayerInfoDTO(array[0].AsString, array[1].AsString, (uint)array[2].AsUInt.Value, (uint)array[3].AsUInt.Value, (uint)array[4].AsUInt.Value, (uint)array[5].AsUInt.Value, (uint)array[6].AsUInt.Value, (uint)array[7].AsUInt.Value, array[8].AsUInt.Value, (uint)array[9].AsUInt.Value, array[10].AsTimeSpan.Value, array[11].AsTimeSpan, array[12].AsArray.Select(a => StatisticValueDTO.FromParam(a)).ToList(), array[13].AsUEnum<RegistrationStatus>().Value, array[14].AsBoolean.Value, array[15].AsUInt.Value, array[16].AsBoolean, AvatarDTO.FromParam(array[17]), array[18].AsArray.Select(a => AvatarPartDTO.FromParam(a)).ToList(), array[19].AsString, array[20].AsBoolean.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class WordScorePairDTO
    {
        public WordScorePairDTO(string word, byte score)
        {
            this.Word = word;
            this.Score = score;
        }

        public string Word { get; }
        public byte Score { get; }

        public static implicit operator WordScorePairDTO(FLGameLogic.WordScorePair obj) => new WordScorePairDTO(obj.word, obj.score);
        public static implicit operator FLGameLogic.WordScorePair(WordScorePairDTO obj) => new FLGameLogic.WordScorePair { word = obj.Word, score = obj.Score };

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.String(Word), LightMessage.Common.WireProtocol.Param.UInt(Score));

        public static WordScorePairDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new WordScorePairDTO(array[0].AsString, (byte)array[1].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class GameInfoDTO
    {
        public GameInfoDTO(PlayerInfoDTO? otherPlayerInfo, byte numRounds, System.Collections.Generic.IEnumerable<string> categories, System.Collections.Generic.IEnumerable<bool> haveCategoryAnswers, System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<WordScorePairDTO>> myWordsPlayed, System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<WordScorePairDTO>>? theirWordsPlayed, bool myTurnFirst, byte numTurnsTakenByOpponent, bool expired, bool expiredForMe, System.TimeSpan? expiryTimeRemaining, uint roundTimeExtensions, System.TimeSpan? myTurnTimeRemaining, bool rewardClaimed)
        {
            this.OtherPlayerInfo = otherPlayerInfo;
            this.NumRounds = numRounds;
            this.Categories = categories.ToList();
            this.HaveCategoryAnswers = haveCategoryAnswers.ToList();
            this.MyWordsPlayed = myWordsPlayed.Select(a => a.ToList()).ToList();
            this.TheirWordsPlayed = theirWordsPlayed?.Select(a => a.ToList()).ToList();
            this.MyTurnFirst = myTurnFirst;
            this.NumTurnsTakenByOpponent = numTurnsTakenByOpponent;
            this.Expired = expired;
            this.ExpiredForMe = expiredForMe;
            this.ExpiryTimeRemaining = expiryTimeRemaining;
            this.RoundTimeExtensions = roundTimeExtensions;
            this.MyTurnTimeRemaining = myTurnTimeRemaining;
            this.RewardClaimed = rewardClaimed;
        }

        public PlayerInfoDTO? OtherPlayerInfo { get; }
        public byte NumRounds { get; }
        public System.Collections.Generic.IReadOnlyList<string> Categories { get; }
        public System.Collections.Generic.IReadOnlyList<bool> HaveCategoryAnswers { get; }
        public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<WordScorePairDTO>> MyWordsPlayed { get; }
        public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<WordScorePairDTO>>? TheirWordsPlayed { get; }
        public bool MyTurnFirst { get; }
        public byte NumTurnsTakenByOpponent { get; }
        public bool Expired { get; }
        public bool ExpiredForMe { get; }
        public System.TimeSpan? ExpiryTimeRemaining { get; }
        public uint RoundTimeExtensions { get; }
        public System.TimeSpan? MyTurnTimeRemaining { get; }
        public bool RewardClaimed { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(OtherPlayerInfo?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null(), LightMessage.Common.WireProtocol.Param.UInt(NumRounds), LightMessage.Common.WireProtocol.Param.Array(Categories.Select(a => LightMessage.Common.WireProtocol.Param.String(a))), LightMessage.Common.WireProtocol.Param.Array(HaveCategoryAnswers.Select(a => LightMessage.Common.WireProtocol.Param.Boolean(a))), LightMessage.Common.WireProtocol.Param.Array(MyWordsPlayed.Select(a => LightMessage.Common.WireProtocol.Param.Array(a.Select(b => b?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null())))), LightMessage.Common.WireProtocol.Param.Array(TheirWordsPlayed?.Select(a => LightMessage.Common.WireProtocol.Param.Array(a.Select(b => b?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null())))), LightMessage.Common.WireProtocol.Param.Boolean(MyTurnFirst), LightMessage.Common.WireProtocol.Param.UInt(NumTurnsTakenByOpponent), LightMessage.Common.WireProtocol.Param.Boolean(Expired), LightMessage.Common.WireProtocol.Param.Boolean(ExpiredForMe), LightMessage.Common.WireProtocol.Param.TimeSpan(ExpiryTimeRemaining), LightMessage.Common.WireProtocol.Param.UInt(RoundTimeExtensions), LightMessage.Common.WireProtocol.Param.TimeSpan(MyTurnTimeRemaining), LightMessage.Common.WireProtocol.Param.Boolean(RewardClaimed));

        public static GameInfoDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new GameInfoDTO(PlayerInfoDTO.FromParam(array[0]), (byte)array[1].AsUInt.Value, array[2].AsArray.Select(a => a.AsString).ToList(), array[3].AsArray.Select(a => a.AsBoolean.Value).ToList(), array[4].AsArray.Select(a => a.AsArray.Select(b => WordScorePairDTO.FromParam(b)).ToList()).ToList(), array[5].AsArray?.Select(a => a.AsArray.Select(b => WordScorePairDTO.FromParam(b)).ToList()).ToList(), array[6].AsBoolean.Value, (byte)array[7].AsUInt.Value, array[8].AsBoolean.Value, array[9].AsBoolean.Value, array[10].AsTimeSpan, (uint)array[11].AsUInt.Value, array[12].AsTimeSpan, array[13].AsBoolean.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class SimplifiedGameInfoDTO
    {
        public SimplifiedGameInfoDTO(System.Guid gameID, GameState gameState, string? otherPlayerName, AvatarDTO? otherPlayerAvatar, bool myTurn, byte myScore, byte theirScore, bool winnerOfExpiredGame, System.TimeSpan? expiryTimeRemaining, System.TimeSpan? myTurnTimeRemaining, bool rewardClaimed)
        {
            this.GameID = gameID;
            this.GameState = gameState;
            this.OtherPlayerName = otherPlayerName;
            this.OtherPlayerAvatar = otherPlayerAvatar;
            this.MyTurn = myTurn;
            this.MyScore = myScore;
            this.TheirScore = theirScore;
            this.WinnerOfExpiredGame = winnerOfExpiredGame;
            this.ExpiryTimeRemaining = expiryTimeRemaining;
            this.MyTurnTimeRemaining = myTurnTimeRemaining;
            this.RewardClaimed = rewardClaimed;
        }

        public System.Guid GameID { get; }
        public GameState GameState { get; }
        public string? OtherPlayerName { get; }
        public AvatarDTO? OtherPlayerAvatar { get; }
        public bool MyTurn { get; }
        public byte MyScore { get; }
        public byte TheirScore { get; }
        public bool WinnerOfExpiredGame { get; }
        public System.TimeSpan? ExpiryTimeRemaining { get; }
        public System.TimeSpan? MyTurnTimeRemaining { get; }
        public bool RewardClaimed { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.Guid(GameID), LightMessage.Common.WireProtocol.Param.UEnum(GameState), LightMessage.Common.WireProtocol.Param.String(OtherPlayerName), OtherPlayerAvatar?.ToParam() ?? LightMessage.Common.WireProtocol.Param.Null(), LightMessage.Common.WireProtocol.Param.Boolean(MyTurn), LightMessage.Common.WireProtocol.Param.UInt(MyScore), LightMessage.Common.WireProtocol.Param.UInt(TheirScore), LightMessage.Common.WireProtocol.Param.Boolean(WinnerOfExpiredGame), LightMessage.Common.WireProtocol.Param.TimeSpan(ExpiryTimeRemaining), LightMessage.Common.WireProtocol.Param.TimeSpan(MyTurnTimeRemaining), LightMessage.Common.WireProtocol.Param.Boolean(RewardClaimed));

        public static SimplifiedGameInfoDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new SimplifiedGameInfoDTO(array[0].AsGuid.Value, array[1].AsUEnum<GameState>().Value, array[2].AsString, AvatarDTO.FromParam(array[3]), array[4].AsBoolean.Value, (byte)array[5].AsUInt.Value, (byte)array[6].AsUInt.Value, array[7].AsBoolean.Value, array[8].AsTimeSpan, array[9].AsTimeSpan, array[10].AsBoolean.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class ConfigValuesDTO
    {
        public ConfigValuesDTO(byte numRoundsToWinToGetReward, System.TimeSpan roundWinRewardInterval, uint numGoldRewardForWinningRounds, uint priceToRefreshGroups, System.TimeSpan roundTimeExtension, System.Collections.Generic.IEnumerable<uint> roundTimeExtensionPrices, System.Collections.Generic.IEnumerable<uint> revealWordPrices, uint getAnswersPrice, uint maxActiveGames, uint upgradedActiveGameLimitPrice, uint maxActiveGamesWhenUpgraded, uint numTimeExtensionsPerRound, byte refreshGroupsAllowedPerRound, System.TimeSpan upgradedActiveGameLimitTime, byte numRoundsPerGame, byte numGroupChoices, System.TimeSpan clientTimePerRound, System.TimeSpan gameInactivityTimeout, uint maxScoreGain, uint minScoreGain, float loserScoreLossRatio, uint winnerXPGain, uint loserXPGain, uint drawXPGain, uint winnerGoldGain, uint loserGoldGain, uint drawGoldGain, uint videoAdGold, uint inviterReward, uint inviteeReward)
        {
            this.NumRoundsToWinToGetReward = numRoundsToWinToGetReward;
            this.RoundWinRewardInterval = roundWinRewardInterval;
            this.NumGoldRewardForWinningRounds = numGoldRewardForWinningRounds;
            this.PriceToRefreshGroups = priceToRefreshGroups;
            this.RoundTimeExtension = roundTimeExtension;
            this.RoundTimeExtensionPrices = roundTimeExtensionPrices.ToList();
            this.RevealWordPrices = revealWordPrices.ToList();
            this.GetAnswersPrice = getAnswersPrice;
            this.MaxActiveGames = maxActiveGames;
            this.UpgradedActiveGameLimitPrice = upgradedActiveGameLimitPrice;
            this.MaxActiveGamesWhenUpgraded = maxActiveGamesWhenUpgraded;
            this.NumTimeExtensionsPerRound = numTimeExtensionsPerRound;
            this.RefreshGroupsAllowedPerRound = refreshGroupsAllowedPerRound;
            this.UpgradedActiveGameLimitTime = upgradedActiveGameLimitTime;
            this.NumRoundsPerGame = numRoundsPerGame;
            this.NumGroupChoices = numGroupChoices;
            this.ClientTimePerRound = clientTimePerRound;
            this.GameInactivityTimeout = gameInactivityTimeout;
            this.MaxScoreGain = maxScoreGain;
            this.MinScoreGain = minScoreGain;
            this.LoserScoreLossRatio = loserScoreLossRatio;
            this.WinnerXPGain = winnerXPGain;
            this.LoserXPGain = loserXPGain;
            this.DrawXPGain = drawXPGain;
            this.WinnerGoldGain = winnerGoldGain;
            this.LoserGoldGain = loserGoldGain;
            this.DrawGoldGain = drawGoldGain;
            this.VideoAdGold = videoAdGold;
            this.InviterReward = inviterReward;
            this.InviteeReward = inviteeReward;
        }

        public byte NumRoundsToWinToGetReward { get; }
        public System.TimeSpan RoundWinRewardInterval { get; }
        public uint NumGoldRewardForWinningRounds { get; }
        public uint PriceToRefreshGroups { get; }
        public System.TimeSpan RoundTimeExtension { get; }
        public System.Collections.Generic.IReadOnlyList<uint> RoundTimeExtensionPrices { get; }
        public System.Collections.Generic.IReadOnlyList<uint> RevealWordPrices { get; }
        public uint GetAnswersPrice { get; }
        public uint MaxActiveGames { get; }
        public uint UpgradedActiveGameLimitPrice { get; }
        public uint MaxActiveGamesWhenUpgraded { get; }
        public uint NumTimeExtensionsPerRound { get; }
        public byte RefreshGroupsAllowedPerRound { get; }
        public System.TimeSpan UpgradedActiveGameLimitTime { get; }
        public byte NumRoundsPerGame { get; }
        public byte NumGroupChoices { get; }
        public System.TimeSpan ClientTimePerRound { get; }
        public System.TimeSpan GameInactivityTimeout { get; }
        public uint MaxScoreGain { get; }
        public uint MinScoreGain { get; }
        public float LoserScoreLossRatio { get; }
        public uint WinnerXPGain { get; }
        public uint LoserXPGain { get; }
        public uint DrawXPGain { get; }
        public uint WinnerGoldGain { get; }
        public uint LoserGoldGain { get; }
        public uint DrawGoldGain { get; }
        public uint VideoAdGold { get; }
        public uint InviterReward { get; }
        public uint InviteeReward { get; }

        public static implicit operator ConfigValuesDTO(FLGrainInterfaces.Configuration.ConfigValues obj) => new ConfigValuesDTO(obj.NumRoundsToWinToGetReward, obj.RoundWinRewardInterval, obj.NumGoldRewardForWinningRounds, obj.PriceToRefreshGroups, obj.RoundTimeExtension, obj.RoundTimeExtensionPrices.ToList(), obj.RevealWordPrices.ToList(), obj.GetAnswersPrice, obj.MaxActiveGames, obj.UpgradedActiveGameLimitPrice, obj.MaxActiveGamesWhenUpgraded, obj.NumTimeExtensionsPerRound, obj.RefreshGroupsAllowedPerRound, obj.UpgradedActiveGameLimitTime, obj.NumRoundsPerGame, obj.NumGroupChoices, obj.ClientTimePerRound, obj.GameInactivityTimeout, obj.MaxScoreGain, obj.MinScoreGain, obj.LoserScoreLossRatio, obj.WinnerXPGain, obj.LoserXPGain, obj.DrawXPGain, obj.WinnerGoldGain, obj.LoserGoldGain, obj.DrawGoldGain, obj.VideoAdGold, obj.InviterReward, obj.InviteeReward);

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.UInt(NumRoundsToWinToGetReward), LightMessage.Common.WireProtocol.Param.TimeSpan(RoundWinRewardInterval), LightMessage.Common.WireProtocol.Param.UInt(NumGoldRewardForWinningRounds), LightMessage.Common.WireProtocol.Param.UInt(PriceToRefreshGroups), LightMessage.Common.WireProtocol.Param.TimeSpan(RoundTimeExtension), LightMessage.Common.WireProtocol.Param.Array(RoundTimeExtensionPrices.Select(a => LightMessage.Common.WireProtocol.Param.UInt(a))), LightMessage.Common.WireProtocol.Param.Array(RevealWordPrices.Select(a => LightMessage.Common.WireProtocol.Param.UInt(a))), LightMessage.Common.WireProtocol.Param.UInt(GetAnswersPrice), LightMessage.Common.WireProtocol.Param.UInt(MaxActiveGames), LightMessage.Common.WireProtocol.Param.UInt(UpgradedActiveGameLimitPrice), LightMessage.Common.WireProtocol.Param.UInt(MaxActiveGamesWhenUpgraded), LightMessage.Common.WireProtocol.Param.UInt(NumTimeExtensionsPerRound), LightMessage.Common.WireProtocol.Param.UInt(RefreshGroupsAllowedPerRound), LightMessage.Common.WireProtocol.Param.TimeSpan(UpgradedActiveGameLimitTime), LightMessage.Common.WireProtocol.Param.UInt(NumRoundsPerGame), LightMessage.Common.WireProtocol.Param.UInt(NumGroupChoices), LightMessage.Common.WireProtocol.Param.TimeSpan(ClientTimePerRound), LightMessage.Common.WireProtocol.Param.TimeSpan(GameInactivityTimeout), LightMessage.Common.WireProtocol.Param.UInt(MaxScoreGain), LightMessage.Common.WireProtocol.Param.UInt(MinScoreGain), LightMessage.Common.WireProtocol.Param.Float(LoserScoreLossRatio), LightMessage.Common.WireProtocol.Param.UInt(WinnerXPGain), LightMessage.Common.WireProtocol.Param.UInt(LoserXPGain), LightMessage.Common.WireProtocol.Param.UInt(DrawXPGain), LightMessage.Common.WireProtocol.Param.UInt(WinnerGoldGain), LightMessage.Common.WireProtocol.Param.UInt(LoserGoldGain), LightMessage.Common.WireProtocol.Param.UInt(DrawGoldGain), LightMessage.Common.WireProtocol.Param.UInt(VideoAdGold), LightMessage.Common.WireProtocol.Param.UInt(InviterReward), LightMessage.Common.WireProtocol.Param.UInt(InviteeReward));

        public static ConfigValuesDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new ConfigValuesDTO((byte)array[0].AsUInt.Value, array[1].AsTimeSpan.Value, (uint)array[2].AsUInt.Value, (uint)array[3].AsUInt.Value, array[4].AsTimeSpan.Value, array[5].AsArray.Select(a => (uint)a.AsUInt.Value).ToList(), array[6].AsArray.Select(a => (uint)a.AsUInt.Value).ToList(), (uint)array[7].AsUInt.Value, (uint)array[8].AsUInt.Value, (uint)array[9].AsUInt.Value, (uint)array[10].AsUInt.Value, (uint)array[11].AsUInt.Value, (byte)array[12].AsUInt.Value, array[13].AsTimeSpan.Value, (byte)array[14].AsUInt.Value, (byte)array[15].AsUInt.Value, array[16].AsTimeSpan.Value, array[17].AsTimeSpan.Value, (uint)array[18].AsUInt.Value, (uint)array[19].AsUInt.Value, array[20].AsFloat.Value, (uint)array[21].AsUInt.Value, (uint)array[22].AsUInt.Value, (uint)array[23].AsUInt.Value, (uint)array[24].AsUInt.Value, (uint)array[25].AsUInt.Value, (uint)array[26].AsUInt.Value, (uint)array[27].AsUInt.Value, (uint)array[28].AsUInt.Value, (uint)array[29].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class GoldPackConfigDTO
    {
        public GoldPackConfigDTO(string sku, uint numGold, string title, GoldPackTag tag)
        {
            this.Sku = sku;
            this.NumGold = numGold;
            this.Title = title;
            this.Tag = tag;
        }

        public string Sku { get; }
        public uint NumGold { get; }
        public string Title { get; }
        public GoldPackTag Tag { get; }

        public static implicit operator GoldPackConfigDTO(FLGrainInterfaces.Configuration.GoldPackConfig obj) => new GoldPackConfigDTO(obj.Sku, obj.NumGold, obj.Title, obj.Tag);

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.String(Sku), LightMessage.Common.WireProtocol.Param.UInt(NumGold), LightMessage.Common.WireProtocol.Param.String(Title), LightMessage.Common.WireProtocol.Param.UEnum(Tag));

        public static GoldPackConfigDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new GoldPackConfigDTO(array[0].AsString, (uint)array[1].AsUInt.Value, array[2].AsString, array[3].AsUEnum<GoldPackTag>().Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class VideoAdTrackerInfoDTO
    {
        public VideoAdTrackerInfoDTO(System.TimeSpan? timeSinceLastWatched, uint numberWatchedToday, System.TimeSpan interval, uint numberPerDay)
        {
            this.TimeSinceLastWatched = timeSinceLastWatched;
            this.NumberWatchedToday = numberWatchedToday;
            this.Interval = interval;
            this.NumberPerDay = numberPerDay;
        }

        public System.TimeSpan? TimeSinceLastWatched { get; }
        public uint NumberWatchedToday { get; }
        public System.TimeSpan Interval { get; }
        public uint NumberPerDay { get; }

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.TimeSpan(TimeSinceLastWatched), LightMessage.Common.WireProtocol.Param.UInt(NumberWatchedToday), LightMessage.Common.WireProtocol.Param.TimeSpan(Interval), LightMessage.Common.WireProtocol.Param.UInt(NumberPerDay));

        public static VideoAdTrackerInfoDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new VideoAdTrackerInfoDTO(array[0].AsTimeSpan, (uint)array[1].AsUInt.Value, array[2].AsTimeSpan.Value, (uint)array[3].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class CoinGiftInfoDTO
    {
        public CoinGiftInfoDTO(System.Guid giftID, CoinGiftSubject subject, uint count, string? description, string? extraData1, string? extraData2, string? extraData3, string? extraData4)
        {
            this.GiftID = giftID;
            this.Subject = subject;
            this.Count = count;
            this.Description = description;
            this.ExtraData1 = extraData1;
            this.ExtraData2 = extraData2;
            this.ExtraData3 = extraData3;
            this.ExtraData4 = extraData4;
        }

        public System.Guid GiftID { get; }
        public CoinGiftSubject Subject { get; }
        public uint Count { get; }
        public string? Description { get; }
        public string? ExtraData1 { get; }
        public string? ExtraData2 { get; }
        public string? ExtraData3 { get; }
        public string? ExtraData4 { get; }

        public static implicit operator CoinGiftInfoDTO(FLGrainInterfaces.CoinGiftInfo obj) => new CoinGiftInfoDTO(obj.GiftID, obj.Subject, obj.Count, obj.Description, obj.ExtraData1, obj.ExtraData2, obj.ExtraData3, obj.ExtraData4);

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.Guid(GiftID), LightMessage.Common.WireProtocol.Param.UEnum(Subject), LightMessage.Common.WireProtocol.Param.UInt(Count), LightMessage.Common.WireProtocol.Param.String(Description), LightMessage.Common.WireProtocol.Param.String(ExtraData1), LightMessage.Common.WireProtocol.Param.String(ExtraData2), LightMessage.Common.WireProtocol.Param.String(ExtraData3), LightMessage.Common.WireProtocol.Param.String(ExtraData4));

        public static CoinGiftInfoDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new CoinGiftInfoDTO(array[0].AsGuid.Value, array[1].AsUEnum<CoinGiftSubject>().Value, (uint)array[2].AsUInt.Value, array[3].AsString, array[4].AsString, array[5].AsString, array[6].AsString, array[7].AsString);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class GroupInfoDTO
    {
        public GroupInfoDTO(string name, ushort id)
        {
            this.Name = name;
            this.ID = id;
        }

        public string Name { get; }
        public ushort ID { get; }

        public static implicit operator GroupInfoDTO(FLGrainInterfaces.Configuration.GroupConfig obj) => new GroupInfoDTO(obj.Name, obj.ID);

        public LightMessage.Common.WireProtocol.Param ToParam() => LightMessage.Common.WireProtocol.Param.Array(LightMessage.Common.WireProtocol.Param.String(Name), LightMessage.Common.WireProtocol.Param.UInt(ID));

        public static GroupInfoDTO FromParam(LightMessage.Common.WireProtocol.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new GroupInfoDTO(array[0].AsString, (ushort)array[1].AsUInt.Value);
        }
    }
}