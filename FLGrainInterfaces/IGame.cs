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
    public enum EGameState
    {
        New,
        WaitingForSecondPlayer,
        InProgress,
        Finished
    }

    [Immutable]
    public class GameInfo
    {
        public EGameState GameState { get; set; }
        public Guid OtherPlayerID { get; set; }
        public byte NumRounds { get; set; }
        public List<string> Categories { get; set; }
        public List<List<(string word, byte score)>> MyWordsPlayed { get; set; }
        public List<List<(string word, byte score)>> TheirWordsPlayed { get; set; }
        public bool MyTurn { get; set; } //?? share game logic between client and server, move this to client, AFTER PROTOTYPE PHASE! NOT NOW!!!!!!

        public IEnumerable<Param> ToParams(IGrainFactory grainFactory)
        {
            return new[]
            {
                Param.UInt((ulong)GameState),
                Param.String(OtherPlayerID.ToString()), //?? player name
                Param.UInt(NumRounds),
                Param.Array(Categories.Select(c => Param.String(c))),
                Param.Array(MyWordsPlayed.Select(
                    turnWords => Param.Array(turnWords.Select(
                        wordScore => Param.Array(Param.String(wordScore.word), Param.UInt(wordScore.score))
                    ))
                )),
                TheirWordsPlayed == null ? Param.Array() : Param.Array(TheirWordsPlayed.Select(
                    turnWords => Param.Array(turnWords.Select(
                        wordScore => Param.Array(Param.String(wordScore.word), Param.UInt(wordScore.score))
                    ))
                )),
                Param.Boolean(MyTurn)
            };
        }
    }

    [Immutable]
    public class SimplifiedGameInfo
    {
        public Guid GameID { get; set; }
        public EGameState GameState { get; set; }
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
    //?? or, at least support getting the currently in progress turn after a reconnection
    public interface IGame : IGrainWithGuidKey
    {
        Task<EGameState> GetState();
        Task<byte> StartNew(Guid playerOneID); // Returns number of rounds
        Task<(Guid opponentID, byte numRounds)> AddSecondPlayer(Guid playerTwoID);
        Task<(string category, TimeSpan turnTime)> StartRound(Guid id);
        Task<(uint totalScore, sbyte thisWordScore, string corrected)> PlayWord(Guid id, string word);

        Task<GameInfo> GetGameInfo(Guid playerID);
        Task<SimplifiedGameInfo> GetSimplifiedGameInfo(Guid playerID);
    }
}
