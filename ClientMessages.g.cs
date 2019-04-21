using System.Linq;
using Network;
using Network.Types;

namespace Network
{
    public class SystemEndPoint : LightMessage.Unity.EndPoint
    {
        protected override string EndPointName => "sys";

        public System.Threading.Tasks.Task SuggestCategory(string name, System.Collections.Generic.IReadOnlyList<string> words) => EndPointProxy.SendInvocationForReply("csug", System.Threading.CancellationToken.None, LightMessage.Common.Messages.Param.String(name), LightMessage.Common.Messages.Param.Array(words.Select(a => LightMessage.Common.Messages.Param.String(a))));
        public System.Threading.Tasks.Task SuggestWord(string categoryName, System.Collections.Generic.IReadOnlyList<string> words) => EndPointProxy.SendInvocationForReply("wsug", System.Threading.CancellationToken.None, LightMessage.Common.Messages.Param.String(categoryName), LightMessage.Common.Messages.Param.Array(words.Select(a => LightMessage.Common.Messages.Param.String(a))));

        protected override void RegisterMessageEvents()
        {
        }
    }

    public class GameEndPoint : LightMessage.Unity.EndPoint
    {
        protected override string EndPointName => "gm";

        public delegate void OpponentJoinedDelegate(System.Guid gameID, PlayerInfo opponentInfo);

        public event OpponentJoinedDelegate OpponentJoined;

        void OnOpponentJoined(System.Collections.Generic.IReadOnlyList<LightMessage.Common.Messages.Param> args)
        {
            OpponentJoined?.Invoke(args[0].AsGuid.Value, PlayerInfo.FromParam(args[1]));
        }

        public delegate void OpponentTurnEndedDelegate(System.Guid gameID, byte roundNumber, System.Collections.Generic.IReadOnlyList<WordScorePairDTO> wordsPlayed);

        public event OpponentTurnEndedDelegate OpponentTurnEnded;

        void OnOpponentTurnEnded(System.Collections.Generic.IReadOnlyList<LightMessage.Common.Messages.Param> args)
        {
            OpponentTurnEnded?.Invoke(args[0].AsGuid.Value, (byte)args[1].AsUInt.Value, args[2].AsArray.Select(a => WordScorePairDTO.FromParam(a)).ToList());
        }

        public delegate void GameEndedDelegate(System.Guid gameID, uint myScore, uint theirScore);

        public event GameEndedDelegate GameEnded;

        void OnGameEnded(System.Collections.Generic.IReadOnlyList<LightMessage.Common.Messages.Param> args)
        {
            GameEnded?.Invoke(args[0].AsGuid.Value, (uint)args[1].AsUInt.Value, (uint)args[2].AsUInt.Value);
        }

        public async System.Threading.Tasks.Task<(System.Guid gameID, PlayerInfo opponentInfo, byte numRounds, bool myTurnFirst)> NewGame()
        {
            var result = await EndPointProxy.SendInvocationForReply("new", System.Threading.CancellationToken.None);
            return (result[0].AsGuid.Value, PlayerInfo.FromParam(result[1]), (byte)result[2].AsUInt.Value, result[3].AsBoolean.Value);
        }

        public async System.Threading.Tasks.Task<(string category, System.TimeSpan roundTime)> StartRound(System.Guid gameID)
        {
            var result = await EndPointProxy.SendInvocationForReply("round", System.Threading.CancellationToken.None, LightMessage.Common.Messages.Param.Guid(gameID));
            return (result[0].AsString, result[1].AsTimeSpan.Value);
        }

        public async System.Threading.Tasks.Task<(byte wordScore, string corrected)> PlayWord(System.Guid gameID, string word)
        {
            var result = await EndPointProxy.SendInvocationForReply("word", System.Threading.CancellationToken.None, LightMessage.Common.Messages.Param.Guid(gameID), LightMessage.Common.Messages.Param.String(word));
            return ((byte)result[0].AsUInt.Value, result[1].AsString);
        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<WordScorePairDTO>> EndRound(System.Guid gameID)
        {
            var result = await EndPointProxy.SendInvocationForReply("endr", System.Threading.CancellationToken.None, LightMessage.Common.Messages.Param.Guid(gameID));
            return result[0].AsArray.Select(a => WordScorePairDTO.FromParam(a)).ToList();
        }

        public async System.Threading.Tasks.Task<GameInfo> GetGameInfo(System.Guid gameID)
        {
            var result = await EndPointProxy.SendInvocationForReply("info", System.Threading.CancellationToken.None, LightMessage.Common.Messages.Param.Guid(gameID));
            return GameInfo.FromParam(result[0]);
        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<SimplifiedGameInfo>> GetAllGames()
        {
            var result = await EndPointProxy.SendInvocationForReply("all", System.Threading.CancellationToken.None);
            return result[0].AsArray.Select(a => SimplifiedGameInfo.FromParam(a)).ToList();
        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<WordScorePairDTO>> GetAnswers(string category)
        {
            var result = await EndPointProxy.SendInvocationForReply("ans", System.Threading.CancellationToken.None, LightMessage.Common.Messages.Param.String(category));
            return result[0].AsArray.Select(a => WordScorePairDTO.FromParam(a)).ToList();
        }

        protected override void RegisterMessageEvents()
        {
            EndPointProxy.On("opj", OnOpponentJoined);
            EndPointProxy.On("opr", OnOpponentTurnEnded);
            EndPointProxy.On("gend", OnGameEnded);
        }
    }

    public class ConnectionManager : LightMessage.Unity.ConnectionManagerBase<ConnectionManager>
    {
        protected override System.Collections.Generic.IEnumerable<LightMessage.Unity.EndPoint> GetEndPoints() => new LightMessage.Unity.EndPoint[] { new SystemEndPoint(), new GameEndPoint() };
        public System.Threading.Tasks.Task<System.Guid> Connect(System.Guid? clientID) => base.Connect(LightMessage.Common.Messages.Param.Guid(clientID));
    }
}

namespace Network.Types
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