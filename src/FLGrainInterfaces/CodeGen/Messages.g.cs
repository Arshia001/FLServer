using System.Linq;
using FLGrainInterfaces;
#nullable enable annotations 

namespace FLGrainInterfaces
{
    public interface ISystemEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
        System.Threading.Tasks.Task<bool> SendNumRoundsWonForRewardUpdated(System.Guid clientID, uint totalRoundsWon);
        System.Threading.Tasks.Task<bool> SendStatisticUpdated(System.Guid clientID, StatisticValue stat);
    }

    public interface ISuggestionEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
    }

    public interface IGameEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
        System.Threading.Tasks.Task<bool> SendOpponentJoined(System.Guid clientID, System.Guid gameID, PlayerInfo opponentInfo);
        System.Threading.Tasks.Task<bool> SendOpponentTurnEnded(System.Guid clientID, System.Guid gameID, byte roundNumber, System.Collections.Generic.IEnumerable<WordScorePairDTO>? wordsPlayed);
        System.Threading.Tasks.Task<bool> SendGameEnded(System.Guid clientID, System.Guid gameID, uint myScore, uint theirScore, uint myPlayerScore, uint myPlayerRank, uint myLevel, uint myXP, ulong myGold);
        System.Threading.Tasks.Task<bool> SendGameExpired(System.Guid clientID, System.Guid gameID, bool myWin, uint myPlayerScore, uint myPlayerRank, uint myLevel, uint myXP, ulong myGold);
    }
}
#nullable enable annotations 

namespace FLGrainInterfaces
{
    public enum HandShakeMode
    {
        ClientID,
        EmailAndPassword,
        RecoveryEmailRequest
    }

    public enum GameState
    {
        New,
        WaitingForSecondPlayer,
        InProgress,
        Finished,
        Expired
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
        MoneySpentInfinitePlay,
        InfinitePlayUsed,
        GameLostDueToExpiry,
        RoundsCompleted
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
        AlreadyRegistered
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

    [Orleans.Concurrency.Immutable]
    public class PlayerLeaderBoardInfo
    {
        public PlayerLeaderBoardInfo(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.String(Name));

