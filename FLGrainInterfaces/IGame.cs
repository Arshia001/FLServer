using FLGameLogic;
using LightMessage.Common.Messages;
using Orleans;
using Orleans.Concurrency;
using OrleansBondUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    //?? we should probably support disconnections during play, as the games are not time-sensitive in nature (IF we can trust clients...)
    [BondSerializationTag("@g")]
    public interface IGame : IGrainWithGuidKey
    {
        Task<GameState> GetState();
        Task<byte> StartNew(Guid playerOneID); // Returns number of rounds
        Task<(Guid opponentID, byte numRounds)> AddSecondPlayer(PlayerInfo playerTwo);
        Task<(string category, bool? haveAnswers, TimeSpan? roundTime, bool mustChooseGroup, IEnumerable<GroupInfoDTO> groups)> StartRound(Guid id);
        Task<(string category, bool haveAnswers, TimeSpan roundTime)> ChooseGroup(Guid id, ushort groupID);
        Task<(byte wordScore, string corrected)> PlayWord(Guid id, string word);
        Task<Immutable<IEnumerable<WordScorePair>>> EndRound(Guid playerID); // Returns words opponent played this round, if they took their turn already

        Task<TimeSpan?> IncreaseRoundTime(Guid playerID);
        Task<(string word, byte wordScore)?> RevealWord(Guid playerID);

        Task<List<GroupConfig>> RefreshGroups(Guid guid);

        Task<Immutable<GameInfo>> GetGameInfo(Guid playerID);
        Task<Immutable<SimplifiedGameInfo>> GetSimplifiedGameInfo(Guid playerID);
        Task<bool> WasFirstTurnPlayed(); //?? change to notification sent from game to matchmaking sytem
    }
}
