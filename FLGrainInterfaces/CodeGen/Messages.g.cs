using System.Linq;
using FLGrainInterfaces;

namespace FLGrainInterfaces
{
    public interface ISystemEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
        System.Threading.Tasks.Task SendNumRoundsWonForRewardUpdated(System.Guid clientID, uint totalRoundsWon);
    }

    public interface ISuggestionEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
    }

    public interface IGameEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
        System.Threading.Tasks.Task SendOpponentJoined(System.Guid clientID, System.Guid gameID, PlayerInfo opponentInfo);
        System.Threading.Tasks.Task SendOpponentTurnEnded(System.Guid clientID, System.Guid gameID, byte roundNumber, System.Collections.Generic.IEnumerable<WordScorePairDTO> wordsPlayed);
        System.Threading.Tasks.Task SendGameEnded(System.Guid clientID, System.Guid gameID, uint myScore, uint theirScore, uint myPlayerScore, uint myPlayerRank);
    }
}

namespace FLGrainInterfaces
{
    public enum GameState
    {
        New,
        WaitingForSecondPlayer,
        InProgress,
        Finished
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

    public class LeaderBoardEntryDTO
    {
        public string Name { get; set; }
        public ulong Rank { get; set; }
        public ulong Score { get; set; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.String(Name), LightMessage.Common.Messages.Param.UInt(Rank), LightMessage.Common.Messages.Param.UInt(Score));

        public static LeaderBoardEntryDTO FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new LeaderBoardEntryDTO { Name = array[0].AsString, Rank = array[1].AsUInt.Value, Score = array[2].AsUInt.Value };
        }
    }

    public class PlayerInfo
    {
        public System.Guid ID { get; set; }
        public string Name { get; set; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.Guid(ID), LightMessage.Common.Messages.Param.String(Name));

