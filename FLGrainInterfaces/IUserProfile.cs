using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface IUserProfile : IGrainWithGuidKey
    {
        Task<byte> JoinGameAsFirstPlayer(IGame game);
        Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game);
        Task<Immutable<IReadOnlyList<IGame>>> GetGames();
    }
}
