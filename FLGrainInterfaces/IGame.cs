using FLGameLogic;
using LightMessage.Common.Messages;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public enum GameState
    {
        New,
        WaitingForSecondPlayer,
        InProgress,
        Finished
    }

    public static class WordScorePairExtensions
    {
        public static Param ToParam(this WordScorePair ws) => Param.Array(Param.String(ws.word), Param.UInt(ws.score));
    }

    //?? rework according to new client-side logic
    [Immutable]
    public class GameInfo
    {
        public Guid OtherPlayerID { get; set; }
        public byte NumRounds { get; set; }
        public List<string> Categories { get; set; }
        public IReadOnlyList<IReadOnlyList<WordScorePair>> MyWordsPlayed { get; set; }
        public IReadOnlyList<IReadOnlyList<WordScorePair>> TheirWordsPlayed { get; set; }
        public DateTime MyTurnEndTime { get; set; }
        public bool MyTurnFirst { get; set; }
        public byte NumTurnsTakenByOpponent { get; set; }


        public async Task<Param> ToParam(IGrainFactory grainFactory)
        {
            return Param.Array(
                await PlayerInfo.GetAsParamForPlayerID(grainFactory, OtherPlayerID),
                Param.UInt(NumRounds),
                Param.Array(Categories.Select(c => Param.String(c))),
                Param.Array(MyWordsPlayed.Select(
                    turnWords => Param.Array(turnWords.Select(
                        wordScore => wordScore.ToParam()
                    ))
                )),
                TheirWordsPlayed == null ? Param.Array() : Param.Array(TheirWordsPlayed.Select(
                    turnWords => Param.Array(turnWords.Select(
                        wordScore => Param.Array(Param.String(wordScore.word), Param.UInt(wordScore.score))
                    ))
                )),
                Param.DateTime(MyTurnEndTime),
                Param.Boolean(MyTurnFirst),
                Param.UInt(NumTurnsTakenByOpponent)
            );
        }
    }

    [Immutable]
    public class SimplifiedGameInfo
    {
        public Guid GameID { get; set; }
        public GameState GameState { get; set; }
        public Guid OtherPlayerID { get; set; }
        public bool MyTurn { get; set; }
        public byte MyScore { get; set; }
        public byte TheirScore { get; set; }

        public IEnumerable<Param> ToParams(IGrainFactory grainFactory)
        {
            return new[]
            {
                Param.Guid(GameID),
                Param.UInt((ulong)GameState),
                Param.String(OtherPlayerID.ToString()), //?? player name
                Param.Boolean(MyTurn),
                Param.UInt(MyScore),
                Param.UInt(TheirScore)
            };
        }
    }

    //?? we should probably support disconnections during play, as the games are not time-sensitive in nature (IF we can trust clients...)
    public interface IGame : IGrainWithGuidKey
    {
        Task<GameState> GetState();
        Task<byte> StartNew(Guid playerOneID); // Returns number of rounds
        Task<(Guid opponentID, byte numRounds)> AddSecondPlayer(PlayerInfo playerTwo);
        Task<(string category, TimeSpan turnTime)> StartRound(Guid id);
        Task<(byte wordScore, string corrected)> PlayWord(Guid id, string word);
        Task<IEnumerable<WordScorePair>> EndRound(Guid playerID); // Returns words opponent played this round, if they took their turn already

        Task<GameInfo> GetGameInfo(Guid playerID);
        Task<SimplifiedGameInfo> GetSimplifiedGameInfo(Guid playerID);
    }
}
