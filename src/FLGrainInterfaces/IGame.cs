using FLGameLogic;
using FLGrainInterfaces.Configuration;
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
    // we could support disconnections during play, but game time is limited already, and it takes too long for the client to actually go into disconnected state
    [BondSerializationTag("@g")]
    public interface IGame : IGrainWithGuidKey
    {
        Task<GameState> GetState();
        Task<byte> StartNew(Guid playerOneID); // Returns number of rounds
        Task<(Guid opponentID, byte numRounds)> AddSecondPlayer(PlayerInfo playerTwo);
        Task<(string? category, bool? haveAnswers, TimeSpan? roundTime, bool mustChooseGroup, IEnumerable<GroupInfoDTO> groups)> StartRound(Guid id);
        Task<(string category, bool haveAnswers, TimeSpan roundTime)> ChooseGroup(Guid id, ushort groupID);
        Task<(byte wordScore, string corrected)> PlayWord(Guid id, string word);
        Task<Immutable<IEnumerable<WordScorePair>?>> EndRound(Guid playerID); // Returns words opponent played this round, if they took their turn already

        Task<TimeSpan?> IncreaseRoundTime(Guid playerID);
        Task<(string word, byte wordScore)?> RevealWord(Guid playerID);

        Task<List<GroupConfig>?> RefreshGroups(Guid guid);

        Task<Guid[]> GetPlayerIDs();
        Task<GameInfo> GetGameInfo(Guid playerID);
        Task<SimplifiedGameInfo> GetSimplifiedGameInfo(Guid playerID);
    }
}
