using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface IMatchMakingGrain : IGrainWithIntegerKey
    {
        Task AddGame(IGame game, IPlayer firstPlayer);
        Task<(Guid gameID, PlayerInfo? opponentInfo, byte numRounds, bool myTurnFirst)> FindOrCreateGame(IPlayer player);
    }
}