        public static PlayerLeaderBoardInfo FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new PlayerLeaderBoardInfo(array[0].AsString);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class LeaderBoardEntryDTO
    {
        public LeaderBoardEntryDTO(PlayerLeaderBoardInfo? info, ulong rank, ulong score)
        {
            this.Info = info;
            this.Rank = rank;
            this.Score = score;
        }

        public PlayerLeaderBoardInfo? Info { get; }
        public ulong Rank { get; }
        public ulong Score { get; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(Info?.ToParam() ?? LightMessage.Common.Messages.Param.Null(), LightMessage.Common.Messages.Param.UInt(Rank), LightMessage.Common.Messages.Param.UInt(Score));

        public static LeaderBoardEntryDTO FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new LeaderBoardEntryDTO(PlayerLeaderBoardInfo.FromParam(array[0]), array[1].AsUInt.Value, array[2].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class PlayerInfo
    {
        public PlayerInfo(System.Guid id, string name, uint level)
        {
            this.ID = id;
            this.Name = name;
            this.Level = level;
        }

        public System.Guid ID { get; }
        public string Name { get; }
        public uint Level { get; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.Guid(ID), LightMessage.Common.Messages.Param.String(Name), LightMessage.Common.Messages.Param.UInt(Level));

        public static PlayerInfo FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new PlayerInfo(array[0].AsGuid.Value, array[1].AsString, (uint)array[2].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class StatisticValue
    {
        public StatisticValue(Statistics statistic, int parameter, ulong value)
        {
            this.Statistic = statistic;
            this.Parameter = parameter;
            this.Value = value;
        }

        public Statistics Statistic { get; }
        public int Parameter { get; }
        public ulong Value { get; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.UEnum(Statistic), LightMessage.Common.Messages.Param.Int(Parameter), LightMessage.Common.Messages.Param.UInt(Value));

        public static StatisticValue FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new StatisticValue(array[0].AsUEnum<Statistics>().Value, (int)array[1].AsInt.Value, array[2].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class OwnPlayerInfo
    {
        public OwnPlayerInfo(string name, string? email, uint xp, uint level, uint nextLevelXPThreshold, uint score, uint rank, ulong gold, uint currentNumRoundsWonForReward, System.TimeSpan nextRoundWinRewardTimeRemaining, System.TimeSpan? infinitePlayTimeRemaining, System.Collections.Generic.IEnumerable<StatisticValue> statisticsValues, bool isRegistered, bool notificationsEnabled, ulong tutorialProgress)
        {
            this.Name = name;
            this.Email = email;
            this.XP = xp;
            this.Level = level;
            this.NextLevelXPThreshold = nextLevelXPThreshold;
            this.Score = score;
            this.Rank = rank;
            this.Gold = gold;
            this.CurrentNumRoundsWonForReward = currentNumRoundsWonForReward;
            this.NextRoundWinRewardTimeRemaining = nextRoundWinRewardTimeRemaining;
            this.InfinitePlayTimeRemaining = infinitePlayTimeRemaining;
            this.StatisticsValues = statisticsValues.ToList();
            this.IsRegistered = isRegistered;
            this.NotificationsEnabled = notificationsEnabled;
            this.TutorialProgress = tutorialProgress;
        }

        public string Name { get; }
        public string? Email { get; }
        public uint XP { get; }
        public uint Level { get; }
        public uint NextLevelXPThreshold { get; }
        public uint Score { get; }
        public uint Rank { get; }
        public ulong Gold { get; }
        public uint CurrentNumRoundsWonForReward { get; }
        public System.TimeSpan NextRoundWinRewardTimeRemaining { get; }
        public System.TimeSpan? InfinitePlayTimeRemaining { get; }
        public System.Collections.Generic.IReadOnlyList<StatisticValue> StatisticsValues { get; }
        public bool IsRegistered { get; }
        public bool NotificationsEnabled { get; }
        public ulong TutorialProgress { get; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.String(Name), LightMessage.Common.Messages.Param.String(Email), LightMessage.Common.Messages.Param.UInt(XP), LightMessage.Common.Messages.Param.UInt(Level), LightMessage.Common.Messages.Param.UInt(NextLevelXPThreshold), LightMessage.Common.Messages.Param.UInt(Score), LightMessage.Common.Messages.Param.UInt(Rank), LightMessage.Common.Messages.Param.UInt(Gold), LightMessage.Common.Messages.Param.UInt(CurrentNumRoundsWonForReward), LightMessage.Common.Messages.Param.TimeSpan(NextRoundWinRewardTimeRemaining), LightMessage.Common.Messages.Param.TimeSpan(InfinitePlayTimeRemaining), LightMessage.Common.Messages.Param.Array(StatisticsValues.Select(a => a?.ToParam() ?? LightMessage.Common.Messages.Param.Null())), LightMessage.Common.Messages.Param.Boolean(IsRegistered), LightMessage.Common.Messages.Param.Boolean(NotificationsEnabled), LightMessage.Common.Messages.Param.UInt(TutorialProgress));

        public static OwnPlayerInfo FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new OwnPlayerInfo(array[0].AsString, array[1].AsString, (uint)array[2].AsUInt.Value, (uint)array[3].AsUInt.Value, (uint)array[4].AsUInt.Value, (uint)array[5].AsUInt.Value, (uint)array[6].AsUInt.Value, array[7].AsUInt.Value, (uint)array[8].AsUInt.Value, array[9].AsTimeSpan.Value, array[10].AsTimeSpan, array[11].AsArray.Select(a => StatisticValue.FromParam(a)).ToList(), array[12].AsBoolean.Value, array[13].AsBoolean.Value, array[14].AsUInt.Value);
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

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.String(Word), LightMessage.Common.Messages.Param.UInt(Score));

        public static WordScorePairDTO FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new WordScorePairDTO(array[0].AsString, (byte)array[1].AsUInt.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class GameInfo
    {
        public GameInfo(PlayerInfo? otherPlayerInfo, byte numRounds, System.Collections.Generic.IEnumerable<string> categories, System.Collections.Generic.IEnumerable<bool> haveCategoryAnswers, System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<WordScorePairDTO>> myWordsPlayed, System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<WordScorePairDTO>>? theirWordsPlayed, System.DateTime myTurnEndTime, bool myTurnFirst, byte numTurnsTakenByOpponent, bool expired, bool expiredForMe)
        {
            this.OtherPlayerInfo = otherPlayerInfo;
            this.NumRounds = numRounds;
            this.Categories = categories.ToList();
            this.HaveCategoryAnswers = haveCategoryAnswers.ToList();
            this.MyWordsPlayed = myWordsPlayed.Select(a => a.ToList()).ToList();
            this.TheirWordsPlayed = theirWordsPlayed?.Select(a => a.ToList()).ToList();
            this.MyTurnEndTime = myTurnEndTime;
            this.MyTurnFirst = myTurnFirst;
            this.NumTurnsTakenByOpponent = numTurnsTakenByOpponent;
            this.Expired = expired;
            this.ExpiredForMe = expiredForMe;
        }

        public PlayerInfo? OtherPlayerInfo { get; }
        public byte NumRounds { get; }
        public System.Collections.Generic.IReadOnlyList<string> Categories { get; }
        public System.Collections.Generic.IReadOnlyList<bool> HaveCategoryAnswers { get; }
        public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<WordScorePairDTO>> MyWordsPlayed { get; }
        public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<WordScorePairDTO>>? TheirWordsPlayed { get; }
        public System.DateTime MyTurnEndTime { get; }
        public bool MyTurnFirst { get; }
        public byte NumTurnsTakenByOpponent { get; }
        public bool Expired { get; }
        public bool ExpiredForMe { get; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(OtherPlayerInfo?.ToParam() ?? LightMessage.Common.Messages.Param.Null(), LightMessage.Common.Messages.Param.UInt(NumRounds), LightMessage.Common.Messages.Param.Array(Categories.Select(a => LightMessage.Common.Messages.Param.String(a))), LightMessage.Common.Messages.Param.Array(HaveCategoryAnswers.Select(a => LightMessage.Common.Messages.Param.Boolean(a))), LightMessage.Common.Messages.Param.Array(MyWordsPlayed.Select(a => LightMessage.Common.Messages.Param.Array(a.Select(b => b?.ToParam() ?? LightMessage.Common.Messages.Param.Null())))), LightMessage.Common.Messages.Param.Array(TheirWordsPlayed?.Select(a => LightMessage.Common.Messages.Param.Array(a.Select(b => b?.ToParam() ?? LightMessage.Common.Messages.Param.Null())))), LightMessage.Common.Messages.Param.DateTime(MyTurnEndTime), LightMessage.Common.Messages.Param.Boolean(MyTurnFirst), LightMessage.Common.Messages.Param.UInt(NumTurnsTakenByOpponent), LightMessage.Common.Messages.Param.Boolean(Expired), LightMessage.Common.Messages.Param.Boolean(ExpiredForMe));

        public static GameInfo FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new GameInfo(PlayerInfo.FromParam(array[0]), (byte)array[1].AsUInt.Value, array[2].AsArray.Select(a => a.AsString).ToList(), array[3].AsArray.Select(a => a.AsBoolean.Value).ToList(), array[4].AsArray.Select(a => a.AsArray.Select(b => WordScorePairDTO.FromParam(b)).ToList()).ToList(), array[5].AsArray?.Select(a => a.AsArray.Select(b => WordScorePairDTO.FromParam(b)).ToList()).ToList(), array[6].AsDateTime.Value, array[7].AsBoolean.Value, (byte)array[8].AsUInt.Value, array[9].AsBoolean.Value, array[10].AsBoolean.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class SimplifiedGameInfo
    {
        public SimplifiedGameInfo(System.Guid gameID, GameState gameState, string? otherPlayerName, bool myTurn, byte myScore, byte theirScore, bool winnerOfExpiredGame)
        {
            this.GameID = gameID;
            this.GameState = gameState;
            this.OtherPlayerName = otherPlayerName;
            this.MyTurn = myTurn;
            this.MyScore = myScore;
            this.TheirScore = theirScore;
            this.WinnerOfExpiredGame = winnerOfExpiredGame;
        }

        public System.Guid GameID { get; }
        public GameState GameState { get; }
        public string? OtherPlayerName { get; }
        public bool MyTurn { get; }
        public byte MyScore { get; }
        public byte TheirScore { get; }
        public bool WinnerOfExpiredGame { get; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.Guid(GameID), LightMessage.Common.Messages.Param.UEnum(GameState), LightMessage.Common.Messages.Param.String(OtherPlayerName), LightMessage.Common.Messages.Param.Boolean(MyTurn), LightMessage.Common.Messages.Param.UInt(MyScore), LightMessage.Common.Messages.Param.UInt(TheirScore), LightMessage.Common.Messages.Param.Boolean(WinnerOfExpiredGame));

        public static SimplifiedGameInfo FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new SimplifiedGameInfo(array[0].AsGuid.Value, array[1].AsUEnum<GameState>().Value, array[2].AsString, array[3].AsBoolean.Value, (byte)array[4].AsUInt.Value, (byte)array[5].AsUInt.Value, array[6].AsBoolean.Value);
        }
    }

    [Orleans.Concurrency.Immutable]
    public class ConfigValuesDTO
    {
        public ConfigValuesDTO(byte numRoundsToWinToGetReward, System.TimeSpan roundWinRewardInterval, uint numGoldRewardForWinningRounds, uint priceToRefreshGroups, System.TimeSpan roundTimeExtension, uint roundTimeExtensionPrice, uint revealWordPrice, uint getAnswersPrice, uint maxActiveGames, uint infinitePlayPrice, uint numTimeExtensionsPerRound, byte refreshGroupsAllowedPerRound)
        {
            this.NumRoundsToWinToGetReward = numRoundsToWinToGetReward;
            this.RoundWinRewardInterval = roundWinRewardInterval;
            this.NumGoldRewardForWinningRounds = numGoldRewardForWinningRounds;
            this.PriceToRefreshGroups = priceToRefreshGroups;
            this.RoundTimeExtension = roundTimeExtension;
            this.RoundTimeExtensionPrice = roundTimeExtensionPrice;
            this.RevealWordPrice = revealWordPrice;
            this.GetAnswersPrice = getAnswersPrice;
            this.MaxActiveGames = maxActiveGames;
            this.InfinitePlayPrice = infinitePlayPrice;
            this.NumTimeExtensionsPerRound = numTimeExtensionsPerRound;
            this.RefreshGroupsAllowedPerRound = refreshGroupsAllowedPerRound;
        }

        public byte NumRoundsToWinToGetReward { get; }
        public System.TimeSpan RoundWinRewardInterval { get; }
        public uint NumGoldRewardForWinningRounds { get; }
        public uint PriceToRefreshGroups { get; }
        public System.TimeSpan RoundTimeExtension { get; }
        public uint RoundTimeExtensionPrice { get; }
        public uint RevealWordPrice { get; }
        public uint GetAnswersPrice { get; }
        public uint MaxActiveGames { get; }
        public uint InfinitePlayPrice { get; }
        public uint NumTimeExtensionsPerRound { get; }
        public byte RefreshGroupsAllowedPerRound { get; }

        public static implicit operator ConfigValuesDTO(FLGrainInterfaces.Configuration.ConfigValues obj) => new ConfigValuesDTO(obj.NumRoundsToWinToGetReward, obj.RoundWinRewardInterval, obj.NumGoldRewardForWinningRounds, obj.PriceToRefreshGroups, obj.RoundTimeExtension, obj.RoundTimeExtensionPrice, obj.RevealWordPrice, obj.GetAnswersPrice, obj.MaxActiveGames, obj.InfinitePlayPrice, obj.NumTimeExtensionsPerRound, obj.RefreshGroupsAllowedPerRound);

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.UInt(NumRoundsToWinToGetReward), LightMessage.Common.Messages.Param.TimeSpan(RoundWinRewardInterval), LightMessage.Common.Messages.Param.UInt(NumGoldRewardForWinningRounds), LightMessage.Common.Messages.Param.UInt(PriceToRefreshGroups), LightMessage.Common.Messages.Param.TimeSpan(RoundTimeExtension), LightMessage.Common.Messages.Param.UInt(RoundTimeExtensionPrice), LightMessage.Common.Messages.Param.UInt(RevealWordPrice), LightMessage.Common.Messages.Param.UInt(GetAnswersPrice), LightMessage.Common.Messages.Param.UInt(MaxActiveGames), LightMessage.Common.Messages.Param.UInt(InfinitePlayPrice), LightMessage.Common.Messages.Param.UInt(NumTimeExtensionsPerRound), LightMessage.Common.Messages.Param.UInt(RefreshGroupsAllowedPerRound));

        public static ConfigValuesDTO FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new ConfigValuesDTO((byte)array[0].AsUInt.Value, array[1].AsTimeSpan.Value, (uint)array[2].AsUInt.Value, (uint)array[3].AsUInt.Value, array[4].AsTimeSpan.Value, (uint)array[5].AsUInt.Value, (uint)array[6].AsUInt.Value, (uint)array[7].AsUInt.Value, (uint)array[8].AsUInt.Value, (uint)array[9].AsUInt.Value, (uint)array[10].AsUInt.Value, (byte)array[11].AsUInt.Value);
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

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.String(Sku), LightMessage.Common.Messages.Param.UInt(NumGold), LightMessage.Common.Messages.Param.String(Title), LightMessage.Common.Messages.Param.UEnum(Tag));

        public static GoldPackConfigDTO FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new GoldPackConfigDTO(array[0].AsString, (uint)array[1].AsUInt.Value, array[2].AsString, array[3].AsUEnum<GoldPackTag>().Value);
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

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.String(Name), LightMessage.Common.Messages.Param.UInt(ID));

        public static GroupInfoDTO FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new GroupInfoDTO(array[0].AsString, (ushort)array[1].AsUInt.Value);
        }
    }
}