        public static PlayerInfo FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new PlayerInfo { ID = array[0].AsGuid.Value, Name = array[1].AsString };
        }
    }

    public class OwnPlayerInfo
    {
        public string Name { get; set; }
        public uint XP { get; set; }
        public uint Level { get; set; }
        public uint NextLevelXPThreshold { get; set; }
        public uint Score { get; set; }
        public uint Rank { get; set; }
        public ulong Gold { get; set; }
        public uint CurrentNumRoundsWonForReward { get; set; }
        public System.TimeSpan NextRoundWinRewardTimeRemaining { get; set; }
        public System.TimeSpan? InfinitePlayTimeRemaining { get; set; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.String(Name), LightMessage.Common.Messages.Param.UInt(XP), LightMessage.Common.Messages.Param.UInt(Level), LightMessage.Common.Messages.Param.UInt(NextLevelXPThreshold), LightMessage.Common.Messages.Param.UInt(Score), LightMessage.Common.Messages.Param.UInt(Rank), LightMessage.Common.Messages.Param.UInt(Gold), LightMessage.Common.Messages.Param.UInt(CurrentNumRoundsWonForReward), LightMessage.Common.Messages.Param.TimeSpan(NextRoundWinRewardTimeRemaining), LightMessage.Common.Messages.Param.TimeSpan(InfinitePlayTimeRemaining));

        public static OwnPlayerInfo FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new OwnPlayerInfo { Name = array[0].AsString, XP = (uint)array[1].AsUInt.Value, Level = (uint)array[2].AsUInt.Value, NextLevelXPThreshold = (uint)array[3].AsUInt.Value, Score = (uint)array[4].AsUInt.Value, Rank = (uint)array[5].AsUInt.Value, Gold = array[6].AsUInt.Value, CurrentNumRoundsWonForReward = (uint)array[7].AsUInt.Value, NextRoundWinRewardTimeRemaining = array[8].AsTimeSpan.Value, InfinitePlayTimeRemaining = array[9].AsTimeSpan };
        }
    }

    public class WordScorePairDTO
    {
        public string Word { get; set; }
        public byte Score { get; set; }

        public static implicit operator WordScorePairDTO(FLGameLogic.WordScorePair obj) => new WordScorePairDTO { Word = obj.word, Score = obj.score };
        public static implicit operator FLGameLogic.WordScorePair(WordScorePairDTO obj) => new FLGameLogic.WordScorePair { word = obj.Word, score = obj.Score };

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.String(Word), LightMessage.Common.Messages.Param.UInt(Score));

        public static WordScorePairDTO FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new WordScorePairDTO { Word = array[0].AsString, Score = (byte)array[1].AsUInt.Value };
        }
    }

    public class GameInfo
    {
        public PlayerInfo OtherPlayerInfo { get; set; }
        public byte NumRounds { get; set; }
        public System.Collections.Generic.IReadOnlyList<string> Categories { get; set; }
        public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<WordScorePairDTO>> MyWordsPlayed { get; set; }
        public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<WordScorePairDTO>> TheirWordsPlayed { get; set; }
        public System.DateTime MyTurnEndTime { get; set; }
        public bool MyTurnFirst { get; set; }
        public byte NumTurnsTakenByOpponent { get; set; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(OtherPlayerInfo?.ToParam() ?? LightMessage.Common.Messages.Param.Null(), LightMessage.Common.Messages.Param.UInt(NumRounds), LightMessage.Common.Messages.Param.Array(Categories.Select(a => LightMessage.Common.Messages.Param.String(a))), LightMessage.Common.Messages.Param.Array(MyWordsPlayed.Select(a => LightMessage.Common.Messages.Param.Array(a.Select(b => b?.ToParam() ?? LightMessage.Common.Messages.Param.Null())))), LightMessage.Common.Messages.Param.Array(TheirWordsPlayed.Select(a => LightMessage.Common.Messages.Param.Array(a.Select(b => b?.ToParam() ?? LightMessage.Common.Messages.Param.Null())))), LightMessage.Common.Messages.Param.DateTime(MyTurnEndTime), LightMessage.Common.Messages.Param.Boolean(MyTurnFirst), LightMessage.Common.Messages.Param.UInt(NumTurnsTakenByOpponent));

        public static GameInfo FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new GameInfo { OtherPlayerInfo = PlayerInfo.FromParam(array[0]), NumRounds = (byte)array[1].AsUInt.Value, Categories = array[2].AsArray.Select(a => a.AsString).ToList(), MyWordsPlayed = array[3].AsArray.Select(a => a.AsArray.Select(b => WordScorePairDTO.FromParam(b)).ToList()).ToList(), TheirWordsPlayed = array[4].AsArray.Select(a => a.AsArray.Select(b => WordScorePairDTO.FromParam(b)).ToList()).ToList(), MyTurnEndTime = array[5].AsDateTime.Value, MyTurnFirst = array[6].AsBoolean.Value, NumTurnsTakenByOpponent = (byte)array[7].AsUInt.Value };
        }
    }

    public class SimplifiedGameInfo
    {
        public System.Guid GameID { get; set; }
        public GameState GameState { get; set; }
        public string OtherPlayerName { get; set; }
        public bool MyTurn { get; set; }
        public byte MyScore { get; set; }
        public byte TheirScore { get; set; }

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.Guid(GameID), LightMessage.Common.Messages.Param.UEnum(GameState), LightMessage.Common.Messages.Param.String(OtherPlayerName), LightMessage.Common.Messages.Param.Boolean(MyTurn), LightMessage.Common.Messages.Param.UInt(MyScore), LightMessage.Common.Messages.Param.UInt(TheirScore));

        public static SimplifiedGameInfo FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new SimplifiedGameInfo { GameID = array[0].AsGuid.Value, GameState = array[1].AsUEnum<GameState>().Value, OtherPlayerName = array[2].AsString, MyTurn = array[3].AsBoolean.Value, MyScore = (byte)array[4].AsUInt.Value, TheirScore = (byte)array[5].AsUInt.Value };
        }
    }

    public class ConfigValuesDTO
    {
        public byte NumRoundsToWinToGetReward { get; set; }
        public System.TimeSpan RoundWinRewardInterval { get; set; }
        public uint NumGoldRewardForWinningRounds { get; set; }
        public uint PriceToRefreshGroups { get; set; }
        public System.TimeSpan RoundTimeExtension { get; set; }
        public uint RoundTimeExtensionPrice { get; set; }
        public uint RevealWordPrice { get; set; }
        public uint MaxActiveGames { get; set; }
        public uint InfinitePlayPrice { get; set; }

        public static implicit operator ConfigValuesDTO(ConfigValues obj) => new ConfigValuesDTO { NumRoundsToWinToGetReward = obj.NumRoundsToWinToGetReward, RoundWinRewardInterval = obj.RoundWinRewardInterval, NumGoldRewardForWinningRounds = obj.NumGoldRewardForWinningRounds, PriceToRefreshGroups = obj.PriceToRefreshGroups, RoundTimeExtension = obj.RoundTimeExtension, RoundTimeExtensionPrice = obj.RoundTimeExtensionPrice, RevealWordPrice = obj.RevealWordPrice, MaxActiveGames = obj.MaxActiveGames, InfinitePlayPrice = obj.InfinitePlayPrice };

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.UInt(NumRoundsToWinToGetReward), LightMessage.Common.Messages.Param.TimeSpan(RoundWinRewardInterval), LightMessage.Common.Messages.Param.UInt(NumGoldRewardForWinningRounds), LightMessage.Common.Messages.Param.UInt(PriceToRefreshGroups), LightMessage.Common.Messages.Param.TimeSpan(RoundTimeExtension), LightMessage.Common.Messages.Param.UInt(RoundTimeExtensionPrice), LightMessage.Common.Messages.Param.UInt(RevealWordPrice), LightMessage.Common.Messages.Param.UInt(MaxActiveGames), LightMessage.Common.Messages.Param.UInt(InfinitePlayPrice));

        public static ConfigValuesDTO FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new ConfigValuesDTO { NumRoundsToWinToGetReward = (byte)array[0].AsUInt.Value, RoundWinRewardInterval = array[1].AsTimeSpan.Value, NumGoldRewardForWinningRounds = (uint)array[2].AsUInt.Value, PriceToRefreshGroups = (uint)array[3].AsUInt.Value, RoundTimeExtension = array[4].AsTimeSpan.Value, RoundTimeExtensionPrice = (uint)array[5].AsUInt.Value, RevealWordPrice = (uint)array[6].AsUInt.Value, MaxActiveGames = (uint)array[7].AsUInt.Value, InfinitePlayPrice = (uint)array[8].AsUInt.Value };
        }
    }

    public class GroupInfoDTO
    {
        public string Name { get; set; }
        public ushort ID { get; set; }

        public static implicit operator GroupInfoDTO(GroupConfig obj) => new GroupInfoDTO { Name = obj.Name, ID = obj.ID };

        public LightMessage.Common.Messages.Param ToParam() => LightMessage.Common.Messages.Param.Array(LightMessage.Common.Messages.Param.String(Name), LightMessage.Common.Messages.Param.UInt(ID));

        public static GroupInfoDTO FromParam(LightMessage.Common.Messages.Param param)
        {
            if (param.IsNull)
                return null;
            var array = param.AsArray;
            return new GroupInfoDTO { Name = array[0].AsString, ID = (ushort)array[1].AsUInt.Value };
        }
    }
}