using LightMessage.Common.Messages;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public class PlayerInfoUtil
    {
        public static Task<Immutable<PlayerInfo>> GetForPlayerID(IGrainFactory grainFactory, Guid playerID) => grainFactory.GetGrain<IUserProfile>(playerID).GetPlayerInfo();

        public static Task<Param> GetAsParamForPlayerID(IGrainFactory grainFactory, Guid playerID) => GetForPlayerID(grainFactory, playerID).ContinueWith(t => t.Result.Value.ToParam());
    }


    public interface IUserProfile : IGrainWithGuidKey
    {
        Task<byte> JoinGameAsFirstPlayer(IGame game);
        Task<(Guid opponentID, byte numRounds)> JoinGameAsSecondPlayer(IGame game);
        Task<Immutable<IReadOnlyList<IGame>>> GetGames();
        Task<Immutable<PlayerInfo>> GetPlayerInfo();
    }
}
