using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface IMatchMakingGrain : IGrainWithIntegerKey
    {
        Task AddGame(IGame game, IPlayer firstPlayer);
        Task<(Guid gameID, PlayerInfoDTO? opponentInfo, byte numRounds, bool myTurnFirst, TimeSpan? expiryTimeRemaining)> 
            FindOrCreateGame(IPlayer player, Immutable<ISet<Guid>> activeOpponents);
    }
}
