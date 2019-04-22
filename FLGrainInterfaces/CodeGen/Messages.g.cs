using System.Linq;
using FLGrainInterfaces;

namespace FLGrainInterfaces
{
    public interface ISystemEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
    }

    public interface IGameEndPoint : LightMessage.OrleansUtils.GrainInterfaces.IEndPointGrain
    {
        System.Threading.Tasks.Task SendOpponentJoined(System.Guid clientID, System.Guid gameID, PlayerInfo opponentInfo);
        System.Threading.Tasks.Task SendOpponentTurnEnded(System.Guid clientID, System.Guid gameID, byte roundNumber, System.Collections.Generic.IEnumerable<WordScorePairDTO> wordsPlayed);
        System.Threading.Tasks.Task SendGameEnded(System.Guid clientID, System.Guid gameID, uint myScore, uint theirScore);
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

    public class WordScorePairDTO
    {
        public string Word { get; set; }
        public byte Score { get; set; }

        public static implicit operator FLGameLogic.WordScorePair(WordScorePairDTO obj) => new FLGameLogic.WordScorePair { word = obj.Word, score = obj.Score };
        public static implicit operator WordScorePairDTO(FLGameLogic.WordScorePair obj) => new WordScorePairDTO { Word = obj.word, Score = obj.score };

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
}